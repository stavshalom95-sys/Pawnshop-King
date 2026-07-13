using System.Collections;
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
    }
}
