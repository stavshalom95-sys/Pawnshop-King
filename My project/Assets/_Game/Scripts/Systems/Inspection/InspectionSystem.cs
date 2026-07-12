using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Upgrades;
using UnityEngine;

namespace PawnshopKing.Systems.Inspection
{
    /// <summary>
    /// Processes inspection actions, reveals clues, and updates player knowledge
    /// (GDD 39). Passes are staged — condition read first, value estimate second,
    /// risk tells last — and risk tells are probabilistic: clues push judgment,
    /// never certainty (GDD 12.3). Tool upgrades (GDD 23.1) shift the odds:
    /// Magnifier raises the condition-read chance, UV Light raises the fake-tell
    /// chance, and the Reference Guide tightens the value band. The
    /// AuthenticityKnown / StolenStatusKnown flags are reserved for paid
    /// authentication services in a later phase.
    /// </summary>
    public static class InspectionSystem
    {
        // "A few actions and clues, not 20 screens" (GDD 12.1).
        public const int MaxInspectionsPerItem = 3;

        // Base odds before tool upgrades; UpgradeSystem bonuses stack on top.
        private const float BaseConditionReadChance = 0.7f;
        private const float BaseFakeTellChance = 0.65f;
        private const float StolenTellChance = 0.6f;

        // Inspection events (GDD 17, 25): while you handle the item, the seller's
        // behavior sometimes betrays its true origin before any risk tell is rolled.
        private const float OriginHintChance = 0.35f;
        private const float RiskyArchetypeThreshold = 0.2f;

        // Untooled value estimate band around the true roll (GDD 12.2 Closer Look).
        private const float ValueBandLow = 0.8f;
        private const float ValueBandHigh = 1.25f;

        // Clue pools straight from GDD 12.3.
        private static readonly string[] FakeTells =
        {
            "Logo engraving is slightly off-center.",
            "Gold weight seems low.",
            "Packaging is suspiciously perfect for its age.",
            "The finish looks far too fresh for the claimed age.",
        };

        private static readonly string[] StolenTells =
        {
            "Serial sticker looks recently replaced.",
            "Back panel screws were recently opened.",
            "The seller avoids eye contact when you ask about its origin.",
            "Engraved initials have been buffed out.",
        };

        private static readonly string[] NothingTells =
        {
            "Nothing else jumps out at you.",
            "Honest wear, consistent with its age.",
            "It smells faintly of smoke and basement.",
        };

        // A failed condition read: the pass is spent but the grade stays unknown.
        private static readonly string[] ConditionMisses =
        {
            "Hard to get a good read in this light — a magnifier would help.",
            "The wear could be surface-level, or could run deep. You can't tell.",
            "You squint at it, but can't call its condition either way.",
        };

        public static bool CanInspect(ItemInstance item) => item.timesInspected < MaxInspectionsPerItem;

        public static int InspectionsLeft(ItemInstance item) => MaxInspectionsPerItem - item.timesInspected;

        /// <summary>
        /// Runs one inspection pass and returns the clues it revealed. Pass the selling
        /// customer's archetype when inspecting at the counter — early passes may then
        /// surface an origin hint from the seller's behavior (GDD 17, 25).
        /// </summary>
        public static List<string> Inspect(GameState state, ItemInstance item, CustomerArchetypeDefinition archetype = null)
        {
            var revealed = new List<string>();
            if (!CanInspect(item)) return revealed;
            item.timesInspected++;

            // Pass 1 — visual check: obvious damage and wear (GDD 12.2 Visual Check).
            // A miss burns the pass without the read — the Magnifier buys certainty.
            if (!item.playerKnowledge.HasFlag(KnowledgeFlags.ConditionAssessed))
            {
                float readChance = Mathf.Clamp01(
                    BaseConditionReadChance + UpgradeSystem.GetEffectBonus(state, UpgradeEffect.ConditionAccuracy));
                if (Random.value < readChance)
                {
                    item.playerKnowledge |= KnowledgeFlags.ConditionAssessed;
                    AddClue(item, revealed, ConditionClue(item.condition));
                }
                else
                {
                    AddClue(item, revealed, PickNew(ConditionMisses, item) ?? "Still no clear read on its condition.");
                }

                TryAddOriginHint(item, archetype, revealed);
                return revealed;
            }

            // Pass 2 — closer look: a rough value estimate, never the exact roll.
            if (!item.playerKnowledge.HasFlag(KnowledgeFlags.ValueAppraised))
            {
                item.playerKnowledge |= KnowledgeFlags.ValueAppraised;
                GetValueEstimate(state, item, out int low, out int high);
                AddClue(item, revealed, $"You'd put it somewhere around ${low:N0}–${high:N0}.");
                TryAddOriginHint(item, archetype, revealed);
                return revealed;
            }

            // Pass 3 — risk tells. A failed roll is indistinguishable from a clean item.
            float fakeTellChance = Mathf.Clamp01(
                BaseFakeTellChance + UpgradeSystem.GetEffectBonus(state, UpgradeEffect.FakeDetection));
            bool fakeTell = item.authenticity == Authenticity.Counterfeit && Random.value < fakeTellChance;
            bool stolenTell = item.stolenState != StolenState.Clean && Random.value < StolenTellChance;

            string clue = null;
            if (fakeTell && stolenTell) clue = Random.value < 0.5f ? PickNew(FakeTells, item) : PickNew(StolenTells, item);
            else if (fakeTell) clue = PickNew(FakeTells, item);
            else if (stolenTell) clue = PickNew(StolenTells, item);

            clue ??= PickNew(NothingTells, item) ?? "Nothing new catches your eye.";
            AddClue(item, revealed, clue);
            return revealed;
        }

        /// <summary>
        /// Deterministic rough band around the true roll, so re-reading the UI never
        /// re-rolls it. The Reference Guide tightens the band toward the truth.
        /// </summary>
        public static void GetValueEstimate(GameState state, ItemInstance item, out int low, out int high)
        {
            float tighten = Mathf.Clamp01(UpgradeSystem.GetEffectBonus(state, UpgradeEffect.ValueAccuracy));
            float lowFraction = 1f - (1f - ValueBandLow) * (1f - tighten);
            float highFraction = 1f + (ValueBandHigh - 1f) * (1f - tighten);

            low = Mathf.Max(5, Mathf.RoundToInt(item.rolledBaseValue * lowFraction / 10f) * 10);
            high = Mathf.Max(low + 10, Mathf.RoundToInt(item.rolledBaseValue * highFraction / 10f) * 10);
        }

        private static string ConditionClue(ConditionState condition)
        {
            switch (condition)
            {
                case ConditionState.Pristine: return "Untouched — barely a mark on it.";
                case ConditionState.Clean: return "Good shape overall.";
                case ConditionState.Worn: return "Real wear on the edges and corners.";
                case ConditionState.Damaged: return "Visible damage — it would need work.";
                case ConditionState.Broken: return "It doesn't work at all.";
                default: return "Condition noted.";
            }
        }

        /// <summary>
        /// Truth-gated archetype hint (GDD 17, 25): fires only when the item genuinely
        /// is fake/stolen AND the archetype's risk profile leans the same way, so a
        /// hint is information — but its absence never proves the item is clean.
        /// </summary>
        private static void TryAddOriginHint(ItemInstance item, CustomerArchetypeDefinition archetype, List<string> revealed)
        {
            if (archetype == null || archetype.originHints.Count == 0) return;
            if (Random.value >= OriginHintChance) return;

            bool stolenMatch = item.stolenState != StolenState.Clean
                && archetype.riskProfile.stolenChanceBonus >= RiskyArchetypeThreshold;
            bool fakeMatch = item.authenticity == Authenticity.Counterfeit
                && archetype.riskProfile.fakeChanceBonus >= RiskyArchetypeThreshold;
            if (!stolenMatch && !fakeMatch) return;

            string hint = PickNew(archetype.originHints, item);
            if (hint != null) AddClue(item, revealed, hint);
        }

        private static string PickNew(IReadOnlyList<string> pool, ItemInstance item)
        {
            var fresh = new List<string>();
            foreach (var clue in pool)
            {
                if (!item.knownClues.Contains(clue)) fresh.Add(clue);
            }

            return fresh.Count > 0 ? fresh[Random.Range(0, fresh.Count)] : null;
        }

        private static void AddClue(ItemInstance item, List<string> revealed, string clue)
        {
            item.knownClues.Add(clue);
            revealed.Add(clue);
        }
    }
}
