using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using UnityEngine;

namespace PawnshopKing.Systems.Items
{
    /// <summary>
    /// Rolls concrete ItemInstances from ItemDefinitions (GDD 39 "item generation").
    /// True states (authenticity, stolen) are rolled here and stay hidden — only
    /// inspection may move them into the player's knowledge (GDD 11).
    /// Definitions load from Resources (zero-editor-wiring), cached after first use.
    /// </summary>
    public static class ItemGenerator
    {
        public const string ItemsResourcePath = "Definitions/Items";

        // A customer occasionally brings a second item (GDD 41: "retro console bundle").
        private const float SecondItemChance = 0.25f;

        private static ItemDefinition[] allDefinitions;
        private static Dictionary<string, ItemDefinition> definitionsById;

        public static ItemDefinition GetDefinition(string definitionId)
        {
            EnsureLoaded();
            return definitionsById.TryGetValue(definitionId, out var def) ? def : null;
        }

        /// <summary>Rolls the item(s) a visitor carries, filtered by the archetype's categories and skewed by its risk profile.</summary>
        public static List<ItemInstance> GenerateItemsFor(CustomerArchetypeDefinition archetype)
        {
            EnsureLoaded();

            var candidates = FilterByCategories(archetype.possibleItemCategories);
            var items = new List<ItemInstance>();
            if (candidates.Count == 0)
            {
                Debug.LogWarning($"No item definitions match archetype '{archetype.id}' categories — customer arrives empty-handed.");
                return items;
            }

            int count = Random.value < SecondItemChance ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                var definition = candidates[Random.Range(0, candidates.Count)];
                items.Add(GenerateItem(definition, archetype.riskProfile));
            }

            return items;
        }

        /// <summary>Rolls one instance: value, condition, and the hidden fake/stolen truths (GDD 10.2, 10.3).</summary>
        public static ItemInstance GenerateItem(ItemDefinition definition, CustomerRiskProfile risk = null)
        {
            var item = ItemInstance.CreateNew(definition.id);

            item.rolledBaseValue = Random.Range(definition.baseValueMin, definition.baseValueMax + 1);

            item.condition = definition.possibleConditions.Count > 0
                ? definition.possibleConditions[Random.Range(0, definition.possibleConditions.Count)]
                : ConditionState.Clean;

            float fakeChance = Mathf.Clamp01(definition.fakeChance + (risk?.fakeChanceBonus ?? 0f));
            item.authenticity = Random.value < fakeChance ? Authenticity.Counterfeit : Authenticity.Authentic;

            float stolenChance = Mathf.Clamp01(definition.stolenChance + (risk?.stolenChanceBonus ?? 0f));
            item.stolenState = Random.value < stolenChance ? StolenState.ConfirmedStolen : StolenState.Clean;

            item.collectorTags.AddRange(definition.collectorTags);

            return item;
        }

        private static List<ItemDefinition> FilterByCategories(List<ItemCategory> categories)
        {
            var result = new List<ItemDefinition>();
            foreach (var definition in allDefinitions)
            {
                // An archetype with no category list can bring anything.
                if (categories.Count == 0 || categories.Contains(definition.category)) result.Add(definition);
            }

            return result;
        }

        private static void EnsureLoaded()
        {
            if (allDefinitions != null) return;

            allDefinitions = Resources.LoadAll<ItemDefinition>(ItemsResourcePath);
            definitionsById = new Dictionary<string, ItemDefinition>(allDefinitions.Length);
            foreach (var definition in allDefinitions) definitionsById[definition.id] = definition;

            if (allDefinitions.Length == 0)
            {
                Debug.LogWarning($"No ItemDefinition assets found under Resources/{ItemsResourcePath}.");
            }
        }
    }
}
