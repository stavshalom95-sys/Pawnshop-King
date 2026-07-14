using System.Collections.Generic;
using PawnshopKing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// The living shop behind the HUD (canvas below everything): the authored room
    /// photo (Resources/Backgrounds), a subtle readability scrim, drifting dust
    /// motes, and a time-of-day tint that warms as the customer queue drains.
    /// The photo is displayed with a true "cover" fit (crop, never distort) via
    /// RawImage UV cropping, so it fills any aspect ratio without stretching.
    /// All motion is continuous/eased on unscaled time — no jitter, no discrete
    /// jumps (the eye-strain lesson from the film grain pass).
    /// </summary>
    public class ShopSceneBackdrop : MonoBehaviour
    {
        /// <summary>Drop a room photo here (Resources/Backgrounds/Shop.*) to change the scene.</summary>
        public const string BackgroundResourcePath = "Backgrounds/Shop";

        private const float ScrimAlpha = 0.20f;
        private const int MoteCount = 7;
        private const float MoteAlpha = 0.045f;
        private const float DayTintDamping = 3f; // seconds to settle toward the target tint

        private static readonly Color MorningTint = new Color(0.55f, 0.65f, 0.90f, 0.045f);
        private static readonly Color EveningTint = new Color(0.95f, 0.60f, 0.30f, 0.060f);

        private struct Mote
        {
            public RectTransform rect;
            public float speed;     // px/sec upward — extremely slow
            public float swayAmp;
            public float swayRate;
            public float phase;
            public float baseX;
        }

        private GameManager gm;
        private RawImage background;
        private float backgroundAspect = 16f / 9f;
        private int lastScreenWidth = -1, lastScreenHeight = -1;
        private Image tintImage;
        private readonly List<Mote> motes = new List<Mote>();
        private int totalCustomersToday = 1;
        private float dayProgressTarget;
        private float dayProgress;

        private void Awake()
        {
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("ShopSceneBackdrop requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScene();

            gm.Day.DayStarted += OnDayStarted;
            gm.Day.CustomerArrived += OnCustomerArrived;
        }

        private void OnDestroy()
        {
            if (gm == null || gm.Day == null) return;
            gm.Day.DayStarted -= OnDayStarted;
            gm.Day.CustomerArrived -= OnCustomerArrived;
        }

        private void OnDayStarted(int day)
        {
            totalCustomersToday = Mathf.Max(1, gm.Day.CustomersRemaining);
            dayProgressTarget = 0f;
        }

        private void OnCustomerArrived(Data.Runtime.CustomerInstance customer)
        {
            dayProgressTarget = 1f - gm.Day.CustomersRemaining / (float)totalCustomersToday;
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // Re-crop the photo only when the window actually changes size — a
            // cheap int compare, not a per-frame recompute.
            if (background.texture != null && (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight))
            {
                lastScreenWidth = Screen.width;
                lastScreenHeight = Screen.height;
                background.uvRect = CoverUvRect(Screen.width / (float)Screen.height, backgroundAspect);
            }

            // Dust drifts up with a slow sway; wraps below the bottom edge.
            for (int i = 0; i < motes.Count; i++)
            {
                var mote = motes[i];
                float y = mote.rect.anchoredPosition.y + mote.speed * dt;
                if (y > 560f)
                {
                    y = -560f;
                    mote.baseX = Random.Range(-900f, 900f);
                    motes[i] = mote;
                }

                float x = mote.baseX + Mathf.Sin(t * mote.swayRate + mote.phase) * mote.swayAmp;
                mote.rect.anchoredPosition = new Vector2(x, y);
            }

            // Time-of-day tint eases toward the queue's progress — no steps.
            dayProgress = Mathf.MoveTowards(dayProgress, dayProgressTarget, dt / DayTintDamping);
            tintImage.color = Color.Lerp(MorningTint, EveningTint, dayProgress);
        }

        // ---- Construction ------------------------------------------------------

        private void BuildScene()
        {
            var canvasGO = new GameObject("ShopBackdropCanvas", typeof(Canvas), typeof(CanvasScaler));
            canvasGO.transform.SetParent(transform, false);
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -10; // behind every gameplay canvas

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var root = canvasGO.transform;

            BuildBackground(root);
            CreateFullscreen(root, "ReadabilityScrim", new Color(0f, 0f, 0f, ScrimAlpha));

            for (int i = 0; i < MoteCount; i++)
            {
                var mote = CreateSprite(root, "Mote" + i,
                    new Vector2(Random.Range(-900f, 900f), Random.Range(-540f, 540f)),
                    new Vector2(Random.Range(10f, 18f), Random.Range(10f, 18f)),
                    new Color(1f, 1f, 1f, MoteAlpha), UITheme.SoftShadowSprite);
                motes.Add(new Mote
                {
                    rect = (RectTransform)mote.transform,
                    speed = Random.Range(6f, 12f),
                    swayAmp = Random.Range(12f, 26f),
                    swayRate = 2f * Mathf.PI / Random.Range(8f, 13f),
                    phase = Random.Range(0f, 2f * Mathf.PI),
                    baseX = 0f,
                });
                var m = motes[motes.Count - 1];
                m.baseX = m.rect.anchoredPosition.x;
                motes[motes.Count - 1] = m;
            }

            tintImage = CreateFullscreen(root, "DayTint", MorningTint);
        }

        /// <summary>The authored room photo, full-bleed with a true cover fit (crop, never stretch).</summary>
        private void BuildBackground(Transform root)
        {
            var go = new GameObject("RoomPhoto", typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            background = go.GetComponent<RawImage>();
            background.raycastTarget = false;

            var sprite = Resources.Load<Sprite>(BackgroundResourcePath);
            if (sprite != null)
            {
                background.texture = sprite.texture;
                backgroundAspect = sprite.texture.width / (float)sprite.texture.height;
            }
            else
            {
                Debug.LogWarning($"[ShopSceneBackdrop] No sprite at Resources/{BackgroundResourcePath} — showing a flat fallback color instead of the room.");
                background.color = UITheme.Background;
            }
        }

        /// <summary>CSS "background-size: cover" via UV cropping: fills the frame, crops the excess, never distorts.</summary>
        private static Rect CoverUvRect(float screenAspect, float textureAspect)
        {
            if (textureAspect > screenAspect)
            {
                float width = screenAspect / textureAspect;
                return new Rect((1f - width) * 0.5f, 0f, width, 1f);
            }

            float height = textureAspect / screenAspect;
            return new Rect(0f, (1f - height) * 0.5f, 1f, height);
        }

        private static Image CreateFullscreen(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSprite(Transform parent, string name, Vector2 center, Vector2 size, Color color, Sprite sprite)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;

            var image = go.GetComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.raycastTarget = false;
            return image;
        }
    }
}
