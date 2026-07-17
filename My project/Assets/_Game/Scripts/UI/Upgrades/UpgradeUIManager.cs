using System.Text;
using PawnshopKing.Core;
using PawnshopKing.Data.Definitions;
using PawnshopKing.Systems.Localization;
using PawnshopKing.Systems.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Upgrade shop screen (GDD 23.1 Tools), built entirely from code. Lists every
    /// UpgradeDefinition under Resources with its price and effect; buying goes
    /// through UpgradeSystem, which is the single cash/ownership gate. Buttons for
    /// unaffordable upgrades are disabled so cash can never go negative from here.
    /// The top resource bar stays visible, so the cash hit shows immediately.
    /// </summary>
    public class UpgradeUIManager : MonoBehaviour
    {
        public static UpgradeUIManager Instance { get; private set; }

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
                Debug.LogError("UpgradeUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScreen();

            // Close on day transitions alongside the inventory screen, so no overlay
            // ever lingers over the next morning's counter.
            gm.PhaseChanged += OnPhaseChanged;
            LanguageManager.LanguageChanged += OnLanguageChanged;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (gm != null) gm.PhaseChanged -= OnPhaseChanged;
            LanguageManager.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            if (screenRoot.activeSelf) RebuildList();
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

            var upgrades = UpgradeSystem.AllUpgrades;
            int owned = 0;
            foreach (var upgrade in upgrades)
            {
                if (UpgradeSystem.IsOwned(gm.State, upgrade.id)) owned++;
            }

            Loc.Set(titleText, Loc.F(LanguageManager.Keys.UpgradesTitle, owned, upgrades.Count), UITheme.HeaderFont);

            foreach (var upgrade in upgrades) CreateRow(upgrade);
        }

        private void CreateRow(UpgradeDefinition upgrade)
        {
            var rowGO = new GameObject("UpgradeRow", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
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
            Loc.Set(info, BuildInfo(upgrade));

            bool isOwned = UpgradeSystem.IsOwned(gm.State, upgrade.id);
            bool canAfford = gm.State.cash >= upgrade.cost;

            var label = HUDUIManager.CreateSmallButton(rowGO.transform, "Buy", 190f,
                () => OnBuyClicked(upgrade));
            var button = label.transform.parent.GetComponent<Button>();

            if (isOwned)
            {
                Loc.Set(label, Loc.T(LanguageManager.Keys.Installed));
                button.interactable = false;
                label.transform.parent.GetComponent<Image>().color = UITheme.Success;
            }
            else
            {
                Loc.Set(label, Loc.F(LanguageManager.Keys.BuyAmount, upgrade.cost.ToString("N0")));
                if (!canAfford)
                {
                    label.color = UITheme.DisabledLabel;
                    button.interactable = false;
                    label.transform.parent.GetComponent<Image>().color = UITheme.DisabledButton;
                }
            }
        }

        private void OnBuyClicked(UpgradeDefinition upgrade)
        {
            var result = UpgradeSystem.TryPurchase(gm.State, upgrade);
            Loc.Set(feedbackText, ComposePurchaseMessage(upgrade, result));
            RebuildList();
        }

        private static string ComposePurchaseMessage(UpgradeDefinition upgrade, PurchaseResult result)
        {
            switch (result.outcome)
            {
                case PurchaseOutcome.AlreadyOwned:
                    return Loc.F(LanguageManager.Keys.UpgradeAlreadyOwned, upgrade.LocalizedDisplayName);
                case PurchaseOutcome.InsufficientFunds:
                    return Loc.F(LanguageManager.Keys.UpgradeCantAfford, upgrade.LocalizedDisplayName,
                        upgrade.cost.ToString("N0"), GameManager.Instance.State.cash.ToString("N0"));
                default:
                    return Loc.F(LanguageManager.Keys.UpgradeInstalled, upgrade.LocalizedDisplayName, upgrade.cost.ToString("N0"));
            }
        }

        private static string BuildInfo(UpgradeDefinition upgrade)
        {
            var sb = new StringBuilder();
            sb.Append($"{upgrade.LocalizedDisplayName}   <color=#9E9A90>{EffectLabel(upgrade.effect)}</color>");
            sb.Append($"\n<size=85%><color=#B8B4AA>{upgrade.LocalizedDescription}</color></size>");
            return sb.ToString();
        }

        private static string EffectLabel(UpgradeEffect effect)
        {
            switch (effect)
            {
                case UpgradeEffect.ConditionAccuracy: return Loc.T(LanguageManager.Keys.EffectConditionAccuracy);
                case UpgradeEffect.FakeDetection: return Loc.T(LanguageManager.Keys.EffectFakeDetection);
                case UpgradeEffect.ValueAccuracy: return Loc.T(LanguageManager.Keys.EffectValueAccuracy);
                default: return Loc.T(LanguageManager.Keys.EffectTool);
            }
        }

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("UpgradeCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30; // above the HUD and inventory canvases

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Leaves the top resource bar visible, so cash changes show live.
            var root = HUDUIManager.CreatePanel(canvasGO.transform, "UpgradeScreen",
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
            LocalizedLabel.Bind(closeLabel, LanguageManager.Keys.Close);
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
            var scrollGO = new GameObject("UpgradeList", typeof(RectTransform), typeof(ScrollRect));
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
