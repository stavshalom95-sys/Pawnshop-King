using System;
using System.Collections.Generic;
using PawnshopKing.Systems.Localization;
using UnityEngine;

namespace PawnshopKing.Data.Definitions
{
    /// <summary>
    /// Static, designer-authored definition of a customer archetype (GDD 38.1, 14.2).
    /// Concrete visitors are rolled into CustomerInstance — this asset is never mutated in play.
    /// MVP needs 4-6 of these: Desperate Seller, Casual Seller, Amateur Scammer, Fence (GDD 30).
    /// </summary>
    [CreateAssetMenu(fileName = "Customer_", menuName = "Pawnshop King/Definitions/Customer Archetype")]
    public class CustomerArchetypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [Header("Hebrew localization (falls back to English when empty/short)")]
        public string displayNameHe;
        [TextArea] public List<string> dialoguePoolHe = new List<string>();
        [TextArea] public List<string> originHintsHe = new List<string>();

        public string LocalizedDisplayName =>
            LanguageManager.IsRtl && !string.IsNullOrEmpty(displayNameHe) ? displayNameHe : displayName;

        public List<string> LocalizedDialoguePool =>
            LanguageManager.IsRtl && dialoguePoolHe.Count > 0 ? dialoguePoolHe : dialoguePool;

        public List<string> LocalizedOriginHints =>
            LanguageManager.IsRtl && originHintsHe.Count > 0 ? originHintsHe : originHints;

        [Header("Personality ranges (rolled per visit, GDD 14.1)")]
        public StatRange patienceRange = StatRange.Default;
        public StatRange desperationRange = StatRange.Default;
        public StatRange honestyRange = StatRange.Default;
        public StatRange greedRange = StatRange.Default;

        [Header("Items")]
        [Tooltip("Categories this archetype can bring to sell.")]
        public List<ItemCategory> possibleItemCategories = new List<ItemCategory>();
        [Tooltip("Categories this archetype favors when buying or valuing.")]
        public List<ItemCategory> categoryPreferences = new List<ItemCategory>();

        [Header("Risk profile (GDD 14.1)")]
        public CustomerRiskProfile riskProfile = new CustomerRiskProfile();

        [Header("Dialogue")]
        [TextArea] public List<string> dialoguePool = new List<string>();

        [Tooltip("Seller behavior that betrays an item's true origin during early inspection passes — only shown when the item really is fake/stolen and this archetype's risk profile leans that way (GDD 17, 25).")]
        [TextArea] public List<string> originHints = new List<string>();
    }

    /// <summary>Inclusive 0-1 range a personality stat is rolled from at generation.</summary>
    [Serializable]
    public struct StatRange
    {
        [Range(0f, 1f)] public float min;
        [Range(0f, 1f)] public float max;

        public static StatRange Default => new StatRange { min = 0.25f, max = 0.75f };

        public float Roll() => UnityEngine.Random.Range(min, max);
    }

    /// <summary>
    /// How dangerous dealing with this archetype is. Modifiers are added on top of the
    /// item's own fake/stolen chances at generation (e.g. Amateur Scammer boosts fake,
    /// Fence boosts stolen and always costs heat — GDD 14.2, 18).
    /// </summary>
    [Serializable]
    public class CustomerRiskProfile
    {
        [Range(0f, 1f)] public float fakeChanceBonus;
        [Range(0f, 1f)] public float stolenChanceBonus;
        [Min(0)] public int heatPerDeal;
    }
}
