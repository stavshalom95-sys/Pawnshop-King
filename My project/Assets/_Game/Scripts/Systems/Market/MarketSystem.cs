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

    public struct SellReceipt
    {
        public bool sold;
        public int price;
        public string message;
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
                return new SellReceipt { sold = false, message = "No buyer wants this through that channel." };
            }

            state.inventory.Remove(item);
            state.cash += quote.price;

            var definition = ItemGenerator.GetDefinition(item.definitionId);
            string name = definition != null ? definition.displayName : "Item";
            string message = $"{name} sold for ${quote.price:N0}.{Consequences(state, item, channel)}";

            return new SellReceipt { sold = true, price = quote.price, message = message };
        }

        private static string Consequences(GameState state, ItemInstance item, SellChannel channel)
        {
            bool fake = item.authenticity == Authenticity.Counterfeit;
            bool hot = item.stolenState != StolenState.Clean;

            switch (channel)
            {
                case SellChannel.Collector:
                    if (fake) { state.reputation -= 2; return " The collector is furious — it was a fake. (Reputation -2)"; }
                    if (hot) { state.heat += 2; return " The collector asks pointed questions about provenance. (Heat +2)"; }
                    return " A satisfied collector spreads the word.";

                case SellChannel.BlackMarket:
                    if (hot)
                    {
                        // "May reduce heat if moved out quickly" (GDD 15.1).
                        state.heat = Mathf.Max(0, state.heat - 1);
                        return " Moved quietly out of town, no questions asked. (Heat -1)";
                    }
                    state.heat += 1;
                    return " The fence's circle takes note of your business. (Heat +1)";

                default: // Shopfront
                    if (fake) { state.reputation -= 1; return " An angry buyer brought it back as counterfeit. (Reputation -1)"; }
                    if (hot) { state.heat += 3; return " Someone recognized it in the window — police are asking around. (Heat +3)"; }
                    return string.Empty;
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
