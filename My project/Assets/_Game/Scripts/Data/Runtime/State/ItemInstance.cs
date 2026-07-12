using System;
using System.Collections.Generic;

namespace PawnshopKing.Data.Runtime
{
    /// <summary>
    /// A concrete owned/offered item rolled from an ItemDefinition (GDD 38.2).
    /// Holds both the item's true state and, separately, what the player knows about it (GDD 11).
    /// </summary>
    [Serializable]
    public class ItemInstance
    {
        public string instanceId;
        public string definitionId;

        public int rolledBaseValue;

        // True states — hidden from the player until revealed through inspection,
        // tools, or authentication. UI must read playerKnowledge before showing these.
        public ConditionState condition = ConditionState.Clean;
        public Authenticity authenticity = Authenticity.Authentic;
        public StolenState stolenState = StolenState.Clean;
        public RepairState repairState = RepairState.Original;
        public List<string> collectorTags = new List<string>();

        // Player knowledge side of the knowledge model (GDD 9, 11).
        public KnowledgeFlags playerKnowledge = KnowledgeFlags.None;
        public List<string> knownClues = new List<string>();
        public int timesInspected;

        public int acquisitionPrice;
        public AcquisitionSource acquisitionSource;

        public static ItemInstance CreateNew(string definitionId)
        {
            return new ItemInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                definitionId = definitionId
            };
        }
    }
}
