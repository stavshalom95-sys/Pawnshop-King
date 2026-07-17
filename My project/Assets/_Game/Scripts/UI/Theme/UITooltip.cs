using PawnshopKing.Systems.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// One shared floating tooltip bubble, shown/hidden by whichever
    /// UITooltipTrigger is currently hovered. A singleton rather than a
    /// per-element bubble — simpler, and only one tooltip can ever be visible
    /// at a time anyway.
    /// </summary>
    public class UITooltipManager : MonoBehaviour
    {
        public static UITooltipManager Instance { get; private set; }

        private const float MaxWidth = 340f;
        private const float VerticalOffset = 14f;

        private RectTransform bubbleRect;
        private TextMeshProUGUI label;
        private CanvasGroup group;

        private void Awake()
        {
            Instance = this;
            BuildBubble();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Shows localized tooltip text anchored below the given screen-space element.</summary>
        public void Show(string text, RectTransform anchor)
        {
            if (string.IsNullOrEmpty(text)) return;

            label.alignment = LanguageManager.IsRtl ? TextAlignmentOptions.TopRight : TextAlignmentOptions.TopLeft;
            Loc.Set(label, text);

            var canvasRect = (RectTransform)bubbleRect.parent;
            Vector3 anchorWorldBottom = anchor.TransformPoint(new Vector3(
                anchor.rect.center.x, anchor.rect.yMin, 0f));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, RectTransformUtility.WorldToScreenPoint(null, anchorWorldBottom),
                null, out Vector2 localPoint);

            bubbleRect.anchoredPosition = localPoint + new Vector2(0f, -VerticalOffset);
            bubbleRect.pivot = new Vector2(0.5f, 1f);

            // Keep the bubble on-screen horizontally even near the canvas edge.
            float halfWidth = MaxWidth * 0.5f;
            float canvasHalfWidth = canvasRect.rect.width * 0.5f;
            bubbleRect.anchoredPosition = new Vector2(
                Mathf.Clamp(bubbleRect.anchoredPosition.x, -canvasHalfWidth + halfWidth, canvasHalfWidth - halfWidth),
                bubbleRect.anchoredPosition.y);

            group.alpha = 1f;
        }

        public void Hide()
        {
            group.alpha = 0f;
        }

        private void BuildBubble()
        {
            var canvasGO = new GameObject("TooltipCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90; // above gameplay UI, below the pause menu

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var bubbleGO = new GameObject("TooltipBubble", typeof(RectTransform), typeof(Image),
                typeof(CanvasGroup), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            bubbleGO.transform.SetParent(canvasGO.transform, false);
            bubbleRect = (RectTransform)bubbleGO.transform;
            bubbleRect.anchorMin = bubbleRect.anchorMax = new Vector2(0.5f, 0.5f);
            bubbleRect.sizeDelta = new Vector2(MaxWidth, 0f);

            var image = bubbleGO.GetComponent<Image>();
            image.color = UITheme.SurfaceRaised;
            image.sprite = UITheme.RoundedSprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;

            group = bubbleGO.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false; // never steal the hover it's reacting to

            var layout = bubbleGO.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 12, 12);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = bubbleGO.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            label = bubbleGO.AddComponent<TextMeshProUGUI>();
            label.fontSize = 19f;
            label.color = UITheme.TextPrimary;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }
    }

    /// <summary>Attach to any UI Graphic to show a localized tooltip on hover.</summary>
    public class UITooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private string localizationKey;

        public static void Attach(GameObject target, string localizationKey)
        {
            var trigger = target.AddComponent<UITooltipTrigger>();
            trigger.localizationKey = localizationKey;
        }

        public void OnPointerEnter(PointerEventData eventData) =>
            UITooltipManager.Instance?.Show(Loc.T(localizationKey), (RectTransform)transform);

        public void OnPointerExit(PointerEventData eventData) =>
            UITooltipManager.Instance?.Hide();

        private void OnDisable() => UITooltipManager.Instance?.Hide();
    }
}
