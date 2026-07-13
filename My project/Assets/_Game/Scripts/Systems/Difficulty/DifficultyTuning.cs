using PawnshopKing.Data;

namespace PawnshopKing.Systems.DifficultyTier
{
    /// <summary>
    /// The live difficulty knobs. Source of truth is GameState.difficulty (so it
    /// saves with the campaign); GameManager mirrors it here on new game / load /
    /// settings change so the static systems can read it without plumbing state
    /// through every call chain.
    /// Easy: sellers ask less and settle sooner, inspection reads more generous.
    /// Hard: sellers ask more and hold their line, inspection stingier.
    /// </summary>
    public static class DifficultyTuning
    {
        public static Difficulty Current = Difficulty.Easy;

        /// <summary>Added to the seller's asking-price factor (negative = cheaper items).</summary>
        public static float AskFactorShift => Current == Difficulty.Easy ? -0.08f : 0.08f;

        /// <summary>Added to the fraction of the ask a seller quietly settles for (negative = accepts lower offers).</summary>
        public static float SettleLineShift => Current == Difficulty.Easy ? -0.08f : 0.06f;

        /// <summary>Added to the pass-1 condition-read chance.</summary>
        public static float ConditionReadBonus => Current == Difficulty.Easy ? 0.2f : -0.1f;

        /// <summary>Base tightening of the value-estimate band (stacks with the Reference Guide).</summary>
        public static float ValueBandTighten => Current == Difficulty.Easy ? 0.3f : 0f;
    }
}
