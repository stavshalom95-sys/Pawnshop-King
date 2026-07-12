using System;
using System.Collections.Generic;
using UnityEngine;

namespace PawnshopKing.Data.Definitions
{
    /// <summary>
    /// Static, designer-authored definition of an item type (GDD 38.1).
    /// Runtime copies with rolled values live in ItemInstance — this asset is never mutated in play.
    /// </summary>
    [CreateAssetMenu(fileName = "Item_", menuName = "Pawnshop King/Definitions/Item")]
    public class ItemDefinition : ScriptableObject
    {
        [Header("Identity (GDD 10.2)")]
        public string id;
        public string displayName;
        [TextArea] public string description;
        public string brandOrMaker;
        public string era;
        public ItemCategory category;

        [Header("Economics")]
        [Min(0)] public int baseValueMin;
        [Min(0)] public int baseValueMax;
        public RarityTier rarityTier = RarityTier.Common;

        [Header("Risk (chance rolled at generation)")]
        [Range(0f, 1f)] public float fakeChance;
        [Range(0f, 1f)] public float stolenChance;

        [Header("States & Tags")]
        public List<ConditionState> possibleConditions = new List<ConditionState>();
        public List<string> possibleTags = new List<string>();
        public List<string> collectorTags = new List<string>();
        public List<RepairProfile> repairProfiles = new List<RepairProfile>();

        [Header("Presentation")]
        public Sprite icon;
    }

    /// <summary>One repairable flaw this item type can spawn with, and what fixing it costs/returns (GDD 20).</summary>
    [Serializable]
    public class RepairProfile
    {
        public ConditionState repairsFrom = ConditionState.Damaged;
        public ConditionState repairsTo = ConditionState.Clean;
        [Min(0)] public int repairCost;
        [Min(0)] public int repairDays;
    }
}
