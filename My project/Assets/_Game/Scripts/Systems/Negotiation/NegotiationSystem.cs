using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
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
    /// (GDD 39). Bundle-level for MVP: one asking price covers everything on the
    /// counter, and closing a deal moves items into inventory and cash out.
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
            int totalValue = 0;
            foreach (var item in customer.items) totalValue += item.rolledBaseValue;
            if (totalValue <= 0) return 0;

            float factor = Mathf.Clamp(
                0.5f + 0.45f * customer.greed + 0.2f * (1f - customer.honesty) - 0.3f * customer.desperation,
                0.35f, 1.2f);

            return Mathf.Max(5, RoundToFive(totalValue * factor));
        }

        /// <summary>Player accepts the customer's current asking price (GDD 13.2).</summary>
        public static OfferResult BuyAtAskingPrice(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype)
        {
            return CloseDeal(state, customer, archetype, customer.askingPrice, OfferOutcome.Accepted);
        }

        /// <summary>
        /// Resolves one player offer into the GDD 13.5 outcomes: accept, counter,
        /// offended walk-out, desperate capitulation, or fed-up exit.
        /// </summary>
        public static OfferResult MakeOffer(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype, int offer)
        {
            customer.offersMade++;

            if (offer >= customer.askingPrice)
            {
                return CloseDeal(state, customer, archetype, offer, OfferOutcome.Accepted);
            }

            float ratio = offer / (float)customer.askingPrice;

            // The fraction of the current ask they'd quietly settle for.
            float settleLine = Mathf.Clamp(0.9f - 0.4f * customer.desperation + 0.25f * customer.greed, 0.5f, 1f);
            if (ratio >= settleLine && Random.value < 0.65f + (ratio - settleLine) * 2f)
            {
                return CloseDeal(state, customer, archetype, offer, OfferOutcome.Accepted);
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

            // Patience budget: 1-4 rounds of haggling before they're done.
            int maxRounds = 1 + Mathf.RoundToInt(customer.patience * 3f);
            if (customer.offersMade >= maxRounds)
            {
                if (customer.desperation > 0.55f && ratio >= insultLine)
                {
                    // "Customer lowers demand if desperate" (GDD 13.5).
                    customer.mood = CustomerMood.Desperate;
                    return CloseDeal(state, customer, archetype, offer, OfferOutcome.AcceptedReluctantly);
                }

                customer.mood = CustomerMood.Impatient;
                customer.negotiationState = NegotiationState.CustomerLeft;
                return new OfferResult { outcome = OfferOutcome.GaveUpLeft };
            }

            // Too close to split hairs over — take the offer.
            if (customer.askingPrice - offer <= 10)
            {
                return CloseDeal(state, customer, archetype, offer, OfferOutcome.Accepted);
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

        private static OfferResult CloseDeal(GameState state, CustomerInstance customer, CustomerArchetypeDefinition archetype, int price, OfferOutcome outcome)
        {
            // Split the bundle price across items by value share, so each inventory
            // entry knows what it effectively cost (GDD 38.2 acquisition price).
            int totalValue = 0;
            foreach (var item in customer.items) totalValue += item.rolledBaseValue;

            int remaining = price;
            for (int i = 0; i < customer.items.Count; i++)
            {
                var item = customer.items[i];
                bool last = i == customer.items.Count - 1;
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
            customer.items.Clear();

            return new OfferResult { outcome = outcome, price = price };
        }

        private static int RoundToFive(float value) => Mathf.RoundToInt(value / 5f) * 5;
    }
}
