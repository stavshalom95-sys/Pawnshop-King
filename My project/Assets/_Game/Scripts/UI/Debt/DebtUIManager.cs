using PawnshopKing.Core;
using PawnshopKing.Systems.Debt;
using PawnshopKing.Systems.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Voluntary debt prepayment (agency layer on top of the fixed schedule):
    /// lets the player apply cash toward TotalDebt on demand, any day, any
    /// amount up to what's owed. DebtSystem's 7-day scheduled tick is completely
    /// untouched by this — daysUntilPayment never moves here, so the deadline
    /// stays exactly as hard as it was. This only ever gives the player an
    /// extra, optional way to chip away at the principal early.
    /// </summary>
    public class DebtUIManager : MonoBehaviour
    {
        public static DebtUIManager Instance { get; private set; }

        private GameManager gm;
        private GameObject screenRoot;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI infoText;
        private TMP_InputField amountInput;
        private TextMeshProUGUI feedbackText;

        private void Awake()
        {
            Instance = this;
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("DebtUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScreen();
            gm.PhaseChanged += OnPhaseChanged;
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (gm != null) gm.PhaseChanged -= OnPhaseChanged;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        // Day transitions can change everything shown here (a scheduled payment
        // may have just fired) — close rather than risk a stale read.
        private void OnPhaseChanged(GamePhase phase) => Close();

        private void OnLanguageChanged()
        {
            if (screenRoot.activeSelf) RefreshInfo();
        }

        public void Toggle()
        {
            if (screenRoot.activeSelf) Close();
            else Open();
        }

        public void Open()
        {
            feedbackText.text = string.Empty;
            amountInput.text = string.Empty;
            RefreshInfo();
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        public void Close() => screenRoot.SetActive(false);

        private void RefreshInfo()
        {
            var s = gm.State;
            Loc.Set(titleText, Loc.F(LanguageManager.Keys.DebtPanelTitle, s.debt.totalDebt.ToString("N0")), UITheme.HeaderFont);

            string next = s.debt.totalDebt > 0
                ? Loc.F(LanguageManager.Keys.DebtNext, s.debt.nextPaymentAmount.ToString("N0"), s.debt.daysUntilPayment)
                : Loc.T(LanguageManager.Keys.DebtClear);
            string cash = Loc.F(LanguageManager.Keys.DebtPanelCash, s.cash.ToString("N0"));

            infoText.alignment = LanguageManager.IsRtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            Loc.Set(infoText, next + "\n" + cash);
        }

        private void OnPayClicked()
        {
            if (!int.TryParse(amountInput.text, out int amount) || amount <= 0)
            {
                Loc.Set(feedbackText, Loc.T(LanguageManager.Keys.DebtPanelEnterAmount));
                return;
            }

            if (amount > gm.State.cash)
            {
                Loc.Set(feedbackText, Loc.T(LanguageManager.Keys.DebtPanelInsufficientCash));
                return;
            }

            var result = DebtSystem.TryPrepay(gm.State, amount);
            if (!result.success)
            {
                // Only remaining failure reason here is "nothing owed" — amount
                // and cash are already validated above.
                Loc.Set(feedbackText, Loc.T(LanguageManager.Keys.DebtClear));
                return;
            }

            Systems.Audio.AudioManager.Instance?.PlayAccept();
            amountInput.text = string.Empty;
            Loc.Set(feedbackText, result.debtCleared
                ? Loc.F(LanguageManager.Keys.DebtPanelPaidCleared, result.amountPaid.ToString("N0"))
                : Loc.F(LanguageManager.Keys.DebtPanelPaid, result.amountPaid.ToString("N0"), gm.State.debt.totalDebt.ToString("N0")));
            RefreshInfo();
        }

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("DebtCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // same tier as Inventory/Upgrades

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Dim the shop behind the panel, matching the day summary/pause treatment.
            var dim = HUDUIManager.CreatePanel(canvasGO.transform, "DebtScreen", new Color(0f, 0f, 0f, 0.6f));
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            screenRoot = dim.gameObject;

            var panel = HUDUIManager.CreatePanel(dim, "DebtPanel", HUDUIManager.PanelColor, rounded: true);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(560f, 420f);
            HUDUIManager.AddPanelShadow(panel);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(36, 36, 30, 30);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            titleText = HUDUIManager.CreateText(panel, "Title", 30f, TextAlignmentOptions.Left, FontStyles.Bold, header: true);
            infoText = HUDUIManager.CreateText(panel, "Info", 21f, TextAlignmentOptions.Left);
            infoText.color = HUDUIManager.MutedColor;

            var inputRow = new GameObject("InputRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            inputRow.transform.SetParent(panel, false);
            inputRow.AddComponent<LayoutElement>().preferredHeight = 46f;
            var inputLayout = inputRow.GetComponent<HorizontalLayoutGroup>();
            inputLayout.spacing = 12f;
            inputLayout.childAlignment = TextAnchor.MiddleLeft;
            inputLayout.childControlWidth = true;
            inputLayout.childControlHeight = true;
            inputLayout.childForceExpandWidth = false;
            inputLayout.childForceExpandHeight = false;

            amountInput = HUDUIManager.CreateAmountInput(inputRow.transform, LanguageManager.Keys.DebtPayPlaceholder, 220f);
            amountInput.onSubmit.AddListener(_ => OnPayClicked());

            var payLabel = HUDUIManager.CreateSmallButton(inputRow.transform, "Pay", 150f, OnPayClicked);
            LocalizedLabel.Bind(payLabel, LanguageManager.Keys.DebtPanelPay);

            feedbackText = HUDUIManager.CreateText(panel, "Feedback", 19f, TextAlignmentOptions.Left, FontStyles.Italic);
            feedbackText.color = UITheme.Gold;

            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(panel, false);
            spacer.GetComponent<LayoutElement>().flexibleHeight = 1f;

            var closeLabel = HUDUIManager.CreateSmallButton(panel, "Close", 200f, Close);
            LocalizedLabel.Bind(closeLabel, LanguageManager.Keys.Close);
            var closeLayout = closeLabel.transform.parent.GetComponent<LayoutElement>();
            closeLayout.preferredHeight = 54f;

            screenRoot.SetActive(false);
        }
    }
}
