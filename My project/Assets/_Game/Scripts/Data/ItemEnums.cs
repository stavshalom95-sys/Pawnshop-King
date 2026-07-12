using System;

namespace PawnshopKing.Data
{
    /// <summary>MVP item categories (GDD 10.1).</summary>
    public enum ItemCategory
    {
        Watches,
        Jewelry,
        Electronics,
        MusicalInstruments,
        RetroCollectibles,
        AntiquesCurios,
        LuxuryAccessories,
        ToolsPracticalGoods
    }

    public enum RarityTier
    {
        Common,
        Uncommon,
        Rare,
        Exceptional,
        Legendary
    }

    /// <summary>Physical condition. MVP only needs Clean vs Damaged (GDD 30); the extra grades give repair headroom (GDD 20).</summary>
    public enum ConditionState
    {
        Pristine,
        Clean,
        Worn,
        Damaged,
        Broken
    }

    /// <summary>
    /// The item's TRUE authenticity. What the player believes is tracked separately
    /// via KnowledgeFlags — true state and player knowledge must never be conflated (GDD 11).
    /// </summary>
    public enum Authenticity
    {
        Authentic,
        Counterfeit
    }

    /// <summary>Stolen-goods risk levels (GDD 18.1).</summary>
    public enum StolenState
    {
        Clean,
        Suspicious,
        LikelyStolen,
        ConfirmedStolen,
        Hot
    }

    /// <summary>Restoration history — a bad repair can hurt antique value (GDD 10.3, 20.4).</summary>
    public enum RepairState
    {
        Original,
        Restored,
        PoorlyRestored
    }

    public enum AcquisitionSource
    {
        CustomerPurchase,
        Fence,
        Event,
        StartingStock
    }

    /// <summary>
    /// What the player has actually learned about an item instance —
    /// the PlayerKnowledgeState half of the knowledge model (GDD 9, 11).
    /// </summary>
    [Flags]
    public enum KnowledgeFlags
    {
        None = 0,
        ConditionAssessed = 1 << 0,
        AuthenticityKnown = 1 << 1,
        StolenStatusKnown = 1 << 2,
        ValueAppraised = 1 << 3,
        CollectorTagsKnown = 1 << 4
    }
}
