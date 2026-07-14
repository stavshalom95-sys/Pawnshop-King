using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.DifficultyTier;
using UnityEngine;

namespace PawnshopKing.Systems.Negotiation
{
    public enum OfferOutcome
    {
        Accepted,
        AcceptedReluctantly,
        Countered,
        OffendedLeft,
        GaveUpLeft
    }

    public struct OfferResult
    {
        public OfferOutcome outcome;
        /// <summary>Final price when accepted; the new asking price when countered.</summary>
        public int price;
    }

    /// <summary>
    /// Handles offer logic, acceptance/counter behavior, and reputation effects
    /// (GDD 39). The asking price covers whichever items the player has on the
    /// table; closing a deal moves only those into inventory, and the customer
    /// walks with anything the player passed on.
    /// </summary>
    public static class NegotiationSystem
    {
        /// <summary>
        /// What the seller opens with (GDD 13.3): greed and dishonesty inflate,
        /// desperation undercuts. Based on the authentic-market value roll, so a
        /// scammer's fake is priced like the real thing.
        /// </summary>
        public static int CalculateAskingPrice(CustomerInstance customer)
        {
            return CalculateAskingPrice(customer, customer.items);
        }

        /// <summary>Asking price for a chosen subset of what the customer carries.</summary>
        public static int CalculateAskingPrice(CustomerInstance customer, IReadOnlyList<ItemInstance> items)
        {
            int totalValue = 0;
            foreach (var item in items) totalValue += PerceivedValue(customer, item);
            if (totalValue <= 0) return 0;

            float factor = Mathf.Clamp(
                0.5f + 0.45f * customer.greed + 0.2f * (1f - customer.honesty) - 0.3f * customer.desperation
                    + DifficultyTuning.AskFactorShift + TypeAskShift(customer.customerType),
                0.3f, 1.3f);

            // Asks are quoted in $25 steps so subset re-quotes stay too coarse to
            // read exact per-item values off the price deltas.
            return Mathf.Max(5, Mathf.RoundToInt(totalValue * factor / 25f) * 25);
        }

        /// <summary>
        /// The seller's idiosyncratic valuation of one piece: true value ±10%, stable
        /// for the visit (hashed, not rolled) so re-toggling the selection can never
        /// average the noise away. Keeps subset asks from being an exact linear probe
        /// of true item values — the inspection loop stays the better appraisal.
        /// </summary>
        private static int PerceivedValue(CustomerInstance customer, ItemInstance item)
        {
            int hash = (customer.instanceId + item.instanceId).GetHashCode() & 0x7FFFFFFF;
            float wobble = 0.9f + 0.2f * (hash % 1000 / 999f);
            return Mathf.RoundToInt(item.rolledBaseValue * wobble);
        }

        /// <summary>
        /// Re-anchors the ask when the player changes which items are on the table.
        /// A different bundle is a different deal, so counters made against the old
        /// bundle don't carry over — but patience already spent (offersMade) does.
        /// </summary>
        public static void RepriceForSelection(CustomerInstance customer, IReadOnlyList<ItemInstance> selection)
        {
            customer.askingPrice = CalculateAskingPrice(customer, selection);
        }

        /// <summary>Player accepts the customer's current asking price (GDD 13.2).</summary>
        public static OfferResult BuyAtAskingPrice(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype, IReadOnlyList<ItemInstance> selection)
        {
            return CloseDeal(state, customer, archetype, selection, customer.askingPrice, OfferOutcome.Accepted);
        }

        /// <summary>
        /// Resolves one player offer into the GDD 13.5 outcomes: accept, counter,
        /// offended walk-out, desperate capitulation, or fed-up exit.
        /// </summary>
        public static OfferResult MakeOffer(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype, IReadOnlyList<ItemInstance> selection, int offer)
        {
            customer.offersMade++;

            if (offer >= customer.askingPrice)
            {
                // Close at the ask, never above it — a typo'd extra digit must not
                // drain the till when the seller would have taken less.
                return CloseDeal(state, customer, archetype, selection, customer.askingPrice, OfferOutcome.Accepted);
            }

            float ratio = offer / (float)customer.askingPrice;

            // The fraction of the current ask they'd quietly settle for.
            float settleLine = Mathf.Clamp(
                0.9f - 0.4f * customer.desperation + 0.25f * customer.greed
                    + DifficultyTuning.SettleLineShift + TypeSettleShift(customer.customerType),
                0.45f, 1f);
            if (ratio >= settleLine && Random.value < 0.65f + (ratio - settleLine) * 2f)
            {
                return CloseDeal(state, customer, archetype, selection, offer, OfferOutcome.Accepted);
            }

            // Insultingly low offers can end the conversation (GDD 13.5) and, when
            // overused, the lowball habit costs reputation (GDD 13.4).
            float insultLine = Mathf.Clamp(0.45f - 0.3f * customer.desperation, 0.15f, 0.5f);
            if (ratio < insultLine)
            {
                float offendChance = (insultLine - ratio) * 2f + (1f - customer.patience) * 0.5f;
                if (Random.value < offendChance)
                {
                    customer.mood = CustomerMood.Offended;
                    customer.negotiationState = NegotiationState.CustomerLeft;
                    state.reputation -= 1;
                    return new OfferResult { outcome = OfferOutcome.OffendedLeft };
                }
            }

            // Patience budget: 1-4 rounds of haggling before they're done, nudged
            // by the visible customer type (Haggler holds out longer, HurryUp less).
            int maxRounds = Mathf.Max(1,
                1 + Mathf.RoundToInt(customer.patience * 3f) + TypeRoundsShift(customer.customerType));
            if (customer.offersMade >= maxRounds)
            {
                if (customer.desperation > 0.55f && ratio >= insultLine)
                {
                    // "Customer lowers demand if desperate" (GDD 13.5).
                    customer.mood = CustomerMood.Desperate;
                    return CloseDeal(state, customer, archetype, selection, offer, OfferOutcome.AcceptedReluctantly);
                }

                customer.mood = CustomerMood.Impatient;
                customer.negotiationState = NegotiationState.CustomerLeft;
                return new OfferResult { outcome = OfferOutcome.GaveUpLeft };
            }

            // Too close to split hairs over — take the offer.
            if (customer.askingPrice - offer <= 10)
            {
                return CloseDeal(state, customer, archetype, selection, offer, OfferOutcome.Accepted);
            }

            // Counter between the offer and the current ask; greed pulls it upward.
            float pull = Mathf.Clamp(0.35f + 0.4f * customer.greed, 0.35f, 0.8f);
            int counter = Mathf.Clamp(RoundToFive(Mathf.Lerp(offer, customer.askingPrice, pull)),
                offer + 5, customer.askingPrice - 5);

            customer.askingPrice = counter;
            if (customer.offersMade >= maxRounds - 1) customer.mood = CustomerMood.Impatient;
            return new OfferResult { outcome = OfferOutcome.Countered, price = counter };
        }

        public static void Reject(CustomerInstance customer)
        {
            customer.negotiationState = NegotiationState.Rejected;
        }

        private static OfferResult CloseDeal(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype, IReadOnlyList<ItemInstance> selection, int price, OfferOutcome outcome)
        {
            // Split the deal price across the selected items by value share, so each
            // inventory entry knows what it effectively cost (GDD 38.2 acquisition price).
            int totalValue = 0;
            foreach (var item in selection) totalValue += item.rolledBaseValue;

            int remaining = price;
            for (int i = 0; i < selection.Count; i++)
            {
                var item = selection[i];
                bool last = i == selection.Count - 1;
                int share = last || totalValue <= 0
                    ? remaining
                    : Mathf.RoundToInt(price * (item.rolledBaseValue / (float)totalValue));
                remaining -= share;

                item.acquisitionPrice = share;
                item.acquisitionSource = AcquisitionSource.CustomerPurchase;
                state.inventory.Add(item);
            }

            state.cash -= price;
            if (archetype != null) state.heat += archetype.riskProfile.heatPerDeal;
            if (price >= customer.askingPrice) state.reputation += 1; // paid what they asked — fair dealing

            customer.negotiationState = NegotiationState.Accepted;

            // Anything the player passed on leaves with the customer.
            var sold = new HashSet<ItemInstance>(selection);
            customer.items.RemoveAll(sold.Contains);

            return new OfferResult { outcome = outcome, price = price };
        }

        private static int RoundToFive(float value) => Mathf.RoundToInt(value / 5f) * 5;

        // ---- Customer type modifiers (GDD-adjacent: a visible read on an
        // otherwise-hidden stat block — see CustomerType) ----------------------

        private static float TypeAskShift(CustomerType type) => type switch
        {
            CustomerType.Haggler => 0.05f,
            CustomerType.Desperate => -0.05f,
            _ => 0f,
        };

        private static float TypeSettleShift(CustomerType type) => type switch
        {
            CustomerType.Haggler => 0.06f,
            CustomerType.Desperate => -0.06f,
            _ => 0f,
        };

        private static int TypeRoundsShift(CustomerType type) => type switch
        {
            CustomerType.Haggler => 1,
            CustomerType.HurryUp => -1,
            _ => 0,
        };
    }
}
