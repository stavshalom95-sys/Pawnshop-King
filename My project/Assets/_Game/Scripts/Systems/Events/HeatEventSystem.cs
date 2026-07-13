using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Runtime;
using UnityEngine;

namespace PawnshopKing.Systems.Events
{
    /// <summary>What the heat clock did when the day closed — read by the day summary.</summary>
    public struct HeatEventResult
    {
        public bool occurred;
        public int itemsSeized;
        public bool blackMarketRaided;
        /// <summary>Human-readable summary line for the day summary screen.</summary>
        public string message;
    }

    /// <summary>
    /// Police / heat events (GDD 25, 18.2): once heat climbs past the threshold,
    /// each night risks a crackdown. A police visit confiscates every stolen item
    /// at zero compensation; a black market raid shuts that sell channel down for
    /// a few days. Both bleed heat off afterwards — the attention has been spent.
    /// All numbers are tuning values; the GDD prescribes the pressure, not the rates.
    /// </summary>
    public static class HeatEventSystem
    {
        public const int HeatThreshold = 50;
        private const float BaseEventChance = 0.25f;
        private const float ChancePerHeatPoint = 0.01f;    // +1% per point above threshold
        public const int PoliceHeatRelief = 20;
        public const int RaidHeatRelief = 10;
        public const int RaidShutdownDays = 3;

        public static HeatEventResult ProcessEndOfDay(GameState state)
        {
            var result = new HeatEventResult();
            if (state.heat <= HeatThreshold) return result;

            float chance = Mathf.Clamp01(BaseEventChance + (state.heat - HeatThreshold) * ChancePerHeatPoint);
            if (Random.value >= chance) return result;

            result.occurred = true;
            if (Random.value < 0.5f) PoliceVisit(state, ref result);
            else BlackMarketRaid(state, ref result);
            return result;
        }

        /// <summary>Officers sweep the shop and seize all stolen goods — no compensation (GDD 18.2).</summary>
        private static void PoliceVisit(GameState state, ref HeatEventResult result)
        {
            var seized = new List<ItemInstance>();
            foreach (var item in state.inventory)
            {
                if (item.stolenState != StolenState.Clean) seized.Add(item);
            }

            foreach (var item in seized) state.inventory.Remove(item);
            result.itemsSeized = seized.Count;
            state.heat = Mathf.Max(0, state.heat - PoliceHeatRelief);

            if (seized.Count > 0)
            {
                state.reputation -= 2;
                result.message = $"POLICE VISIT — Officers swept the shop and seized {seized.Count} stolen item{(seized.Count == 1 ? "" : "s")}. No compensation, no apology. (Reputation -2, Heat -{PoliceHeatRelief})";
            }
            else
            {
                result.message = $"POLICE VISIT — Officers turned the shop over and left empty-handed. For now, you're clean. (Heat -{PoliceHeatRelief})";
            }
        }

        /// <summary>The fence network goes dark: no black market sales until the closure ticks out.</summary>
        private static void BlackMarketRaid(GameState state, ref HeatEventResult result)
        {
            result.blackMarketRaided = true;
            state.blackMarketClosedDays = RaidShutdownDays;
            state.heat = Mathf.Max(0, state.heat - RaidHeatRelief);
            result.message = $"BLACK MARKET RAID — The fence's network went dark overnight. No black market sales for {RaidShutdownDays} days. (Heat -{RaidHeatRelief})";
        }
    }
}
