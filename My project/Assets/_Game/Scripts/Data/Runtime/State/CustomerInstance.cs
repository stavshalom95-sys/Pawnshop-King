using System;
using System.Collections.Generic;

namespace PawnshopKing.Data.Runtime
{
    /// <summary>
    /// A concrete visitor rolled from a CustomerArchetypeDefinition (GDD 38.2).
    /// Personality stats are fixed for the visit; mood and negotiation state
    /// change as the encounter plays out (GDD 13, 14).
    /// </summary>
    [Serializable]
    public class CustomerInstance
    {
        public string instanceId;
        public string archetypeId;

        // Generated stats, rolled 0-1 from the archetype's ranges at spawn.
        public float patience;
        public float desperation;
        public float honesty;
        public float greed;

        public CustomerMood mood = CustomerMood.Neutral;
        public NegotiationState negotiationState = NegotiationState.NotStarted;
        public CustomerType customerType;

        // Bundle price for everything they carry; counters rewrite it as talks go on.
        public int askingPrice;
        public int offersMade;

        public List<ItemInstance> items = new List<ItemInstance>();

        // Only used when this customer is a recurring one (GDD 14.3).
        public bool isRecurring;
        public CustomerMemoryFlags memoryFlags = CustomerMemoryFlags.None;

        public static CustomerInstance CreateNew(string archetypeId)
        {
            return new CustomerInstance
            {
                instanceId = Guid.NewGuid().ToString("N"),
                archetypeId = archetypeId
            };
        }
    }
}
