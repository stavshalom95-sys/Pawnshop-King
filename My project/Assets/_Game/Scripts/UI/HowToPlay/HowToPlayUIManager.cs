using PawnshopKing.Core;
using PawnshopKing.Systems.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Self-contained "How to Play" overlay: a scrollable list of short
    /// mechanic explanations (inspecting, haggling, Mood, Value). Opens
    /// automatically once at the start of every new campaign (subscribed to
    /// GameManager.NewCampaignStarted, fired before the first customer can
    /// possibly appear), and is also reachable manually from the Main Menu
    /// and Pause Menu for reference later. Doesn't touch game state — purely
    /// explanatory, so it doesn't freeze time or need its own pause logic;
    /// opening it over a paused game just layers on top.
    /// </summary>
    public class HowToPlayUIManager : MonoBehaviour
    {
        public static HowToPlayUIManager Instance { get; private set; }

        private GameObject screenRoot;
        private RectTransform sectionsContent;

        public bool IsOpen => screenRoot != null && screenRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            BuildScreen();

            if (GameManager.Instance != null) GameManager.Instance.NewCampaignStarted += Open;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (GameManager.Instance != null) GameManager.Instance.NewCampaignStarted -= Open;
        }

        public void Open()
        {
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);

            // Sections were built (and their localized text set) once, back when
            // this screen was constructed inactive-about-to-toggle — before the
            // nested VerticalLayoutGroups had converged on real widths. TMP wraps
            // and right-aligns against whatever width it had at that moment, so
            // without this the first-ever Open() renders with the start of every
            // RTL line clipped off. Refreshing on every Open is cheap and matches
            // how every other screen in this project relayouts after localized
            // text changes (see PauseMenuUIManager.RefreshDynamicSettings).
            LayoutRebuilder.ForceRebuildLayoutImmediate(sectionsContent);
        }

        public void Close() => screenRoot.SetActive(false);

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("HowToPlayCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60; // above the pause menu (50), so it works opened from either

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var dim = HUDUIManager.CreatePanel(canvasGO.transform, "HowToPlayDim", new Color(0f, 0f, 0f, 0.85f));
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            screenRoot = dim.gameObject;

            var wrapperGO = new GameObject("Panel", typeof(RectTransform));
            wrapperGO.transform.SetParent(dim, false);
            var wrapperRect = (RectTransform)wrapperGO.transform;
            wrapperRect.anchorMin = wrapperRect.anchorMax = new Vector2(0.5f, 0.5f);
            wrapperRect.pivot = new Vector2(0.5f, 0.5f);
            wrapperRect.sizeDelta = new Vector2(760f, 680f);

            var panel = HUDUIManager.CreatePanel(wrapperRect, "PanelBg", HUDUIManager.PanelColor, rounded: true);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            HUDUIManager.AddPanelShadow(panel);

            var title = HUDUIManager.CreateText(panel, "Title", 32f, TextAlignmentOptions.Center, FontStyles.Bold, header: true);
            title.color = UITheme.NeonCyan;
            var titleRect = (RectTransform)title.transform;
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -24f);
            titleRect.sizeDelta = new Vector2(0f, 44f);
            LocalizedLabel.Bind(title, LanguageManager.Keys.HowToPlayTitle);

            var closeLabel = HUDUIManager.CreateSmallButton(panel, "Close", 140f, Close);
            LocalizedLabel.Bind(closeLabel, LanguageManager.Keys.Close);
            var closeRect = (RectTransform)closeLabel.transform.parent;
            closeRect.anchorMin = closeRect.anchorMax = new Vector2(0.5f, 0f);
            closeRect.pivot = new Vector2(0.5f, 0f);
            closeRect.anchoredPosition = new Vector2(0f, 24f);
            closeRect.sizeDelta = new Vector2(140f, 48f);

            BuildScrollList(panel);

            screenRoot.SetActive(false);
        }

        private void BuildScrollList(RectTransform panelRoot)
        {
            var scrollGO = new GameObject("Sections", typeof(RectTransform), typeof(ScrollRect));
            scrollGO.transform.SetParent(panelRoot, false);
            var scrollRect = (RectTransform)scrollGO.transform;
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector2(32f, 88f);
            scrollRect.offsetMax = new Vector2(-32f, -80f);

            var viewportGO = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewport = (RectTransform)viewportGO.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero;

            var contentGO = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGO.transform.SetParent(viewportGO.transform, false);
            var content = (RectTransform)contentGO.transform;
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);

            var layout = contentGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 20f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            contentGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sectionsContent = content;

            var scroll = scrollGO.GetComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            AddSection(content, LanguageManager.Keys.HowToPlayInspectTitle, LanguageManager.Keys.HowToPlayInspectBody);
            AddSection(content, LanguageManager.Keys.HowToPlayHaggleTitle, LanguageManager.Keys.HowToPlayHaggleBody);
            AddSection(content, LanguageManager.Keys.HowToPlayMoodTitle, LanguageManager.Keys.HowToPlayMoodBody);
            AddSection(content, LanguageManager.Keys.HowToPlayValueTitle, LanguageManager.Keys.HowToPlayValueBody);
        }

        private static void AddSection(Transform parent, string titleKey, string bodyKey)
        {
            var sectionGO = new GameObject(titleKey, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            sectionGO.transform.SetParent(parent, false);

            var layout = sectionGO.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            sectionGO.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var heading = HUDUIManager.CreateText(sectionGO.transform, "Heading", 22f, TextAlignmentOptions.Left, FontStyles.Bold, wrap: true);
            heading.color = UITheme.Gold;
            LocalizedLabel.Bind(heading, titleKey);

            var body = HUDUIManager.CreateText(sectionGO.transform, "Body", 19f, TextAlignmentOptions.Left, wrap: true);
            body.color = HUDUIManager.TextColor;
            LocalizedLabel.Bind(body, bodyKey);
        }
    }
}
