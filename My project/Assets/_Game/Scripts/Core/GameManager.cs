using System;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.SaveLoad;
using UnityEngine;

namespace PawnshopKing.Core
{
    /// <summary>High-level campaign phases the GameManager transitions between (GDD 39).</summary>
    public enum GamePhase
    {
        Boot,
        DayActive,
        DaySummary,
        GameOver,
        Victory
    }

    /// <summary>
    /// Owns the campaign GameState and high-level transitions; starts and ends days
    /// by driving DayManager (GDD 39). All gameplay state lives in State so
    /// SaveLoadSystem can persist one object (GDD 35).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        // GDD gives no canon starting numbers (26.3 lists expense types only) — tune in inspector.
        [Header("New campaign defaults")]
        [SerializeField] private int startingCash = 500;
        [SerializeField] private int startingDebt = 5000;
        [SerializeField] private int firstPaymentAmount = 750;
        [SerializeField] private int daysUntilFirstPayment = 7;

        [SerializeField] private DayManager dayManager;

        public GameState State { get; private set; }
        public GamePhase Phase { get; private set; } = GamePhase.Boot;
        public DayManager Day => dayManager;

        public event Action<GamePhase> PhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (dayManager == null) dayManager = GetComponentInChildren<DayManager>();
        }

        /// <summary>Creates a fresh campaign starting in debt (GDD 40). Single save slot: the old campaign's save is erased.</summary>
        public void StartNewGame()
        {
            SaveLoadSystem.Delete();

            State = new GameState
            {
                cash = startingCash,
                debt =
                {
                    totalDebt = startingDebt,
                    nextPaymentAmount = firstPaymentAmount,
                    daysUntilPayment = daysUntilFirstPayment
                }
            };

            BeginDay();
        }

        /// <summary>
        /// Resumes the saved campaign. The autosave marks a completed day, so this
        /// rolls into the following morning. Returns false when no usable save exists.
        /// </summary>
        public bool ContinueFromSave()
        {
            var loaded = SaveLoadSystem.Load();
            if (loaded == null) return false;

            State = loaded;
            State.currentDay++;
            BeginDay();
            return true;
        }

        /// <summary>Opens the shop for the current day: DayManager builds the queue.</summary>
        public void BeginDay()
        {
            // Snapshot for the day summary's profit/loss and deltas (GDD 32.1 E).
            State.dayStartCash = State.cash;
            State.dayStartReputation = State.reputation;
            State.dayStartHeat = State.heat;

            dayManager.StartDay(State);
            SetPhase(GamePhase.DayActive);
        }

        /// <summary>Closes the shop: DayManager resolves the day (incl. debt), then summary — or an ending.</summary>
        public void EndDay()
        {
            dayManager.EndDay(State);

            if (dayManager.LastDebtResult.gameOver)
            {
                SaveLoadSystem.Delete();  // a finished campaign can't be continued into
                SetPhase(GamePhase.GameOver);
            }
            else if (dayManager.LastDebtResult.debtCleared)
            {
                SaveLoadSystem.Delete();
                SetPhase(GamePhase.Victory);  // GDD 27.2
            }
            else
            {
                SaveLoadSystem.Save(State);  // autosave: day complete, resolution applied
                SetPhase(GamePhase.DaySummary);
            }
        }

        /// <summary>Called from the day summary to roll into the next morning.</summary>
        public void AdvanceToNextDay()
        {
            if (Phase == GamePhase.GameOver || Phase == GamePhase.Victory) return;

            State.currentDay++;
            BeginDay();
        }

        private void SetPhase(GamePhase phase)
        {
            if (Phase == phase) return;
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
