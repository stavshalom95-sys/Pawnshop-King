using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Systems.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// End-of-day summary screen (GDD 32.1 E): profit/loss, reputation and heat
    /// deltas, and the debt and heat clocks' verdicts. Opens automatically when the
    /// day ends, and doubles as the game-over screen (GDD 27.1) and the campaign
    /// victory screen (GDD 27.2), each with a New Campaign button.
    /// </summary>
    public class DaySummaryUIManager : MonoBehaviour
    {
        private GameManager gm;
        private GameObject screenRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI continueLabel;

        private void Awake()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("DaySummaryUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScreen();
            gm.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (gm != null) gm.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.DaySummary:
                case GamePhase.GameOver:
                case GamePhase.Victory:
                    Show(phase);
                    break;
                default:
                    screenRoot.SetActive(false);
                    break;
            }
        }

        private void Show(GamePhase phase)
        {
            var s = gm.State;
            bool gameOver = phase == GamePhase.GameOver;
            bool victory = phase == GamePhase.Victory;

            titleText.text = gameOver ? "GAME OVER"
                : victory ? "CAMPAIGN COMPLETE"
                : $"Day {s.currentDay} — Closing Time";
            titleText.color = gameOver ? UITheme.Danger
                : victory ? UITheme.Gold
                : HUDUIManager.TextColor;

            var sb = new StringBuilder();
            sb.Append($"Profit / Loss:  {Delta(s.cash - s.dayStartCash, "$")}");
            sb.Append($"\nReputation:  {Delta(s.reputation - s.dayStartReputation)}   (now {s.reputation})");
            sb.Append($"\nHeat:  {Delta(s.heat - s.dayStartHeat)}   (now {s.heat})");
            sb.Append($"\n\n<color=#C9B458>{gm.Day.LastDebtResult.message}</color>");

            if (gm.Day.LastHeatEvent.occurred)
            {
                sb.Append($"\n<color=#C05B4D>{gm.Day.LastHeatEvent.message}</color>");
            }

            if (victory) AppendFinalStats(sb, s);
            else if (!gameOver && s.debt.totalDebt > 0)
            {
                sb.Append($"\n<size=85%><color=#9E9A90>Debt remaining: ${s.debt.totalDebt:N0}</color></size>");
            }

            bodyText.text = sb.ToString();
            continueLabel.text = gameOver || victory ? "New Campaign" : $"Open Day {s.currentDay + 1}";
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        /// <summary>The victory ledger (GDD 27.2): the inherited debt is paid — final campaign stats.</summary>
        private static void AppendFinalStats(StringBuilder sb, Data.Runtime.GameState s)
        {
            int toolCount = 0;
            foreach (var upgrade in UpgradeSystem.AllUpgrades)
            {
                if (UpgradeSystem.IsOwned(s, upgrade.id)) toolCount++;
            }

            sb.Append($"\n\nThe inherited debt is history. In {s.currentDay} day{(s.currentDay == 1 ? "" : "s")} you turned a dying shop into your own — <color=#D4A029>the Pawnshop King</color>.");
            sb.Append("\n\n<size=90%>");
            sb.Append($"Final cash:  ${s.cash:N0}");
            sb.Append($"\nReputation:  {s.reputation}   ·   Heat:  {s.heat}");
            sb.Append($"\nInventory:  {s.inventory.Count} item{(s.inventory.Count == 1 ? "" : "s")} still on the shelves");
            sb.Append($"\nTools installed:  {toolCount}/{UpgradeSystem.AllUpgrades.Count}");
            sb.Append($"\nPayments missed along the way:  {s.debt.missedPayments}");
            sb.Append("</size>");
        }

        private static string Delta(int delta, string prefix = "")
        {
            if (delta > 0) return $"<color=#7FBF6A>+{prefix}{delta:N0}</color>";
            if (delta < 0) return $"<color=#C05B4D>-{prefix}{-delta:N0}</color>";
            return $"<color=#9E9A90>±{prefix}0</color>";
        }

        private void OnContinueClicked()
        {
            if (gm.Phase == GamePhase.GameOver || gm.Phase == GamePhase.Victory) gm.StartNewGame();
            else gm.AdvanceToNextDay();
        }

        // ---- Construction ----------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("SummaryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // above HUD and inventory

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Dim the whole shop behind the summary.
            var dim = HUDUIManager.CreatePanel(canvasGO.transform, "SummaryScreen", new Color(0f, 0f, 0f, 0.75f));
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            screenRoot = dim.gameObject;

            var panel = HUDUIManager.CreatePanel(dim, "SummaryPanel", HUDUIManager.PanelColor, rounded: true);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(680f, 480f);
            HUDUIManager.AddPanelShadow(panel);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 32, 32);
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = HUDUIManager.CreateText(panel, "Title", 34f, TextAlignmentOptions.Left, FontStyles.Bold, header: true);
            bodyText = HUDUIManager.CreateText(panel, "Body", 24f, TextAlignmentOptions.Left);

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(panel, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            continueLabel = HUDUIManager.CreateSmallButton(panel, "Continue", 280f, OnContinueClicked);
            var buttonLayout = continueLabel.transform.parent.GetComponent<LayoutElement>();
            buttonLayout.preferredHeight = 62f;
            continueLabel.fontSize = 24f;

            screenRoot.SetActive(false);
        }
    }
}
