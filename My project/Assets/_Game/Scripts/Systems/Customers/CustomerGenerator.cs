using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Items;
using PawnshopKing.Systems.Negotiation;

namespace PawnshopKing.Systems.Customers
{
    /// <summary>
    /// Rolls concrete CustomerInstances from archetype definitions (GDD 39: customer
    /// generation), including the items they walk in with (GDD 39: "picks items based
    /// on archetype").
    /// </summary>
    public static class CustomerGenerator
    {
        public static CustomerInstance Generate(CustomerArchetypeDefinition archetype)
        {
            var customer = CustomerInstance.CreateNew(archetype.id);

            customer.patience = archetype.patienceRange.Roll();
            customer.desperation = archetype.desperationRange.Roll();
            customer.honesty = archetype.honestyRange.Roll();
            customer.greed = archetype.greedRange.Roll();

            customer.mood = RollStartingMood(customer);
            customer.items.AddRange(ItemGenerator.GenerateItemsFor(archetype));
            customer.askingPrice = NegotiationSystem.CalculateAskingPrice(customer);

            return customer;
        }

        // High desperation walks in the door visibly desperate; low patience reads
        // as impatient. Everyone else starts neutral and reacts to the negotiation (GDD 13.5).
        private static CustomerMood RollStartingMood(CustomerInstance customer)
        {
            if (customer.desperation >= 0.7f) return CustomerMood.Desperate;
            if (customer.patience <= 0.3f) return CustomerMood.Impatient;
            return CustomerMood.Neutral;
        }
    }
}
