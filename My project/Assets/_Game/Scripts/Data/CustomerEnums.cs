using System;

namespace PawnshopKing.Data
{
    /// <summary>
    /// Customer's current emotional state during a visit. Drives negotiation
    /// behavior and dialogue tone (GDD 13.5, 14.2).
    /// </summary>
    public enum CustomerMood
    {
        Neutral,
        Friendly,
        Nervous,
        Impatient,
        Desperate,
        Offended
    }

    /// <summary>
    /// A per-visit negotiation "flavor," rolled independently of the archetype's
    /// hidden stats — a visible-to-the-player signal (unlike patience/desperation/
    /// greed, which never surface directly) so haggling has a legible read.
    /// </summary>
    public enum CustomerType
    {
        Haggler,   // Asks more, settles for more, haggles longer — the hard sell.
        Desperate, // Asks less, settles for less — the easy bargain.
        HurryUp    // Wants it over fast — burns through patience quicker.
    }

    /// <summary>Where a customer's visit currently stands (GDD 13.5, 38.2).</summary>
    public enum NegotiationState
    {
        NotStarted,
        InProgress,
        Accepted,
        Rejected,
        CustomerLeft
    }

    /// <summary>
    /// What a recurring customer remembers about past treatment (GDD 14.3).
    /// Only meaningful on customers flagged as recurring — one-off visitors never read these.
    /// </summary>
    [Flags]
    public enum CustomerMemoryFlags
    {
        None = 0,
        WasHumiliated = 1 << 0,
        TreatedFairly = 1 << 1,
        CaughtLying = 1 << 2,
        WasHelped = 1 << 3,
        WasReported = 1 << 4,
        LowballedTooHard = 1 << 5
    }
}
