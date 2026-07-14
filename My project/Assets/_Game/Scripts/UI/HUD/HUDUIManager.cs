using System.Collections;
using System.Collections.Generic;
using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Inspection;
using PawnshopKing.Systems.Items;
using PawnshopKing.Systems.Localization;
using PawnshopKing.Systems.Negotiation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace PawnshopKing.UI
{
    /// <summary>
    /// Builds the whole Phase 1 HUD from code (zero-editor-wiring): the GDD 32.2 top
    /// resource bar, the current-customer panel, and the action button that advances
    /// the day loop. Panel content is event-driven off GameManager/DayManager; the
    /// resource bar uses per-frame change detection until stat-change events exist.
    /// </summary>
    public class HUDUIManager : MonoBehaviour
    {
        // Shared with other code-built screens — all values come from the UITheme
        // noir tokens so a palette change lands everywhere at once.
        internal static readonly Color BarColor = UITheme.TopBarColor;
        internal static readonly Color PanelColor = UITheme.Surface;
        internal static readonly Color TextColor = UITheme.TextPrimary;
        internal static readonly Color MutedColor = UITheme.TextMuted;
        internal static readonly Color ButtonColor = UITheme.NeonCyan;
        internal static readonly Color ButtonTextColor = UITheme.OnNeon;

        private GameManager gm;

        // Diagnostics: kept so the first Update can report post-layout rect sizes.
        private Canvas hudCanvas;
        private RectTransform topBarRect;
        private RectTransform customerPanelRect;
        private RectTransform actionButtonRect;
        private bool loggedLayoutDiagnostics;

        private TextMeshProUGUI dayText;
        private TextMeshProUGUI cashText;
        private TextMeshProUGUI reputationText;
        private TextMeshProUGUI heatText;
        private TextMeshProUGUI debtText;

        private TextMeshProUGUI customerNameText;
        private TextMeshProUGUI customerMoodText;
        private TextMeshProUGUI customerDialogueText;
        private RectTransform itemsContainer;
        private TextMeshProUGUI queueText;
        private TextMeshProUGUI actionLabel;

        private GameObject dealControls;
        private TMP_InputField offerInput;
        private TextMeshProUGUI buyLabel;
        private TextMeshProUGUI dealFeedbackText;

        // Armed Offer state: Offer pressed with nothing to submit pulses the button
        // and hands focus to the input instead of dead-clicking (GDD 32.2 feedback).
        private const string OfferArmHint = "Type your offer — press Enter or hit Offer again.";
        private Image offerButtonImage;
        private Coroutine offerPulse;
        private bool offerArmed;
        private int offerArmedFrame;

        // Onboarding tips (Hebrew, RTL): one context-sensitive line under the
        // counter, toggleable via the "?" button and persisted in PlayerPrefs.
        private const string TipsEnabledKey = "tips_enabled";
        private Image tipBackground;
        private TextMeshProUGUI tipText;
        private bool tipsEnabled;

        private CustomerInstance currentCustomer;
        private CustomerArchetypeDefinition currentArchetype;

        private readonly List<ItemRow> itemRows = new List<ItemRow>();

        /// <summary>One counter row: the item, its select checkbox, info text, and Inspect button.</summary>
        private class ItemRow
        {
            public ItemInstance item;
            public GameObject root;
            public Toggle selectToggle;
            public TextMeshProUGUI infoText;
            public Button inspectButton;
            public TextMeshProUGUI inspectLabel;
            public Image inspectImage;
        }

        // Change-detection cache for the resource bar.
        private int lastDay = int.MinValue, lastCash, lastReputation, lastHeat, lastDebt, lastPayment, lastDebtDays;

        // Juice: the cash label counts toward its new value instead of snapping.
        private int cashShown = int.MinValue;
        private Coroutine cashTickRoutine;

        // Juice: dialogue types out, item rows enter staggered — one click skips all.
        private Coroutine dialogueRoutine;
        private readonly List<CanvasGroup> pendingRowFades = new List<CanvasGroup>();
        private readonly List<Coroutine> rowFadeRoutines = new List<Coroutine>();

        private void Awake()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("HUDUIManager requires GameManager to exist first — add it after GameManager in SceneInitializer.");
                enabled = false;
                return;
            }

            Debug.Log("[HUD] Awake — GameManager found, building HUD.");
            BuildHud();

            gm.PhaseChanged += OnPhaseChanged;
            gm.Day.DayStarted += OnDayStarted;
            gm.Day.CustomerArrived += OnCustomerArrived;
            gm.Day.DayEnded += OnDayEnded;
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            LanguageManager.LanguageChanged -= OnLanguageChanged;
            if (gm == null) return;
            gm.PhaseChanged -= OnPhaseChanged;
            if (gm.Day == null) return;
            gm.Day.DayStarted -= OnDayStarted;
            gm.Day.CustomerArrived -= OnCustomerArrived;
            gm.Day.DayEnded -= OnDayEnded;
        }

        /// <summary>Static labels re-render themselves; this refreshes the value-bearing ones.</summary>
        private void OnLanguageChanged()
        {
            RefreshActionButton();
            RefreshBuyLabel();
            foreach (var row in itemRows) RefreshItemRow(row);
            UpdateTip();
        }

        private void Update()
        {
            var s = gm.State;
            if (s == null) return;

            // Any click fast-forwards running juice — gameplay never waits on polish.
            if (JuiceActive && UIFx.SkipClickPressed()) SkipJuice();

            // One-shot post-layout diagnostic: by the first Update the canvas has
            // laid out, so zero-sized or off-screen rects are visible here.
            if (!loggedLayoutDiagnostics)
            {
                loggedLayoutDiagnostics = true;
                Debug.Log($"[HUD] First Update — canvas pixelRect {hudCanvas.pixelRect.size}, " +
                          $"TopBar rect {topBarRect.rect.size} at {topBarRect.position}, " +
                          $"ActionButton rect {actionButtonRect.rect.size} at {actionButtonRect.position} (label '{actionLabel.text}'), " +
                          $"CustomerPanel rect {customerPanelRect.rect.size} at {customerPanelRect.position}, " +
                          $"EventSystem: {(EventSystem.current != null ? EventSystem.current.currentInputModule?.GetType().Name ?? "no input module" : "MISSING")}");
            }

            if (s.currentDay == lastDay && s.cash == lastCash && s.reputation == lastReputation
                && s.heat == lastHeat && s.debt.totalDebt == lastDebt
                && s.debt.nextPaymentAmount == lastPayment && s.debt.daysUntilPayment == lastDebtDays) return;

            lastDay = s.currentDay;
            lastCash = s.cash;
            lastReputation = s.reputation;
            lastHeat = s.heat;
            lastDebt = s.debt.totalDebt;
            lastPayment = s.debt.nextPaymentAmount;
            lastDebtDays = s.debt.daysUntilPayment;

            dayText.text = $"Day {s.currentDay}";
            UpdateCashLabel(s.cash);
            reputationText.text = $"Rep  {s.reputation}";
            heatText.text = $"Heat  {s.heat}";
            debtText.text = s.debt.totalDebt > 0
                ? $"Debt  ${s.debt.totalDebt:N0}  (${s.debt.nextPaymentAmount:N0} due in {s.debt.daysUntilPayment}d)"
                : "Debt  cleared";
        }

        // ---- Cash tick (juice) ----------------------------------------------

        private void UpdateCashLabel(int target)
        {
            if (cashShown == int.MinValue) // first paint: no animation
            {
                cashShown = target;
                cashText.text = $"Cash  ${target:N0}";
                return;
            }

            if (target == cashShown && cashTickRoutine == null)
            {
                cashText.text = $"Cash  ${target:N0}";
                return;
            }

            if (cashTickRoutine != null) StopCoroutine(cashTickRoutine);
            cashTickRoutine = StartCoroutine(CashTickRoutine(cashShown, target));
        }

        private IEnumerator CashTickRoutine(int from, int to)
        {
            const float duration = 0.4f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cashShown = Mathf.RoundToInt(Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t)));
                cashText.text = $"Cash  ${cashShown:N0}";
                yield return null;
            }

            cashShown = to;
            cashText.text = $"Cash  ${to:N0}";

            // Settle flash: brighten to the focus gold, then ease home.
            float flash = 0f;
            while (flash < 0.25f)
            {
                flash += Time.unscaledDeltaTime;
                cashText.color = Color.Lerp(UITheme.GoldBright, UITheme.Gold, flash / 0.25f);
                yield return null;
            }

            cashText.color = UITheme.Gold;
            cashTickRoutine = null;
        }

        private void CompleteCashTick()
        {
            if (cashTickRoutine == null) return;
            StopCoroutine(cashTickRoutine);
            cashTickRoutine = null;
            cashShown = gm.State.cash;
            cashText.text = $"Cash  ${cashShown:N0}";
            cashText.color = UITheme.Gold;
        }

        // ---- Juice skip (dialogue, row entrances, cash tick) -----------------

        private bool JuiceActive =>
            dialogueRoutine != null || cashTickRoutine != null || pendingRowFades.Count > 0;

        private void SkipJuice()
        {
            if (dialogueRoutine != null)
            {
                StopCoroutine(dialogueRoutine);
                dialogueRoutine = null;
                customerDialogueText.maxVisibleCharacters = int.MaxValue;
            }

            foreach (var routine in rowFadeRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }

            rowFadeRoutines.Clear();
            foreach (var group in pendingRowFades)
            {
                if (group == null) continue;
                group.alpha = 1f;
                group.transform.localScale = Vector3.one;
            }

            pendingRowFades.Clear();
            CompleteCashTick();
        }

        private void SetDialogueInstant(string text)
        {
            if (dialogueRoutine != null)
            {
                StopCoroutine(dialogueRoutine);
                dialogueRoutine = null;
            }

            customerDialogueText.maxVisibleCharacters = int.MaxValue;
            customerDialogueText.text = text;
        }

        private void StartDialogueTypewriter(string line)
        {
            if (dialogueRoutine != null) StopCoroutine(dialogueRoutine);
            customerDialogueText.text = line;
            customerDialogueText.maxVisibleCharacters = 0;
            dialogueRoutine = StartCoroutine(DialogueRoutine());
        }

        private IEnumerator DialogueRoutine()
        {
            const float charsPerSecond = 35f;
            customerDialogueText.ForceMeshUpdate();
            int total = customerDialogueText.textInfo.characterCount;

            float elapsed = 0f;
            int shown = 0;
            while (shown < total)
            {
                elapsed += Time.unscaledDeltaTime;
                int target = Mathf.Min(total, Mathf.FloorToInt(elapsed * charsPerSecond));
                if (target != shown)
                {
                    shown = target;
                    customerDialogueText.maxVisibleCharacters = shown;
                }

                yield return null;
            }

            customerDialogueText.maxVisibleCharacters = int.MaxValue;
            dialogueRoutine = null;
        }

        // ---- Event handlers ------------------------------------------------

        private void OnDayStarted(int day)
        {
            currentCustomer = null;
            currentArchetype = null;
            customerNameText.text = "Shop is open";
            customerMoodText.text = string.Empty;
            SetDialogueInstant("Waiting for the first customer...");
            DisarmOffer();
            ClearItemRows();
            dealControls.SetActive(false);
            dealFeedbackText.text = string.Empty;
            queueText.text = $"{gm.Day.CustomersRemaining} customers in the queue today";
            RefreshActionButton();
            UpdateTip();
            Debug.Log($"[HUD] Day {day} started — {gm.Day.CustomersRemaining} customers queued, action button shows '{actionLabel.text}'.");
        }

        private void OnCustomerArrived(CustomerInstance customer)
        {
            currentCustomer = customer;
            currentArchetype = gm.Day.GetArchetype(customer.archetypeId);

            customerNameText.text = currentArchetype != null ? currentArchetype.displayName : customer.archetypeId;
            StartDialogueTypewriter($"“{PickDialogueLine(currentArchetype)}”");
            UpdateMoodAskingLine();
            RebuildItemRows(customer);

            DisarmOffer();
            offerInput.text = string.Empty;
            bool tradable = customer.items.Count > 0;
            dealControls.SetActive(tradable);
            dealFeedbackText.text = tradable ? string.Empty : "They have nothing you'd trade for.";
            RefreshBuyLabel();

            queueText.text = gm.Day.CustomersRemaining == 0
                ? "Last customer of the day"
                : $"{gm.Day.CustomersRemaining} more waiting outside";
            RefreshActionButton();
            UpdateTip();

            // A new face at the counter eases in rather than snapping.
            UIFx.FadeIn(this, customerPanelRect.gameObject, 0.4f);
        }

        private void OnDayEnded(int day)
        {
            customerNameText.text = "Shop closed";
            customerMoodText.text = string.Empty;
            SetDialogueInstant($"Day {day} is over. The debt clock ticks on.");
            DisarmOffer();
            ClearItemRows();
            dealControls.SetActive(false);
            dealFeedbackText.text = string.Empty;
            queueText.text = string.Empty;
            RefreshActionButton();
        }

        // ---- Deal flow (GDD 13) ----------------------------------------------

        private void OnOfferClicked()
        {
            if (!InNegotiation()) return;

            var selection = SelectedItems();
            if (selection.Count == 0)
            {
                DisarmOffer();
                dealFeedbackText.text = "Check at least one item to deal on.";
                return;
            }

            if (!int.TryParse(offerInput.text, out int amount) || amount <= 0)
            {
                ArmOffer();
                return;
            }

            DisarmOffer();

            if (amount > gm.State.cash)
            {
                dealFeedbackText.text = "You can't cover that offer.";
                return;
            }

            var result = NegotiationSystem.MakeOffer(gm.State, currentCustomer, currentArchetype, selection, amount);
            switch (result.outcome)
            {
                case OfferOutcome.Accepted:
                    Systems.Audio.AudioManager.Instance?.PlayAccept();
                    UIFx.SpawnMoneyFloater(this, customerPanelRect, -result.price, new Vector2(0f, -190f));
                    dealFeedbackText.text = $"Deal. You hand over ${result.price:N0}.{LeftoverSuffix()}";
                    ConcludeVisit();
                    break;
                case OfferOutcome.AcceptedReluctantly:
                    Systems.Audio.AudioManager.Instance?.PlayAccept();
                    UIFx.SpawnMoneyFloater(this, customerPanelRect, -result.price, new Vector2(0f, -190f));
                    dealFeedbackText.text = $"“Fine. Just give me the money.” You pay ${result.price:N0}.{LeftoverSuffix()}";
                    ConcludeVisit();
                    break;
                case OfferOutcome.Countered:
                    dealFeedbackText.text = $"“Make it ${result.price:N0} and we're done.”";
                    UpdateMoodAskingLine();
                    RefreshBuyLabel();
                    UpdateTip();
                    break;
                case OfferOutcome.OffendedLeft:
                    Systems.Audio.AudioManager.Instance?.PlayReject();
                    dealFeedbackText.text = "“Insulting.” They storm out. Word gets around. (Reputation -1)";
                    ConcludeVisit();
                    break;
                case OfferOutcome.GaveUpLeft:
                    Systems.Audio.AudioManager.Instance?.PlayReject();
                    dealFeedbackText.text = "“Forget it.” They pack up and leave.";
                    ConcludeVisit();
                    break;
            }
        }

        private void OnBuyClicked()
        {
            if (!InNegotiation()) return;

            var selection = SelectedItems();
            if (selection.Count == 0)
            {
                dealFeedbackText.text = "Check at least one item to deal on.";
                return;
            }

            if (currentCustomer.askingPrice > gm.State.cash)
            {
                dealFeedbackText.text = "You don't have the cash for their price.";
                return;
            }

            var result = NegotiationSystem.BuyAtAskingPrice(gm.State, currentCustomer, currentArchetype, selection);
            Systems.Audio.AudioManager.Instance?.PlayAccept();
            UIFx.SpawnMoneyFloater(this, customerPanelRect, -result.price, new Vector2(0f, -190f));
            dealFeedbackText.text = $"Bought at asking price — ${result.price:N0}. Fair dealing. (Reputation +1){LeftoverSuffix()}";
            ConcludeVisit();
        }

        private void OnRejectClicked()
        {
            if (!InNegotiation()) return;

            DisarmOffer();
            Systems.Audio.AudioManager.Instance?.PlayReject();
            NegotiationSystem.Reject(currentCustomer);
            dealFeedbackText.text = "You wave them off. They take their goods elsewhere.";
            ConcludeVisit();
        }

        private bool InNegotiation() =>
            currentCustomer != null && currentCustomer.negotiationState == NegotiationState.InProgress;

        private List<ItemInstance> SelectedItems()
        {
            var selected = new List<ItemInstance>();
            foreach (var row in itemRows)
            {
                if (row.selectToggle.isOn) selected.Add(row.item);
            }

            return selected;
        }

        /// <summary>Unchecking an item changes the deal, so the customer re-anchors their ask.</summary>
        private void OnSelectionChanged()
        {
            if (!InNegotiation()) return;
            NegotiationSystem.RepriceForSelection(currentCustomer, SelectedItems());
            UpdateMoodAskingLine();
            RefreshBuyLabel();
            dealFeedbackText.text = string.Empty;
        }

        private string LeftoverSuffix() =>
            currentCustomer.items.Count > 0 ? " They pocket what you passed on." : string.Empty;

        // ---- Armed Offer state ----------------------------------------------

        /// <summary>
        /// Offer pressed with nothing to submit: pulse the button gold and hand
        /// focus to the amount field so typing can start without another click.
        /// </summary>
        private void ArmOffer()
        {
            dealFeedbackText.text = OfferArmHint;
            offerArmedFrame = Time.frameCount;
            offerInput.Select();
            offerInput.ActivateInputField();

            if (offerArmed) return;
            offerArmed = true;
            offerPulse = StartCoroutine(OfferPulseRoutine());
        }

        private void DisarmOffer()
        {
            if (offerPulse != null)
            {
                StopCoroutine(offerPulse);
                offerPulse = null;
            }

            offerArmed = false;
            if (offerButtonImage != null) offerButtonImage.color = ButtonColor;
            if (dealFeedbackText.text == OfferArmHint) dealFeedbackText.text = string.Empty;
        }

        /// <summary>Slow gold pulse — same family as the cash figures, unmistakably not the neutral cyan.</summary>
        private IEnumerator OfferPulseRoutine()
        {
            while (true)
            {
                float wave = (Mathf.Sin(Time.unscaledTime * (2f * Mathf.PI / UITheme.FocusPulsePeriod)) + 1f) * 0.5f;
                offerButtonImage.color = Color.Lerp(UITheme.Gold, UITheme.GoldBright, wave);
                yield return null;
            }
        }

        // ---- Onboarding tips (GDD 33 onboarding, Hebrew RTL) ------------------

        private void BuildTipBubble(RectTransform panel)
        {
            tipsEnabled = PlayerPrefs.GetInt(TipsEnabledKey, 1) == 1;

            var rowGO = new GameObject("TipBubble", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(panel, false);
            tipBackground = rowGO.GetComponent<Image>();
            tipBackground.sprite = UITheme.RoundedSprite;
            tipBackground.type = Image.Type.Sliced;
            tipBackground.raycastTarget = false;

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // "?" first (leftmost), so the Hebrew text reads from the right edge.
            var toggleLabel = CreateSmallButton(rowGO.transform, "?", 40f, OnTipToggleClicked);
            toggleLabel.transform.parent.GetComponent<LayoutElement>().preferredHeight = 36f;

            tipText = CreateText(rowGO.transform, "TipText", 19f, TextAlignmentOptions.Right);
            tipText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            UpdateTip();
        }

        private void OnTipToggleClicked()
        {
            tipsEnabled = !tipsEnabled;
            PlayerPrefs.SetInt(TipsEnabledKey, tipsEnabled ? 1 : 0);
            UpdateTip();
        }

        /// <summary>Re-picks the tip for the current moment; hides the bubble when tips are off.</summary>
        private void UpdateTip()
        {
            if (tipText == null) return;

            if (!tipsEnabled)
            {
                tipText.text = string.Empty;
                tipBackground.color = Color.clear;
                return;
            }

            tipBackground.color = UITheme.SurfaceRaised;
            tipText.alignment = LanguageManager.IsRtl ? TextAlignmentOptions.Right : TextAlignmentOptions.Left;
            Loc.Set(tipText, Loc.T(ChooseTipKey()));
        }

        /// <summary>One sentence for where the player actually is, not a manual.</summary>
        private string ChooseTipKey()
        {
            if (!InNegotiation())
            {
                return gm.Day != null && gm.Day.CustomersRemaining > 0
                    ? LanguageManager.Keys.TipNextCustomer
                    : LanguageManager.Keys.TipCloseShop;
            }

            foreach (var row in itemRows)
            {
                if (row.item.timesInspected == 0) return LanguageManager.Keys.TipInspect;
            }

            return currentCustomer.offersMade == 0
                ? LanguageManager.Keys.TipOpenLow
                : LanguageManager.Keys.TipHaggle;
        }

        /// <summary>Focus left the amount field: clicking away cancels the armed state.</summary>
        private void OnOfferInputEndEdit()
        {
            if (!offerArmed) return;

            // Arming re-focuses the field in the same frame TMP deactivates it
            // (Enter on an empty field) — restore focus instead of disarming.
            if (Time.frameCount == offerArmedFrame)
            {
                offerInput.ActivateInputField();
                return;
            }

            DisarmOffer();
        }

        private void ConcludeVisit()
        {
            DisarmOffer();
            ClearItemRows();
            dealControls.SetActive(false);
            customerMoodText.text = $"Mood: {currentCustomer.mood}";
            RefreshActionButton();
            UpdateTip();
        }

        private void UpdateMoodAskingLine()
        {
            string asking = currentCustomer.askingPrice > 0 ? $"${currentCustomer.askingPrice:N0}" : "—";
            customerMoodText.text = $"Mood: {currentCustomer.mood}     Asking: {asking}";
        }

        private void RefreshBuyLabel()
        {
            if (currentCustomer == null) return;
            Loc.Set(buyLabel, currentCustomer.askingPrice > 0
                ? Loc.F(LanguageManager.Keys.BuyAmount, currentCustomer.askingPrice.ToString("N0"))
                : Loc.T(LanguageManager.Keys.Buy) + "  —");
        }

        // ---- Item rows (counter + inspection, GDD 12) -----------------------

        private void RebuildItemRows(CustomerInstance customer)
        {
            ClearItemRows();
            for (int i = 0; i < customer.items.Count; i++)
            {
                CreateItemRow(customer.items[i]);

                // Staggered entrance: the customer places items on the counter
                // one at a time. Skippable with the rest of the juice.
                var group = itemRows[itemRows.Count - 1].root.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                pendingRowFades.Add(group);
                var routine = UIFx.FadeInAfter(this, group, 0.08f + i * 0.07f);
                if (routine != null) rowFadeRoutines.Add(routine);
            }
        }

        private void ClearItemRows()
        {
            foreach (var routine in rowFadeRoutines)
            {
                if (routine != null) StopCoroutine(routine);
            }

            rowFadeRoutines.Clear();
            pendingRowFades.Clear();

            foreach (var row in itemRows) Destroy(row.root);
            itemRows.Clear();
        }

        private void CreateItemRow(ItemInstance item)
        {
            var rowGO = new GameObject("ItemRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(itemsContainer, false);
            var rowImage = rowGO.GetComponent<Image>();
            rowImage.color = UITheme.SurfaceRaised;
            rowImage.sprite = UITheme.RoundedSprite;
            rowImage.type = Image.Type.Sliced;

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var row = new ItemRow { item = item, root = rowGO };

            // Checkbox deciding whether this item is part of the deal (all on by default).
            var toggleGO = new GameObject("SelectToggle", typeof(RectTransform), typeof(Image), typeof(Toggle));
            toggleGO.transform.SetParent(rowGO.transform, false);
            var toggleLayout = toggleGO.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 36f;
            toggleLayout.preferredHeight = 36f;

            var boxImage = toggleGO.GetComponent<Image>();
            boxImage.color = UITheme.Surface;
            boxImage.sprite = UITheme.RoundedSprite;
            boxImage.type = Image.Type.Sliced;

            var checkGO = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGO.transform.SetParent(toggleGO.transform, false);
            var checkRect = (RectTransform)checkGO.transform;
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.offsetMin = new Vector2(8f, 8f);
            checkRect.offsetMax = new Vector2(-8f, -8f);
            var checkImage = checkGO.GetComponent<Image>();
            checkImage.color = ButtonColor;
            checkImage.sprite = UITheme.RoundedSprite;
            checkImage.type = Image.Type.Sliced;
            checkImage.raycastTarget = false;

            row.selectToggle = toggleGO.GetComponent<Toggle>();
            row.selectToggle.targetGraphic = boxImage;
            row.selectToggle.graphic = checkImage;
            row.selectToggle.isOn = true;
            row.selectToggle.onValueChanged.AddListener(_ => OnSelectionChanged());

            // Category glyph so the item reads at a glance, before the name does.
            UIIcons.CreateIconChip(rowGO.transform, ItemGenerator.GetDefinition(item.definitionId));

            row.infoText = CreateText(rowGO.transform, "Info", 20f, TextAlignmentOptions.Left);
            row.infoText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var buttonGO = new GameObject("InspectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(rowGO.transform, false);
            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.preferredHeight = 46f;

            row.inspectImage = buttonGO.GetComponent<Image>();
            row.inspectImage.color = ButtonColor;
            row.inspectImage.sprite = UITheme.RoundedSprite;
            row.inspectImage.type = Image.Type.Sliced;

            row.inspectButton = buttonGO.GetComponent<Button>();
            row.inspectButton.targetGraphic = row.inspectImage;
            row.inspectButton.onClick.AddListener(() => OnInspectClicked(row));
            buttonGO.AddComponent<ButtonFX>();

            row.inspectLabel = CreateText(buttonGO.transform, "Label", 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            row.inspectLabel.color = ButtonTextColor;
            var labelRect = (RectTransform)row.inspectLabel.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            itemRows.Add(row);
            RefreshItemRow(row);
        }

        private void OnInspectClicked(ItemRow row)
        {
            InspectionSystem.Inspect(gm.State, row.item, currentArchetype);
            RefreshItemRow(row);
            UpdateTip();
        }

        /// <summary>
        /// Renders only what the player knows (GDD 11): condition and value stay "?"
        /// until the matching KnowledgeFlags are earned through inspection, and risk
        /// only ever surfaces as clue text — never as a definitive label.
        /// </summary>
        private void RefreshItemRow(ItemRow row)
        {
            var item = row.item;
            var definition = ItemGenerator.GetDefinition(item.definitionId);
            string name = definition != null ? definition.displayName : item.definitionId;
            string category = definition != null ? definition.category.ToString() : "?";

            string condition = item.playerKnowledge.HasFlag(KnowledgeFlags.ConditionAssessed)
                ? item.condition.ToString()
                : "?";

            string value = "?";
            if (item.playerKnowledge.HasFlag(KnowledgeFlags.ValueAppraised))
            {
                InspectionSystem.GetValueEstimate(gm.State, item, out int low, out int high);
                value = $"~${low:N0}–${high:N0}";
            }

            var sb = new StringBuilder();
            sb.Append($"{name}   <color=#9E9A90>{category}</color>");
            sb.Append($"\n<size=85%>Condition: {condition} · Value: {value}</size>");
            foreach (var clue in item.knownClues)
            {
                sb.Append($"\n<size=85%><color=#C9B458>»</color> <color=#B8B4AA>{clue}</color></size>");
            }

            row.infoText.text = sb.ToString();

            bool canInspect = InspectionSystem.CanInspect(item);
            row.inspectButton.interactable = canInspect;
            Loc.Set(row.inspectLabel, canInspect
                ? Loc.F(LanguageManager.Keys.Inspect, InspectionSystem.InspectionsLeft(item))
                : Loc.T(LanguageManager.Keys.Inspected));
            row.inspectImage.color = canInspect ? ButtonColor : UITheme.DisabledButton;
            row.inspectLabel.color = canInspect ? ButtonTextColor : UITheme.DisabledLabel;
        }

        private void OnPhaseChanged(GamePhase phase) => RefreshActionButton();

        private void OnActionClicked()
        {
            switch (gm.Phase)
            {
                case GamePhase.DayActive:
                    if (gm.Day.NextCustomer() == null) gm.EndDay();
                    break;
                case GamePhase.DaySummary:
                    gm.AdvanceToNextDay();
                    break;
            }
        }

        private void RefreshActionButton()
        {
            switch (gm.Phase)
            {
                case GamePhase.DayActive:
                    Loc.Set(actionLabel, Loc.T(gm.Day.CustomersRemaining > 0
                        ? LanguageManager.Keys.NextCustomer
                        : LanguageManager.Keys.CloseShop));
                    break;
                case GamePhase.DaySummary:
                    Loc.Set(actionLabel, Loc.F(LanguageManager.Keys.OpenDay, gm.State.currentDay + 1));
                    break;
                case GamePhase.GameOver:
                    Loc.Set(actionLabel, Loc.T(LanguageManager.Keys.GameOver));
                    break;
                case GamePhase.Victory:
                    Loc.Set(actionLabel, Loc.T(LanguageManager.Keys.Victory));
                    break;
            }
        }

        private static string PickDialogueLine(CustomerArchetypeDefinition archetype)
        {
            if (archetype == null || archetype.dialoguePool.Count == 0) return "...";
            return archetype.dialoguePool[Random.Range(0, archetype.dialoguePool.Count)];
        }

        // ---- Construction --------------------------------------------------

        private void BuildHud()
        {
            var canvasGO = new GameObject("HUDCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            hudCanvas = canvasGO.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            BuildTopBar(hudCanvas.transform);
            Debug.Log("[HUD] Top bar built (5 resource labels).");
            BuildGameplayPanel(hudCanvas.transform);
            Debug.Log("[HUD] Gameplay panel built (customer panel, action button, Inventory/Upgrades buttons).");

            // The rig is built before the first scene loads, so don't wait for the
            // canvas system to schedule a rebuild — lay everything out right now.
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(topBarRect);
            LayoutRebuilder.ForceRebuildLayoutImmediate(customerPanelRect);
            Debug.Log("[HUD] BuildHud complete — forced immediate layout rebuild.");

            UIFx.FadeIn(this, hudCanvas.gameObject);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                Debug.Log($"[HUD] EventSystem already present: '{EventSystem.current.gameObject.name}'.");
                return;
            }

            var esGO = new GameObject("EventSystem", typeof(EventSystem),
#if ENABLE_INPUT_SYSTEM
                typeof(InputSystemUIInputModule));
#else
                typeof(StandaloneInputModule));
#endif
            esGO.transform.SetParent(transform, false);
            Debug.Log($"[HUD] EventSystem created with {esGO.GetComponent<BaseInputModule>().GetType().Name}.");
        }

        private void BuildTopBar(Transform canvas)
        {
            var bar = CreatePanel(canvas, "TopBar", BarColor);
            topBarRect = bar;
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = Vector2.one;
            bar.pivot = new Vector2(0.5f, 1f);
            bar.anchoredPosition = Vector2.zero;
            bar.sizeDelta = new Vector2(0f, 64f);

            var layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 12, 12);
            layout.spacing = 32f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            dayText = CreateText(bar, "DayText", 26f, TextAlignmentOptions.Left, FontStyles.Bold, header: true);
            dayText.color = UITheme.NeonCyan;
            cashText = CreateText(bar, "CashText", 24f, TextAlignmentOptions.Left);
            cashText.color = UITheme.Gold;
            reputationText = CreateText(bar, "ReputationText", 24f, TextAlignmentOptions.Left);
            heatText = CreateText(bar, "HeatText", 24f, TextAlignmentOptions.Left);
            heatText.color = UITheme.Danger;
            debtText = CreateText(bar, "DebtText", 24f, TextAlignmentOptions.Right);
        }

        private void BuildGameplayPanel(Transform canvas)
        {
            // Fills everything under the top bar; the shop screen proper (GDD 32.1 A).
            // Barely tinted now that ShopSceneBackdrop paints a real room behind
            // this canvas — its own readability scrim does the dimming; this is
            // just a faint navy cast so the top bar's color carries down.
            var gameplay = CreatePanel(canvas, "GameplayPanel", new Color(
                UITheme.Background.r, UITheme.Background.g, UITheme.Background.b, 0.08f));
            gameplay.anchorMin = Vector2.zero;
            gameplay.anchorMax = Vector2.one;
            gameplay.offsetMin = Vector2.zero;
            gameplay.offsetMax = new Vector2(0f, -64f);

            var panel = CreatePanel(gameplay, "CustomerPanel", PanelColor, rounded: true);
            customerPanelRect = panel;
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            // Tuned against the real Shop_Empty/Shop_Full art: the painted counter
            // surface sits in roughly the bottom quarter of frame after the cover-fit
            // crop, so the panel's BOTTOM edge rests near that line (like a ledger
            // standing on the counter) and the panel extends upward from there,
            // leaving the shelves/lamps above it visible. Sparse/Stocked use a
            // different camera composition, so this is a best-fit compromise, not
            // a pixel-perfect match for all four.
            panel.anchoredPosition = new Vector2(0f, 15f);
            panel.sizeDelta = new Vector2(860f, 600f);
            AddPanelShadow(panel);

            // Slightly translucent so the counter reads through at the card's
            // edges — the UI as part of the room, not a rectangle floating over it.
            var panelImage = panel.GetComponent<Image>();
            panelImage.color = new Color(panelImage.color.r, panelImage.color.g, panelImage.color.b, 0.88f);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            customerNameText = CreateText(panel, "NameText", 34f, TextAlignmentOptions.Left, FontStyles.Bold, header: true);
            customerMoodText = CreateText(panel, "MoodText", 22f, TextAlignmentOptions.Left);
            customerDialogueText = CreateText(panel, "DialogueText", 24f, TextAlignmentOptions.Left, FontStyles.Italic);

            var containerGO = new GameObject("ItemsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerGO.transform.SetParent(panel, false);
            itemsContainer = (RectTransform)containerGO.transform;
            var itemsLayout = containerGO.GetComponent<VerticalLayoutGroup>();
            itemsLayout.spacing = 8f;
            itemsLayout.childAlignment = TextAnchor.UpperLeft;
            itemsLayout.childControlWidth = true;
            itemsLayout.childControlHeight = true;
            itemsLayout.childForceExpandWidth = true;
            itemsLayout.childForceExpandHeight = false;

            BuildDealControls(panel);

            dealFeedbackText = CreateText(panel, "DealFeedbackText", 20f, TextAlignmentOptions.Left, FontStyles.Italic);
            dealFeedbackText.color = UITheme.Gold;

            queueText = CreateText(panel, "QueueText", 18f, TextAlignmentOptions.Left);
            queueText.color = MutedColor;

            BuildTipBubble(panel);

            BuildActionButton(gameplay);

            // Inventory screen toggle, bottom-left (GDD 32.1 B).
            var inventoryLabel = CreateSmallButton(gameplay, "Inventory", 200f,
                () => InventoryUIManager.Instance?.Toggle());
            LocalizedLabel.Bind(inventoryLabel, LanguageManager.Keys.Inventory);
            var inventoryRect = (RectTransform)inventoryLabel.transform.parent;
            inventoryRect.anchorMin = inventoryRect.anchorMax = Vector2.zero;
            inventoryRect.pivot = Vector2.zero;
            inventoryRect.anchoredPosition = new Vector2(36f, 36f);
            inventoryRect.sizeDelta = new Vector2(220f, 68f);

            // Upgrade shop toggle next to it (GDD 23.1).
            var upgradesLabel = CreateSmallButton(gameplay, "Upgrades", 200f,
                () => UpgradeUIManager.Instance?.Toggle());
            LocalizedLabel.Bind(upgradesLabel, LanguageManager.Keys.Upgrades);
            var upgradesRect = (RectTransform)upgradesLabel.transform.parent;
            upgradesRect.anchorMin = upgradesRect.anchorMax = Vector2.zero;
            upgradesRect.pivot = Vector2.zero;
            upgradesRect.anchoredPosition = new Vector2(272f, 36f);
            upgradesRect.sizeDelta = new Vector2(220f, 68f);
        }

        private void BuildActionButton(RectTransform parent)
        {
            var buttonGO = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(parent, false);

            var rect = (RectTransform)buttonGO.transform;
            actionButtonRect = rect;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 36f);
            rect.sizeDelta = new Vector2(300f, 68f);
            Debug.Log("[HUD] Action button created.");

            var image = buttonGO.GetComponent<Image>();
            image.color = ButtonColor;
            image.sprite = UITheme.RoundedSprite;
            image.type = Image.Type.Sliced;

            var shadow = buttonGO.AddComponent<Shadow>();
            shadow.effectColor = UITheme.ShadowTint;
            shadow.effectDistance = new Vector2(0f, -4f);

            var button = buttonGO.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnActionClicked);
            buttonGO.AddComponent<ButtonFX>();

            actionLabel = CreateText(buttonGO.transform, "Label", 26f, TextAlignmentOptions.Center, FontStyles.Bold);
            actionLabel.color = ButtonTextColor;
            var labelRect = (RectTransform)actionLabel.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;
        }

        private void BuildDealControls(RectTransform panel)
        {
            dealControls = new GameObject("DealControls", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            dealControls.transform.SetParent(panel, false);

            var layout = dealControls.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            offerInput = CreateOfferInput(dealControls.transform);
            offerInput.onSubmit.AddListener(_ => OnOfferClicked());
            offerInput.onEndEdit.AddListener(_ => OnOfferInputEndEdit());

            var offerLabel = CreateSmallButton(dealControls.transform, "Offer", 110f, OnOfferClicked);
            offerButtonImage = offerLabel.transform.parent.GetComponent<Image>();
            LocalizedLabel.Bind(offerLabel, LanguageManager.Keys.Offer);

            buyLabel = CreateSmallButton(dealControls.transform, "Buy", 190f, OnBuyClicked);
            var rejectLabel = CreateSmallButton(dealControls.transform, "Reject", 110f, OnRejectClicked);
            LocalizedLabel.Bind(rejectLabel, LanguageManager.Keys.Reject);

            dealControls.SetActive(false);
        }

        private TMP_InputField CreateOfferInput(Transform parent)
        {
            var go = new GameObject("OfferInput", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var inputImage = go.GetComponent<Image>();
            inputImage.color = UITheme.SurfaceRaised;
            inputImage.sprite = UITheme.RoundedSprite;
            inputImage.type = Image.Type.Sliced;
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = 170f;
            layoutElement.preferredHeight = 46f;

            var input = go.AddComponent<TMP_InputField>();

            var viewportGO = new GameObject("TextArea", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(go.transform, false);
            var viewport = (RectTransform)viewportGO.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(12f, 6f);
            viewport.offsetMax = new Vector2(-12f, -6f);

            var text = CreateText(viewport, "Text", 20f, TextAlignmentOptions.Left);
            var textRect = (RectTransform)text.transform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = textRect.offsetMax = Vector2.zero;

            var placeholder = CreateText(viewport, "Placeholder", 20f, TextAlignmentOptions.Left, FontStyles.Italic);
            placeholder.text = "offer $";
            placeholder.color = MutedColor;
            var placeholderRect = (RectTransform)placeholder.transform;
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = placeholderRect.offsetMax = Vector2.zero;

            input.textViewport = viewport;
            input.textComponent = text;
            input.placeholder = placeholder;
            input.contentType = TMP_InputField.ContentType.IntegerNumber;

            return input;
        }

        internal static TextMeshProUGUI CreateSmallButton(Transform parent, string label, float width, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var layoutElement = go.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = width;
            layoutElement.preferredHeight = 46f;

            var image = go.GetComponent<Image>();
            image.color = ButtonColor;
            image.sprite = UITheme.RoundedSprite;
            image.type = Image.Type.Sliced;

            var shadow = go.AddComponent<Shadow>();
            shadow.effectColor = UITheme.ShadowTint;
            shadow.effectDistance = new Vector2(0f, -3f);

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);
            go.AddComponent<ButtonFX>();

            var text = CreateText(go.transform, "Label", 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            text.text = label;
            text.color = ButtonTextColor;
            var labelRect = (RectTransform)text.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            return text;
        }

        internal static RectTransform CreatePanel(Transform parent, string name, Color color, bool rounded = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            if (rounded)
            {
                image.sprite = UITheme.RoundedSprite;
                image.type = Image.Type.Sliced;
            }

            return (RectTransform)go.transform;
        }

        /// <summary>Soft drop shadow behind a fixed-anchor panel: a sibling rendered first, slightly larger and lower.</summary>
        internal static void AddPanelShadow(RectTransform panel)
        {
            var go = new GameObject(panel.name + "Shadow", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(panel.parent, false);
            go.transform.SetSiblingIndex(panel.GetSiblingIndex());

            var rect = (RectTransform)go.transform;
            rect.anchorMin = panel.anchorMin;
            rect.anchorMax = panel.anchorMax;
            rect.pivot = panel.pivot;
            rect.anchoredPosition = panel.anchoredPosition + new Vector2(0f, -8f);
            rect.sizeDelta = panel.sizeDelta + new Vector2(36f, 36f);

            var image = go.GetComponent<Image>();
            image.sprite = UITheme.SoftShadowSprite;
            image.type = Image.Type.Sliced;
            image.color = UITheme.ShadowTint;
            image.raycastTarget = false;
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string name, float size,
            TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal, bool header = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = TextColor;
            text.text = string.Empty;
            if (header && UITheme.HeaderFont != null)
            {
                text.font = UITheme.HeaderFont;
                text.characterSpacing = UITheme.HeaderCharacterSpacing;
            }

            return text;
        }
    }
}
