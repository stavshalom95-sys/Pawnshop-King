using System.Collections;
using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Systems.Localization;
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

        // Juice: profit counts up from zero, then the debt verdict gets stamped.
        private GameObject stampGO;
        private TextMeshProUGUI stampLabel;
        private GamePhase shownPhase;
        private int finalProfit;
        private bool bodyAnimating;
        private Coroutine bodyRoutine;

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
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (gm != null) gm.PhaseChanged -= OnPhaseChanged;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (screenRoot.activeSelf) Show(gm.Phase);
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

        private void Update()
        {
            // Click fast-forwards the count-up straight to the stamped verdict.
            if (!bodyAnimating || !screenRoot.activeSelf) return;
            if (!UIFx.SkipClickPressed()) return;

            if (bodyRoutine != null) StopCoroutine(bodyRoutine);
            FinishBody();
        }

        private void Show(GamePhase phase)
        {
            var s = gm.State;
            shownPhase = phase;
            finalProfit = s.cash - s.dayStartCash;
            bool gameOver = phase == GamePhase.GameOver;
            bool victory = phase == GamePhase.Victory;

            string title = gameOver ? Loc.T(LanguageManager.Keys.GameOver).ToUpperInvariant()
                : victory ? Loc.T(LanguageManager.Keys.Victory).ToUpperInvariant()
                : Loc.F(LanguageManager.Keys.DayClosing, s.currentDay);
            Loc.Set(titleText, title, UITheme.HeaderFont);
            titleText.color = gameOver ? UITheme.Danger
                : victory ? UITheme.Gold
                : HUDUIManager.TextColor;

            Loc.Set(continueLabel, gameOver || victory
                ? Loc.T(LanguageManager.Keys.NewCampaign)
                : Loc.F(LanguageManager.Keys.OpenDay, s.currentDay + 1));

            stampGO.SetActive(false);
            if (bodyRoutine != null) StopCoroutine(bodyRoutine);
            bodyRoutine = StartCoroutine(BodyCountUpRoutine());

            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        private IEnumerator BodyCountUpRoutine()
        {
            bodyAnimating = true;
            const float duration = 0.7f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                RenderBody(Mathf.RoundToInt(Mathf.Lerp(0f, finalProfit, 1f - (1f - t) * (1f - t))));
                yield return null;
            }

            FinishBody();
        }

        private void FinishBody()
        {
            bodyAnimating = false;
            bodyRoutine = null;
            RenderBody(finalProfit);
            ShowStamp();
        }

        /// <summary>Composes the summary body with the profit line at the given (possibly mid-count) value.</summary>
        private void RenderBody(int profitShown)
        {
            var s = gm.State;
            bool gameOver = shownPhase == GamePhase.GameOver;
            bool victory = shownPhase == GamePhase.Victory;

            var sb = new StringBuilder();
            sb.Append(Loc.F(LanguageManager.Keys.SummaryProfitToday, Delta(profitShown, "$")));
            sb.Append("\n" + Loc.F(LanguageManager.Keys.SummaryReputation, Delta(s.reputation - s.dayStartReputation), s.reputation));
            sb.Append("\n" + Loc.F(LanguageManager.Keys.SummaryHeat, Delta(s.heat - s.dayStartHeat), s.heat));
            sb.Append($"\n\n<color=#C9B458>{DebtLine(gm.Day.LastDebtResult, s)}</color>");

            if (gm.Day.LastHeatEvent.occurred)
            {
                sb.Append($"\n<color=#C05B4D>{HeatLine(gm.Day.LastHeatEvent)}</color>");
            }

            if (victory) AppendFinalStats(sb, s);
            else if (!gameOver && s.debt.totalDebt > 0)
            {
                sb.Append($"\n<size=85%><color=#9E9A90>{Loc.F(LanguageManager.Keys.DebtRemaining, s.debt.totalDebt.ToString("N0"))}</color></size>");
            }

            bodyText.alignment = LanguageManager.IsRtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            Loc.Set(bodyText, sb.ToString());
        }

        /// <summary>Rubber-stamps the debt verdict: PAID (green) or SEIZED (red). Nothing when no payment resolved.</summary>
        private void ShowStamp()
        {
            var debt = gm.Day.LastDebtResult;

            string key;
            Color color;
            if (debt.forcedSale) { key = LanguageManager.Keys.StampSeized; color = UITheme.Danger; }
            else if (debt.debtCleared || debt.paid) { key = LanguageManager.Keys.StampPaid; color = UITheme.Success; }
            else return;

            Loc.Set(stampLabel, Loc.T(key), UITheme.HeaderFont);
            stampLabel.color = color;
            stampGO.SetActive(true);
            StartCoroutine(StampSlamRoutine());
        }

        private IEnumerator StampSlamRoutine()
        {
            var rect = (RectTransform)stampGO.transform;
            var group = stampGO.GetComponent<CanvasGroup>();
            const float duration = 0.22f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (rect == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float slam = t * t; // ease-in: falls onto the page
                rect.localScale = Vector3.one * Mathf.Lerp(2.1f, 1f, slam);
                group.alpha = t;
                yield return null;
            }

            rect.localScale = Vector3.one;
            group.alpha = 1f;
        }

        /// <summary>Localized debt verdict composed from the tick's structured fields, not its English message.</summary>
        private static string DebtLine(Systems.Debt.DebtTickResult result, Data.Runtime.GameState s)
        {
            if (result.bankrupt) return Loc.T(LanguageManager.Keys.Bankrupt);
            if (result.gameOver) return Loc.F(LanguageManager.Keys.DebtNoAssets, result.amountDue.ToString("N0"));
            if (result.debtCleared) return Loc.F(LanguageManager.Keys.DebtFinal, result.amountPaid.ToString("N0"));
            if (result.forcedSale && result.paid)
            {
                return Loc.F(LanguageManager.Keys.DebtSeized, result.amountDue.ToString("N0"), result.itemsSeized);
            }

            if (result.paid)
            {
                return Loc.F(LanguageManager.Keys.DebtPaid, result.amountPaid.ToString("N0"),
                    s.debt.totalDebt.ToString("N0"), s.debt.nextPaymentAmount.ToString("N0"), s.debt.daysUntilPayment);
            }

            return s.debt.totalDebt > 0
                ? Loc.F(LanguageManager.Keys.DebtNext, s.debt.nextPaymentAmount.ToString("N0"), s.debt.daysUntilPayment)
                : Loc.T(LanguageManager.Keys.DebtClear);
        }

        private static string HeatLine(Systems.Events.HeatEventResult heat)
        {
            if (heat.blackMarketRaided)
            {
                return Loc.F(LanguageManager.Keys.HeatRaid,
                    Systems.Events.HeatEventSystem.RaidShutdownDays, Systems.Events.HeatEventSystem.RaidHeatRelief);
            }

            return heat.itemsSeized > 0
                ? Loc.F(LanguageManager.Keys.HeatPoliceSeized, heat.itemsSeized, Systems.Events.HeatEventSystem.PoliceHeatRelief)
                : Loc.F(LanguageManager.Keys.HeatPoliceClean, Systems.Events.HeatEventSystem.PoliceHeatRelief);
        }

        /// <summary>The victory ledger (GDD 27.2): the inherited debt is paid — final campaign stats.</summary>
        private static void AppendFinalStats(StringBuilder sb, Data.Runtime.GameState s)
        {
            int toolCount = 0;
            foreach (var upgrade in UpgradeSystem.AllUpgrades)
            {
                if (UpgradeSystem.IsOwned(s, upgrade.id)) toolCount++;
            }

            sb.Append("\n\n" + Loc.F(LanguageManager.Keys.VictoryNarrative, s.currentDay));
            sb.Append("\n\n<size=90%>");
            sb.Append(Loc.F(LanguageManager.Keys.VictoryStats, s.cash.ToString("N0"), s.reputation, s.heat,
                s.inventory.Count, toolCount, UpgradeSystem.AllUpgrades.Count, s.debt.missedPayments));
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

            BuildStamp(panel);

            screenRoot.SetActive(false);
        }

        /// <summary>The rubber stamp: top-right of the panel, tilted, outside the vertical layout's control.</summary>
        private void BuildStamp(RectTransform panel)
        {
            stampGO = new GameObject("VerdictStamp", typeof(RectTransform), typeof(CanvasGroup));
            stampGO.transform.SetParent(panel, false);
            stampGO.AddComponent<LayoutElement>().ignoreLayout = true;

            var rect = (RectTransform)stampGO.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -22f);
            rect.sizeDelta = new Vector2(240f, 64f);
            rect.localRotation = Quaternion.Euler(0f, 0f, -8f);

            stampLabel = stampGO.AddComponent<TextMeshProUGUI>();
            stampLabel.fontSize = 42f;
            stampLabel.fontStyle = FontStyles.Bold;
            stampLabel.alignment = TextAlignmentOptions.Center;
            stampLabel.raycastTarget = false;
            if (UITheme.HeaderFont != null)
            {
                stampLabel.font = UITheme.HeaderFont;
                stampLabel.characterSpacing = UITheme.HeaderCharacterSpacing;
            }

            stampGO.SetActive(false);
        }
    }
}
