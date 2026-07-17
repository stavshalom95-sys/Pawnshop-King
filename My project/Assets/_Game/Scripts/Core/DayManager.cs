using System;
using System.Collections.Generic;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Customers;
using PawnshopKing.Systems.Debt;
using PawnshopKing.Systems.Events;
using UnityEngine;

namespace PawnshopKing.Core
{
    /// <summary>
    /// Runs one shop day: start-of-day setup, customer queue generation, and
    /// end-of-day resolution (GDD 39). Daily expense processing plugs in here
    /// once DebtSystem/EconomySystem exist (Phase 3).
    /// </summary>
    public class DayManager : MonoBehaviour
    {
        [Header("Customer queue (4-6 visitors per day, GDD 40)")]
        [SerializeField] private List<CustomerArchetypeDefinition> availableArchetypes = new List<CustomerArchetypeDefinition>();
        [SerializeField, Min(1)] private int minCustomersPerDay = 4;
        [SerializeField, Min(1)] private int maxCustomersPerDay = 6;

        /// <summary>Where archetype assets live, relative to a Resources folder (zero-editor-wiring workflow).</summary>
        public const string ArchetypesResourcePath = "Definitions/Customers";

        private readonly Queue<CustomerInstance> customerQueue = new Queue<CustomerInstance>();

        public int CustomersRemaining => customerQueue.Count;
        public CustomerInstance CurrentCustomer { get; private set; }

        /// <summary>What the debt clock did when the last day closed — read by the day summary.</summary>
        public DebtTickResult LastDebtResult { get; private set; }

        /// <summary>What the heat clock did when the last day closed — read by the day summary.</summary>
        public HeatEventResult LastHeatEvent { get; private set; }

        public event Action<int> DayStarted;
        public event Action<CustomerInstance> CustomerArrived;
        public event Action<int> DayEnded;

        /// <summary>
        /// Fills the archetype list from Resources so no inspector wiring is needed.
        /// Runtime-safe (works in builds), unlike AssetDatabase — but assets must live
        /// under Assets/_Game/Resources/{ArchetypesResourcePath}.
        /// </summary>
        public void LoadArchetypes()
        {
            availableArchetypes.Clear();
            availableArchetypes.AddRange(Resources.LoadAll<CustomerArchetypeDefinition>(ArchetypesResourcePath));

            if (availableArchetypes.Count == 0)
            {
                Debug.LogWarning($"No CustomerArchetypeDefinition assets found under Resources/{ArchetypesResourcePath}.");
            }
        }

        /// <summary>Resolves an instance's archetypeId back to its definition (UI display, item generation).</summary>
        public CustomerArchetypeDefinition GetArchetype(string archetypeId)
        {
            for (int i = 0; i < availableArchetypes.Count; i++)
            {
                if (availableArchetypes[i].id == archetypeId) return availableArchetypes[i];
            }

            return null;
        }

        /// <summary>Sets up the morning: rolls today's customer queue and announces the day (GDD 41 day start).</summary>
        public void StartDay(GameState state)
        {
            CurrentCustomer = null;

            // Self-heal: an unwired (or hand-placed but empty) DayManager loads its own content.
            if (availableArchetypes.Count == 0) LoadArchetypes();

            BuildCustomerQueue(state.reputation);
            DayStarted?.Invoke(state.currentDay);
        }

        /// <summary>
        /// Brings the next customer to the counter. Returns null when the queue is
        /// empty — the caller should then move to EndDay.
        /// </summary>
        public CustomerInstance NextCustomer()
        {
            CurrentCustomer = customerQueue.Count > 0 ? customerQueue.Dequeue() : null;
            if (CurrentCustomer != null)
            {
                CurrentCustomer.negotiationState = NegotiationState.InProgress;
                CustomerArrived?.Invoke(CurrentCustomer);
            }

            return CurrentCustomer;
        }

        /// <summary>End-of-day resolution (GDD 41 end of day): debt clock, payments, their teeth — then heat's turn.</summary>
        public void EndDay(GameState state)
        {
            customerQueue.Clear();
            CurrentCustomer = null;

            // Tick an existing black market closure before tonight's events, so a
            // fresh raid always buys its full shutdown (GDD 25).
            if (state.blackMarketClosedDays > 0) state.blackMarketClosedDays--;

            LastDebtResult = DebtSystem.ProcessEndOfDay(state);
            LastHeatEvent = LastDebtResult.gameOver ? default : HeatEventSystem.ProcessEndOfDay(state);

            DayEnded?.Invoke(state.currentDay);
        }

        private void BuildCustomerQueue(int reputation)
        {
            customerQueue.Clear();

            if (availableArchetypes.Count == 0)
            {
                Debug.LogWarning("DayManager has no customer archetypes assigned — the day will have an empty queue.");
                return;
            }

            int count = UnityEngine.Random.Range(minCustomersPerDay, maxCustomersPerDay + 1);
            for (int i = 0; i < count; i++)
            {
                var archetype = availableArchetypes[UnityEngine.Random.Range(0, availableArchetypes.Count)];
                customerQueue.Enqueue(CustomerGenerator.Generate(archetype, reputation));
            }
        }
    }
}
