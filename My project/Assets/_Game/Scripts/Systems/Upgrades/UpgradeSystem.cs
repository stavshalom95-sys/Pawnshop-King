using System.Collections.Generic;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using UnityEngine;

namespace PawnshopKing.Systems.Upgrades
{
    /// <summary>One purchase attempt's outcome, with player-facing feedback text.</summary>
    public struct PurchaseResult
    {
        public bool success;
        public string message;
    }

    /// <summary>
    /// Loads UpgradeDefinitions from Resources (zero-editor-wiring, cached after first
    /// use), answers ownership/effect queries, and processes purchases (GDD 23.1).
    /// Ownership lives in ShopState.installedToolIds so it survives save/load (GDD 35);
    /// definitions stay in ScriptableObjects and are only referenced by id.
    /// </summary>
    public static class UpgradeSystem
    {
        public const string UpgradesResourcePath = "Definitions/Upgrades";

        private static UpgradeDefinition[] allDefinitions;
        private static Dictionary<string, UpgradeDefinition> definitionsById;

        public static IReadOnlyList<UpgradeDefinition> AllUpgrades
        {
            get
            {
                EnsureLoaded();
                return allDefinitions;
            }
        }

        public static UpgradeDefinition GetDefinition(string upgradeId)
        {
            EnsureLoaded();
            return definitionsById.TryGetValue(upgradeId, out var def) ? def : null;
        }

        public static bool IsOwned(GameState state, string upgradeId) =>
            state != null && state.shop.installedToolIds.Contains(upgradeId);

        /// <summary>
        /// Sum of owned-upgrade magnitudes for one effect. Systems add this on top of
        /// their base numbers, so new tool assets change the math with zero code edits.
        /// </summary>
        public static float GetEffectBonus(GameState state, UpgradeEffect effect)
        {
            if (state == null) return 0f;
            EnsureLoaded();

            float bonus = 0f;
            foreach (var definition in allDefinitions)
            {
                if (definition.effect == effect && IsOwned(state, definition.id)) bonus += definition.magnitude;
            }

            return bonus;
        }

        /// <summary>Buys the upgrade if it isn't owned and the cash covers it — debt money has a rival use now.</summary>
        public static PurchaseResult TryPurchase(GameState state, UpgradeDefinition definition)
        {
            if (IsOwned(state, definition.id))
            {
                return new PurchaseResult { success = false, message = $"You already own the {definition.displayName}." };
            }

            if (state.cash < definition.cost)
            {
                return new PurchaseResult
                {
                    success = false,
                    message = $"You can't afford the {definition.displayName} — it's ${definition.cost:N0} and you have ${state.cash:N0}."
                };
            }

            state.cash -= definition.cost;
            state.shop.installedToolIds.Add(definition.id);
            return new PurchaseResult { success = true, message = $"{definition.displayName} installed. ${definition.cost:N0} well spent — probably." };
        }

        private static void EnsureLoaded()
        {
            if (allDefinitions != null) return;

            allDefinitions = Resources.LoadAll<UpgradeDefinition>(UpgradesResourcePath);
            definitionsById = new Dictionary<string, UpgradeDefinition>(allDefinitions.Length);
            foreach (var definition in allDefinitions) definitionsById[definition.id] = definition;

            if (allDefinitions.Length == 0)
            {
                Debug.LogWarning($"No UpgradeDefinition assets found under Resources/{UpgradesResourcePath}.");
            }
        }
    }
}
