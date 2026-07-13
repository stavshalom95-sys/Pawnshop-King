using System;
using System.Collections.Generic;

namespace PawnshopKing.Data.Runtime
{
    /// <summary>
    /// Root serializable runtime state for a campaign (GDD 38.2).
    /// This is the single object SaveLoadSystem persists — no gameplay state
    /// may live outside it, or it won't survive save/load (GDD 35, 39).
    /// </summary>
    [Serializable]
    public class GameState
    {
        public int currentDay = 1;
        public Difficulty difficulty = Difficulty.Easy;

        // Core resources (GDD 8.1)
        public int cash;
        public int reputation;
        public int heat;
        public DebtState debt = new DebtState();

        // Day-start snapshots so the day summary can show deltas (GDD 32.1 E).
        public int dayStartCash;
        public int dayStartReputation;
        public int dayStartHeat;

        public List<ItemInstance> inventory = new List<ItemInstance>();

        // World pressure from heat events (GDD 25): while > 0, the black market
        // channel is shut down. Ticks down one per day in DayManager.
        public int blackMarketClosedDays;

        // Referenced by id so definitions stay in ScriptableObjects (GDD 35)
        public List<string> activeEventIds = new List<string>();
        public List<string> unlockedUpgradeIds = new List<string>();
        public List<string> knownCollectorIds = new List<string>();

        public ShopState shop = new ShopState();
    }
}
