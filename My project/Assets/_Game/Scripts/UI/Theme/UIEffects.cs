using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Hover/press feedback for code-built buttons: scales toward 1.03 on hover and
    /// 0.96 while pressed (ease-out, ~120ms, unscaled time). Scale-only on purpose —
    /// screens freely rewrite button Image colors (disabled tints, refreshes), so
    /// touching color here would fight them. Layout groups ignore localScale, so
    /// this never causes reflow.
    /// </summary>
    public class ButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Button button;
        private Vector3 baseScale;
        private bool hovering;
        private bool pressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            baseScale = transform.localScale;

            // Every code-built button carries ButtonFX, so this one hook gives the
            // whole UI a click sound (routed through the SFX volume setting).
            if (button != null)
            {
                button.onClick.AddListener(() => Systems.Audio.AudioManager.Instance?.PlayClick());
            }
        }

        private void OnDisable()
        {
            hovering = pressed = false;
            transform.localScale = baseScale;
        }

        public void OnPointerEnter(PointerEventData eventData) => hovering = true;
        public void OnPointerExit(PointerEventData eventData) { hovering = false; pressed = false; }
        public void OnPointerDown(PointerEventData eventData) => pressed = true;
        public void OnPointerUp(PointerEventData eventData) => pressed = false;

        private void Update()
        {
            bool interactable = button == null || button.interactable;
            float target = !interactable ? 1f
                : pressed ? UITheme.PressScale
                : hovering ? UITheme.HoverScale
                : 1f;

            var desired = baseScale * target;
            transform.localScale = Vector3.Lerp(transform.localScale, desired,
                1f - Mathf.Exp(-Time.unscaledDeltaTime / (UITheme.HoverDuration * 0.35f)));
        }
    }

    /// <summary>Small motion helpers shared by the code-built screens.</summary>
    public static class UIFx
    {
        /// <summary>
        /// Fades a screen/panel in over FadeDuration (alpha only — no translation, so
        /// repeated toggles can never drift a layout). Host must be an active,
        /// persistent MonoBehaviour (the UI managers all are).
        /// </summary>
        public static void FadeIn(MonoBehaviour host, GameObject target, float from = 0f)
        {
            if (!target.activeInHierarchy || !host.isActiveAndEnabled) return;

            var group = target.GetComponent<CanvasGroup>();
            if (group == null) group = target.AddComponent<CanvasGroup>();
            host.StartCoroutine(FadeRoutine(group, from));
        }

        private static IEnumerator FadeRoutine(CanvasGroup group, float from)
        {
            float elapsed = 0f;
            group.alpha = from;
            while (elapsed < UITheme.FadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / UITheme.FadeDuration);
                group.alpha = Mathf.Lerp(from, 1f, 1f - (1f - t) * (1f - t)); // ease-out
                yield return null;
            }

            group.alpha = 1f;
        }

        /// <summary>Fade + slight scale-in after an unscaled delay — staggered row entrances. Null-safe against destroyed rows.</summary>
        public static Coroutine FadeInAfter(MonoBehaviour host, CanvasGroup group, float delay)
        {
            if (!host.isActiveAndEnabled || group == null) return null;
            return host.StartCoroutine(FadeInAfterRoutine(group, delay));
        }

        private static IEnumerator FadeInAfterRoutine(CanvasGroup group, float delay)
        {
            float waited = 0f;
            while (waited < delay)
            {
                if (group == null) yield break;
                group.alpha = 0f;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            float elapsed = 0f;
            while (elapsed < UITheme.FadeDuration)
            {
                if (group == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / UITheme.FadeDuration);
                group.alpha = 1f - (1f - t) * (1f - t);
                group.transform.localScale = Vector3.one * Mathf.Lerp(0.97f, 1f, t);
                yield return null;
            }

            group.alpha = 1f;
            group.transform.localScale = Vector3.one;
        }

        /// <summary>One check for "the player clicked to skip juice", on either input backend.</summary>
        public static bool SkipClickPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return UnityEngine.InputSystem.Mouse.current != null
                && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame;
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

        /// <summary>Floating +$/−$ label that drifts up and fades out — money made physical.</summary>
        public static void SpawnMoneyFloater(MonoBehaviour host, RectTransform parent, int amount, Vector2 anchoredPosition)
        {
            if (!host.isActiveAndEnabled || parent == null) return;

            var go = new GameObject("MoneyFloater", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(260f, 44f);

            var label = go.AddComponent<TextMeshProUGUI>();
            label.fontSize = 27f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.raycastTarget = false;
            label.text = (amount >= 0 ? "+$" : "-$") + Mathf.Abs(amount).ToString("N0");
            label.color = amount >= 0 ? UITheme.Success : UITheme.Danger;

            host.StartCoroutine(FloaterRoutine(go, rect, go.GetComponent<CanvasGroup>()));
        }

        private static IEnumerator FloaterRoutine(GameObject go, RectTransform rect, CanvasGroup group)
        {
            const float duration = 0.9f;
            float startY = rect.anchoredPosition.y;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (rect == null) yield break;
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float ease = 1f - (1f - t) * (1f - t);
                rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, startY + 70f * ease);
                group.alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
                yield return null;
            }

            Object.Destroy(go);
        }
    }
}
