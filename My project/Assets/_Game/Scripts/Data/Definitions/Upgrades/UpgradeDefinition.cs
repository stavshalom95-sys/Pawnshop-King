using PawnshopKing.Systems.Localization;
using UnityEngine;

namespace PawnshopKing.Data.Definitions
{
    /// <summary>Which piece of shop math an upgrade improves (GDD 23.1 Tools).</summary>
    public enum UpgradeEffect
    {
        /// <summary>Additive bonus to the chance a condition read succeeds (Magnifier).</summary>
        ConditionAccuracy,

        /// <summary>Additive bonus to the chance a counterfeit shows a tell (UV Light).</summary>
        FakeDetection,

        /// <summary>0-1 fraction the value-estimate band is tightened by (Reference Guide).</summary>
        ValueAccuracy
    }

    /// <summary>
    /// Static, designer-authored definition of a purchasable shop upgrade (GDD 23.1).
    /// Ownership is tracked by id in ShopState.installedToolIds — this asset is never
    /// mutated in play. The effect/magnitude pair keeps the math data-driven: systems
    /// sum magnitudes of owned upgrades per effect instead of hardcoding tool checks.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade_", menuName = "Pawnshop King/Definitions/Upgrade")]
    public class UpgradeDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;
        [TextArea] public string description;

        [Header("Hebrew localization (falls back to English above when empty)")]
        public string displayNameHe;
        [TextArea] public string descriptionHe;

        public string LocalizedDisplayName =>
            LanguageManager.IsRtl && !string.IsNullOrEmpty(displayNameHe) ? displayNameHe : displayName;

        public string LocalizedDescription =>
            LanguageManager.IsRtl && !string.IsNullOrEmpty(descriptionHe) ? descriptionHe : description;

        [Header("Shop")]
        [Min(0)] public int cost;

        [Header("Effect")]
        public UpgradeEffect effect;
        [Range(0f, 1f)] public float magnitude;
    }
}
