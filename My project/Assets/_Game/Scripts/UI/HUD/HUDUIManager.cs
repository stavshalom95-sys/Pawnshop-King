using System.Collections.Generic;
using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Data;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Inspection;
using PawnshopKing.Systems.Items;
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
        // Shared with other code-built screens (InventoryUIManager).
        internal static readonly Color BarColor = new Color(0.07f, 0.08f, 0.11f, 0.95f);
        internal static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.95f);
        internal static readonly Color TextColor = new Color(0.92f, 0.90f, 0.86f);
        internal static readonly Color MutedColor = new Color(0.62f, 0.60f, 0.56f);
        internal static readonly Color ButtonColor = new Color(0.83f, 0.62f, 0.15f);
        internal static readonly Color ButtonTextColor = new Color(0.12f, 0.09f, 0.03f);

        private GameManager gm;

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

        private CustomerInstance currentCustomer;
        private CustomerArchetypeDefinition currentArchetype;

        private readonly List<ItemRow> itemRows = new List<ItemRow>();

        /// <summary>One counter row: the item, its info text, and its Inspect button.</summary>
        private class ItemRow
        {
            public ItemInstance item;
            public GameObject root;
            public TextMeshProUGUI infoText;
            public Button inspectButton;
            public TextMeshProUGUI inspectLabel;
            public Image inspectImage;
        }

        // Change-detection cache for the resource bar.
        private int lastDay = int.MinValue, lastCash, lastReputation, lastHeat, lastDebt, lastPayment, lastDebtDays;

        private void Awake()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("HUDUIManager requires GameManager to exist first — add it after GameManager in SceneInitializer.");
                enabled = false;
                return;
            }

            BuildHud();

            gm.PhaseChanged += OnPhaseChanged;
            gm.Day.DayStarted += OnDayStarted;
            gm.Day.CustomerArrived += OnCustomerArrived;
            gm.Day.DayEnded += OnDayEnded;
        }

        private void OnDestroy()
        {
            if (gm == null) return;
            gm.PhaseChanged -= OnPhaseChanged;
            if (gm.Day == null) return;
            gm.Day.DayStarted -= OnDayStarted;
            gm.Day.CustomerArrived -= OnCustomerArrived;
            gm.Day.DayEnded -= OnDayEnded;
        }

        private void Update()
        {
            var s = gm.State;
            if (s == null) return;

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
            cashText.text = $"Cash  ${s.cash:N0}";
            reputationText.text = $"Rep  {s.reputation}";
            heatText.text = $"Heat  {s.heat}";
            debtText.text = s.debt.totalDebt > 0
                ? $"Debt  ${s.debt.totalDebt:N0}  (${s.debt.nextPaymentAmount:N0} due in {s.debt.daysUntilPayment}d)"
                : "Debt  cleared";
        }

        // ---- Event handlers ------------------------------------------------

        private void OnDayStarted(int day)
        {
            currentCustomer = null;
            currentArchetype = null;
            customerNameText.text = "Shop is open";
            customerMoodText.text = string.Empty;
            customerDialogueText.text = "Waiting for the first customer...";
            ClearItemRows();
            dealControls.SetActive(false);
            dealFeedbackText.text = string.Empty;
            queueText.text = $"{gm.Day.CustomersRemaining} customers in the queue today";
            RefreshActionButton();
        }

        private void OnCustomerArrived(CustomerInstance customer)
        {
            currentCustomer = customer;
            currentArchetype = gm.Day.GetArchetype(customer.archetypeId);

            customerNameText.text = currentArchetype != null ? currentArchetype.displayName : customer.archetypeId;
            customerDialogueText.text = $"“{PickDialogueLine(currentArchetype)}”";
            UpdateMoodAskingLine();
            RebuildItemRows(customer);

            offerInput.text = string.Empty;
            bool tradable = customer.items.Count > 0;
            dealControls.SetActive(tradable);
            dealFeedbackText.text = tradable ? string.Empty : "They have nothing you'd trade for.";
            RefreshBuyLabel();

            queueText.text = gm.Day.CustomersRemaining == 0
                ? "Last customer of the day"
                : $"{gm.Day.CustomersRemaining} more waiting outside";
            RefreshActionButton();
        }

        private void OnDayEnded(int day)
        {
            customerNameText.text = "Shop closed";
            customerMoodText.text = string.Empty;
            customerDialogueText.text = $"Day {day} is over. The debt clock ticks on.";
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

            if (!int.TryParse(offerInput.text, out int amount) || amount <= 0)
            {
                dealFeedbackText.text = "Enter an offer amount first.";
                return;
            }

            if (amount > gm.State.cash)
            {
                dealFeedbackText.text = "You can't cover that offer.";
                return;
            }

            var result = NegotiationSystem.MakeOffer(gm.State, currentCustomer, currentArchetype, amount);
            switch (result.outcome)
            {
                case OfferOutcome.Accepted:
                    dealFeedbackText.text = $"Deal. You hand over ${result.price:N0}.";
                    ConcludeVisit();
                    break;
                case OfferOutcome.AcceptedReluctantly:
                    dealFeedbackText.text = $"“Fine. Just give me the money.” You pay ${result.price:N0}.";
                    ConcludeVisit();
                    break;
                case OfferOutcome.Countered:
                    dealFeedbackText.text = $"“Make it ${result.price:N0} and we're done.”";
                    UpdateMoodAskingLine();
                    RefreshBuyLabel();
                    break;
                case OfferOutcome.OffendedLeft:
                    dealFeedbackText.text = "“Insulting.” They storm out. Word gets around. (Reputation -1)";
                    ConcludeVisit();
                    break;
                case OfferOutcome.GaveUpLeft:
                    dealFeedbackText.text = "“Forget it.” They pack up and leave.";
                    ConcludeVisit();
                    break;
            }
        }

        private void OnBuyClicked()
        {
            if (!InNegotiation()) return;

            if (currentCustomer.askingPrice > gm.State.cash)
            {
                dealFeedbackText.text = "You don't have the cash for their price.";
                return;
            }

            var result = NegotiationSystem.BuyAtAskingPrice(gm.State, currentCustomer, currentArchetype);
            dealFeedbackText.text = $"Bought at asking price — ${result.price:N0}. Fair dealing. (Reputation +1)";
            ConcludeVisit();
        }

        private void OnRejectClicked()
        {
            if (!InNegotiation()) return;

            NegotiationSystem.Reject(currentCustomer);
            dealFeedbackText.text = "You wave them off. They take their goods elsewhere.";
            ConcludeVisit();
        }

        private bool InNegotiation() =>
            currentCustomer != null && currentCustomer.negotiationState == NegotiationState.InProgress;

        private void ConcludeVisit()
        {
            ClearItemRows();
            dealControls.SetActive(false);
            customerMoodText.text = $"Mood: {currentCustomer.mood}";
            RefreshActionButton();
        }

        private void UpdateMoodAskingLine()
        {
            customerMoodText.text = $"Mood: {currentCustomer.mood}     Asking: ${currentCustomer.askingPrice:N0}";
        }

        private void RefreshBuyLabel()
        {
            if (currentCustomer != null) buyLabel.text = $"Buy  ${currentCustomer.askingPrice:N0}";
        }

        // ---- Item rows (counter + inspection, GDD 12) -----------------------

        private void RebuildItemRows(CustomerInstance customer)
        {
            ClearItemRows();
            foreach (var item in customer.items) CreateItemRow(item);
        }

        private void ClearItemRows()
        {
            foreach (var row in itemRows) Destroy(row.root);
            itemRows.Clear();
        }

        private void CreateItemRow(ItemInstance item)
        {
            var rowGO = new GameObject("ItemRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(itemsContainer, false);
            rowGO.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.04f);

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var row = new ItemRow { item = item, root = rowGO };

            row.infoText = CreateText(rowGO.transform, "Info", 20f, TextAlignmentOptions.Left);
            row.infoText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var buttonGO = new GameObject("InspectButton", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonGO.transform.SetParent(rowGO.transform, false);
            var buttonLayout = buttonGO.AddComponent<LayoutElement>();
            buttonLayout.preferredWidth = 150f;
            buttonLayout.preferredHeight = 46f;

            row.inspectImage = buttonGO.GetComponent<Image>();
            row.inspectImage.color = ButtonColor;

            row.inspectButton = buttonGO.GetComponent<Button>();
            row.inspectButton.targetGraphic = row.inspectImage;
            row.inspectButton.onClick.AddListener(() => OnInspectClicked(row));

            row.inspectLabel = CreateText(buttonGO.transform, "Label", 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            row.inspectLabel.color = new Color(0.12f, 0.09f, 0.03f);
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
            row.inspectLabel.text = canInspect ? $"Inspect ({InspectionSystem.InspectionsLeft(item)})" : "Inspected";
            row.inspectImage.color = canInspect ? ButtonColor : new Color(0.35f, 0.33f, 0.30f);
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
                    actionLabel.text = gm.Day.CustomersRemaining > 0 ? "Next Customer" : "Close Shop";
                    break;
                case GamePhase.DaySummary:
                    actionLabel.text = $"Open Day {gm.State.currentDay + 1}";
                    break;
                case GamePhase.GameOver:
                    actionLabel.text = "Game Over";
                    break;
                case GamePhase.Victory:
                    actionLabel.text = "Campaign Complete";
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

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            BuildTopBar(canvas.transform);
            BuildGameplayPanel(canvas.transform);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var esGO = new GameObject("EventSystem", typeof(EventSystem),
#if ENABLE_INPUT_SYSTEM
                typeof(InputSystemUIInputModule));
#else
                typeof(StandaloneInputModule));
#endif
            esGO.transform.SetParent(transform, false);
        }

        private void BuildTopBar(Transform canvas)
        {
            var bar = CreatePanel(canvas, "TopBar", BarColor);
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

            dayText = CreateText(bar, "DayText", 26f, TextAlignmentOptions.Left, FontStyles.Bold);
            cashText = CreateText(bar, "CashText", 24f, TextAlignmentOptions.Left);
            reputationText = CreateText(bar, "ReputationText", 24f, TextAlignmentOptions.Left);
            heatText = CreateText(bar, "HeatText", 24f, TextAlignmentOptions.Left);
            debtText = CreateText(bar, "DebtText", 24f, TextAlignmentOptions.Right);
        }

        private void BuildGameplayPanel(Transform canvas)
        {
            // Fills everything under the top bar; the shop screen proper (GDD 32.1 A).
            var gameplay = CreatePanel(canvas, "GameplayPanel", new Color(0f, 0f, 0f, 0.25f));
            gameplay.anchorMin = Vector2.zero;
            gameplay.anchorMax = Vector2.one;
            gameplay.offsetMin = Vector2.zero;
            gameplay.offsetMax = new Vector2(0f, -64f);

            var panel = CreatePanel(gameplay, "CustomerPanel", PanelColor);
            panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.pivot = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = new Vector2(0f, 10f);
            panel.sizeDelta = new Vector2(860f, 640f);

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(32, 32, 28, 28);
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            customerNameText = CreateText(panel, "NameText", 34f, TextAlignmentOptions.Left, FontStyles.Bold);
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
            dealFeedbackText.color = new Color(0.85f, 0.78f, 0.55f);

            queueText = CreateText(panel, "QueueText", 18f, TextAlignmentOptions.Left);
            queueText.color = MutedColor;

            BuildActionButton(gameplay);

            // Inventory screen toggle, bottom-left (GDD 32.1 B).
            var inventoryLabel = CreateSmallButton(gameplay, "Inventory", 200f,
                () => InventoryUIManager.Instance?.Toggle());
            var inventoryRect = (RectTransform)inventoryLabel.transform.parent;
            inventoryRect.anchorMin = inventoryRect.anchorMax = Vector2.zero;
            inventoryRect.pivot = Vector2.zero;
            inventoryRect.anchoredPosition = new Vector2(36f, 36f);
            inventoryRect.sizeDelta = new Vector2(220f, 68f);

            // Upgrade shop toggle next to it (GDD 23.1).
            var upgradesLabel = CreateSmallButton(gameplay, "Upgrades", 200f,
                () => UpgradeUIManager.Instance?.Toggle());
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
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 36f);
            rect.sizeDelta = new Vector2(300f, 68f);

            var image = buttonGO.GetComponent<Image>();
            image.color = ButtonColor;

            var button = buttonGO.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(OnActionClicked);

            actionLabel = CreateText(buttonGO.transform, "Label", 26f, TextAlignmentOptions.Center, FontStyles.Bold);
            actionLabel.color = new Color(0.12f, 0.09f, 0.03f);
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
            CreateSmallButton(dealControls.transform, "Offer", 110f, OnOfferClicked);
            buyLabel = CreateSmallButton(dealControls.transform, "Buy", 190f, OnBuyClicked);
            CreateSmallButton(dealControls.transform, "Reject", 110f, OnRejectClicked);

            dealControls.SetActive(false);
        }

        private TMP_InputField CreateOfferInput(Transform parent)
        {
            var go = new GameObject("OfferInput", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.16f, 0.17f, 0.21f);
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

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(onClick);

            var text = CreateText(go.transform, "Label", 20f, TextAlignmentOptions.Center, FontStyles.Bold);
            text.text = label;
            text.color = ButtonTextColor;
            var labelRect = (RectTransform)text.transform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = labelRect.offsetMax = Vector2.zero;

            return text;
        }

        internal static RectTransform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return (RectTransform)go.transform;
        }

        internal static TextMeshProUGUI CreateText(Transform parent, string name, float size,
            TextAlignmentOptions alignment, FontStyles style = FontStyles.Normal)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.alignment = alignment;
            text.fontStyle = style;
            text.color = TextColor;
            text.text = string.Empty;
            return text;
        }
    }
}
