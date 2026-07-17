using PawnshopKing.Data;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Items;
using UnityEngine;

namespace PawnshopKing.Systems.Market
{
    public struct ChannelQuote
    {
        public bool available;
        public int price;
    }

    public enum SellOutcome
    {
        Unavailable,
        NotOwned,
        Sold,
    }

    /// <summary>Non-price consequence of a sale — the UI composes localized text from this, never a baked English string.</summary>
    public enum SellConsequence
    {
        None,
        CollectorFakeAngry,
        CollectorHotQuestions,
        CollectorSatisfied,
        BlackMarketHotMoved,
        BlackMarketCleanNoted,
        ShopfrontFakeReturned,
        ShopfrontHotRecognized,
    }

    public struct SellReceipt
    {
        public SellOutcome outcome;
        public int price;
        public SellConsequence consequence;
        public bool Sold => outcome == SellOutcome.Sold;
    }

    /// <summary>
    /// Calculates sale values by channel and executes sales (GDD 39). Channel logic
    /// follows GDD 15.1/15.2: shopfront is safe and steady, collectors pay a premium
    /// for tagged items, and the black market is where hot goods actually belong —
    /// it out-pays the shopfront for stolen items and bleeds heat off, but taking
    /// clean goods there costs heat and pays poorly.
    /// </summary>
    public static class MarketSystem
    {
        private const float ShopfrontFactor = 0.9f;
        private const float CollectorFactor = 1.5f;
        private const float BlackMarketHotFactor = 1.2f;
        private const float BlackMarketCleanFactor = 0.55f;

        // Legit buyers spot fakes at the counter; the fence moves them knowingly.
        private const float FakeLegitFactor = 0.15f;
        private const float FakeBlackMarketFactor = 0.35f;

        public static ChannelQuote GetQuote(GameState state, ItemInstance item, SellChannel channel)
        {
            bool fake = item.authenticity == Authenticity.Counterfeit;
            bool hot = item.stolenState != StolenState.Clean;
            float value = item.rolledBaseValue * ConditionMultiplier(item.condition);

            float price;
            switch (channel)
            {
                case SellChannel.Collector:
                    if (item.collectorTags.Count == 0) return new ChannelQuote { available = false };
                    price = value * (fake ? FakeLegitFactor : CollectorFactor);
                    break;

                case SellChannel.BlackMarket:
                    // The fence network stays dark while a raid closure ticks out (GDD 25).
                    if (state != null && state.blackMarketClosedDays > 0) return new ChannelQuote { available = false };
                    if (fake) price = value * FakeBlackMarketFactor;
                    else price = value * (hot ? BlackMarketHotFactor : BlackMarketCleanFactor);
                    break;

                default: // Shopfront
                    price = value * (fake ? FakeLegitFactor : ShopfrontFactor);
                    break;
            }

            return new ChannelQuote { available = true, price = Mathf.Max(5, Mathf.RoundToInt(price / 5f) * 5) };
        }

        /// <summary>Removes the item from inventory, pays out, and applies channel consequences.</summary>
        public static SellReceipt Sell(GameState state, ItemInstance item, SellChannel channel)
        {
            var quote = GetQuote(state, item, channel);
            if (!quote.available)
            {
                return new SellReceipt { outcome = SellOutcome.Unavailable };
            }

            // Ownership gate: a stale UI row (item seized overnight, double-fired
            // click) must never pay out for goods that already left the shelves.
            if (!state.inventory.Remove(item))
            {
                return new SellReceipt { outcome = SellOutcome.NotOwned };
            }

            state.cash += quote.price;
            var consequence = ApplyConsequences(state, item, channel);

            return new SellReceipt { outcome = SellOutcome.Sold, price = quote.price, consequence = consequence };
        }

        private static SellConsequence ApplyConsequences(GameState state, ItemInstance item, SellChannel channel)
        {
            bool fake = item.authenticity == Authenticity.Counterfeit;
            bool hot = item.stolenState != StolenState.Clean;

            switch (channel)
            {
                case SellChannel.Collector:
                    if (fake) { state.reputation -= 2; return SellConsequence.CollectorFakeAngry; }
                    if (hot) { state.heat += 2; return SellConsequence.CollectorHotQuestions; }
                    return SellConsequence.CollectorSatisfied;

                case SellChannel.BlackMarket:
                    if (hot)
                    {
                        // "May reduce heat if moved out quickly" (GDD 15.1).
                        state.heat = Mathf.Max(0, state.heat - 1);
                        return SellConsequence.BlackMarketHotMoved;
                    }
                    state.heat += 1;
                    return SellConsequence.BlackMarketCleanNoted;

                default: // Shopfront
                    if (fake) { state.reputation -= 1; return SellConsequence.ShopfrontFakeReturned; }
                    if (hot) { state.heat += 3; return SellConsequence.ShopfrontHotRecognized; }
                    return SellConsequence.None;
            }
        }

        private static float ConditionMultiplier(ConditionState condition)
        {
            switch (condition)
            {
                case ConditionState.Pristine: return 1.15f;
                case ConditionState.Clean: return 1f;
                case ConditionState.Worn: return 0.8f;
                case ConditionState.Damaged: return 0.55f;
                case ConditionState.Broken: return 0.3f;
                default: return 1f;
            }
        }
    }
}
