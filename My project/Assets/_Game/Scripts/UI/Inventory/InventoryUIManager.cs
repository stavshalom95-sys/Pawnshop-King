using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Data;
using PawnshopKing.Data.Runtime;
using PawnshopKing.Systems.Inspection;
using PawnshopKing.Systems.Items;
using PawnshopKing.Systems.Market;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Inventory and selling screen (GDD 32.1 B/C), built entirely from code.
    /// Shows what the player owns filtered through what they know (GDD 11), quotes
    /// all three MVP sell channels per item (GDD 15.1), and sells via MarketSystem.
    /// The top resource bar stays visible, so cash updates are immediate.
    /// </summary>
    public class InventoryUIManager : MonoBehaviour
    {
        public static InventoryUIManager Instance { get; private set; }

        private GameManager gm;
        private GameObject screenRoot;
        private RectTransform listContent;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI feedbackText;

        private void Awake()
        {
            Instance = this;
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("InventoryUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScreen();

            // Day transitions seize/liquidate inventory behind this screen's back —
            // close on any phase change so stale rows are never left clickable.
            gm.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (gm != null) gm.PhaseChanged -= OnPhaseChanged;
        }

        private void OnPhaseChanged(GamePhase phase) => Close();

        public void Toggle()
        {
            if (screenRoot.activeSelf) Close();
            else Open();
        }

        public void Open()
        {
            feedbackText.text = string.Empty;
            RebuildList();
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        public void Close() => screenRoot.SetActive(false);

        // ---- List ------------------------------------------------------------

        private void RebuildList()
        {
            foreach (Transform child in listContent) Destroy(child.gameObject);

            var inventory = gm.State.inventory;
            titleText.text = $"Inventory — {inventory.Count} item{(inventory.Count == 1 ? "" : "s")}";

            foreach (var item in inventory) CreateRow(item);
        }

        private void CreateRow(ItemInstance item)
        {
            var rowGO = new GameObject("InventoryRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(listContent, false);
            var rowImage = rowGO.GetComponent<Image>();
            rowImage.color = UITheme.SurfaceRaised;
            rowImage.sprite = UITheme.RoundedSprite;
            rowImage.type = Image.Type.Sliced;

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(14, 14, 10, 10);
            layout.spacing = 12f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var info = HUDUIManager.CreateText(rowGO.transform, "Info", 20f, TextAlignmentOptions.Left);
            info.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            info.text = BuildInfo(item);

            CreateChannelButton(rowGO.transform, item, SellChannel.Shopfront, "Shop");
            CreateChannelButton(rowGO.transform, item, SellChannel.Collector, "Collector");
            CreateChannelButton(rowGO.transform, item, SellChannel.BlackMarket, "Black Mkt");
        }

        private void CreateChannelButton(Transform parent, ItemInstance item, SellChannel channel, string channelName)
        {
            var quote = MarketSystem.GetQuote(gm.State, item, channel);
            var label = HUDUIManager.CreateSmallButton(parent, channelName, 170f,
                () => OnSellClicked(item, channel));

            var button = label.transform.parent.GetComponent<Button>();
            if (quote.available)
            {
                label.text = $"{channelName} ${quote.price:N0}";
            }
            else
            {
                label.text = channel == SellChannel.BlackMarket ? "Raided — closed" : "No collector";
                label.color = UITheme.DisabledLabel;
                button.interactable = false;
                label.transform.parent.GetComponent<Image>().color = UITheme.DisabledButton;
            }
        }

        private void OnSellClicked(ItemInstance item, SellChannel channel)
        {
            var receipt = MarketSystem.Sell(gm.State, item, channel);
            feedbackText.text = receipt.message;
            RebuildList();
        }

        /// <summary>Knowledge-gated summary (GDD 11): unknowns stay "?" here just like on the counter.</summary>
        private static string BuildInfo(ItemInstance item)
        {
            var definition = ItemGenerator.GetDefinition(item.definitionId);
            string name = definition != null ? definition.displayName : item.definitionId;
            string category = definition != null ? definition.category.ToString() : "?";

            string condition = item.playerKnowledge.HasFlag(KnowledgeFlags.ConditionAssessed)
                ? item.condition.ToString()
                : "?";

            string value = "?";
            if (item.playerKnowledge.HasFlag(KnowledgeFlags.ValueAppraised))
            {
                InspectionSystem.GetValueEstimate(GameManager.Instance.State, item, out int low, out int high);
                value = $"~${low:N0}–${high:N0}";
            }

            var sb = new StringBuilder();
            sb.Append($"{name}   <color=#9E9A90>{category}</color>");
            sb.Append($"\n<size=85%>Condition: {condition} · Value: {value} · Paid ${item.acquisitionPrice:N0}</size>");
            foreach (var clue in item.knownClues)
            {
                sb.Append($"\n<size=85%><color=#C9B458>»</color> <color=#B8B4AA>{clue}</color></size>");
            }

            return sb.ToString();
        }

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("InventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20; // above the HUD canvas

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Leaves the top resource bar visible, so cash changes show live.
            var root = HUDUIManager.CreatePanel(canvasGO.transform, "InventoryScreen",
                new Color(UITheme.Background.r, UITheme.Background.g, UITheme.Background.b, 0.98f), rounded: true);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = new Vector2(60f, 40f);
            root.offsetMax = new Vector2(-60f, -104f);
            screenRoot = root.gameObject;

            titleText = HUDUIManager.CreateText(root, "Title", 30f, TextAlignmentOptions.Left, FontStyles.Bold, header: true);
            var titleRect = (RectTransform)titleText.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.offsetMin = new Vector2(28f, titleRect.offsetMin.y);
            titleRect.offsetMax = new Vector2(-180f, titleRect.offsetMax.y);
            titleRect.sizeDelta = new Vector2(titleRect.sizeDelta.x, 40f);

            var closeLabel = HUDUIManager.CreateSmallButton(root, "Close", 130f, Close);
            var closeRect = (RectTransform)closeLabel.transform.parent;
            closeRect.anchorMin = closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-24f, -20f);
            closeRect.sizeDelta = new Vector2(130f, 48f);

            feedbackText = HUDUIManager.CreateText(root, "Feedback", 20f, TextAlignmentOptions.Left, FontStyles.Italic);
            feedbackText.color = UITheme.Gold;
            var feedbackRect = (RectTransform)feedbackText.transform;
            feedbackRect.anchorMin = new Vector2(0f, 1f);
            feedbackRect.anchorMax = new Vector2(1f, 1f);
            feedbackRect.pivot = new Vector2(0.5f, 1f);
            feedbackRect.anchoredPosition = new Vector2(0f, -72f);
            feedbackRect.offsetMin = new Vector2(28f, feedbackRect.offsetMin.y);
            feedbackRect.offsetMax = new Vector2(-28f, feedbackRect.offsetMax.y);
            feedbackRect.sizeDelta = new Vector2(feedbackRect.sizeDelta.x, 30f);

            BuildScrollList(root);

            screenRoot.SetActive(false);
        }

        private void BuildScrollList(RectTransform root)
        {
            var scrollGO = new GameObject("ItemList", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(root, false);
            var scrollRect = (RectTransform)scrollGO.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(24f, 24f);
            scrollRect.offsetMax = new Vector2(-24f, -116f);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewport = (RectTransform)viewportGO.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            listContent = (RectTransform)contentGO.transform;
            listContent.anchorMin = new Vector2(0f, 1f);
            listContent.anchorMax = new Vector2(1f, 1f);
            listContent.pivot = new Vector2(0.5f, 1f);
            listContent.offsetMin = new Vector2(0f, listContent.offsetMin.y);
            listContent.offsetMax = new Vector2(0f, listContent.offsetMax.y);

            var layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = listContent;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;
        }
    }
}
