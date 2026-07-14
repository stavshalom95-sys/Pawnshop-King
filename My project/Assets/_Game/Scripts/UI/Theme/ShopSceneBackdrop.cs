using System.Collections.Generic;
using PawnshopKing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// The living shop behind the HUD (canvas below everything): four room photos
    /// (Empty/Sparse/Stocked/Full) that cross-fade based on the live inventory
    /// count, a subtle readability scrim, drifting dust motes, and a time-of-day
    /// tint that warms as the customer queue drains. Each photo displays with a
    /// true "cover" fit (crop, never distort) via RawImage UV cropping, so it
    /// fills any aspect ratio without stretching.
    /// All motion is continuous/eased on unscaled time — no jitter, no discrete
    /// jumps (the eye-strain lesson from the film grain pass).
    /// </summary>
    public class ShopSceneBackdrop : MonoBehaviour
    {
        /// <summary>Per-stock-level room photos: Resources/Backgrounds/Shop_Empty(.*), _Sparse, _Stocked, _Full.</summary>
        public const string BackgroundResourcePathPrefix = "Backgrounds/Shop_";

        /// <summary>Single-image fallback (the pre-variant background) for any level whose own file is missing.</summary>
        public const string BackgroundResourceFallbackPath = "Backgrounds/Shop";

        // The room art itself is already dark/moody (near-black ceiling, navy
        // walls), so it needs less help than a generic photo would — a lighter
        // scrim lets the shelf and lamp detail actually read instead of washing
        // it flat.
        private const float ScrimAlpha = 0.12f;
        private const int MoteCount = 7;
        private const float MoteAlpha = 0.045f;
        private const float DayTintDamping = 3f; // seconds to settle toward the target tint

        // Bucket edges, in items owned. Empty is always exactly zero.
        private const int StockedAtCount = 4;
        private const int FullAtCount = 10;

        // Heat warning: inert below 70% of HeatEventSystem's threshold, then
        // ramps in and gently pulses as danger actually approaches — a read on
        // the shop's own lighting, not just the HUD number.
        private const float HeatWarningRampStart = 0.7f;
        private const float MaxHeatWarningAlpha = 0.16f;
        private const float HeatWarningPulsePeriod = 4f;

        private const float CrossfadeDuration = 2f; // "very slow and smooth" per spec

        private static readonly Color MorningTint = new Color(0.55f, 0.65f, 0.90f, 0.045f);
        private static readonly Color EveningTint = new Color(0.95f, 0.60f, 0.30f, 0.060f);

        private enum StockLevel { Empty, Sparse, Stocked, Full }

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

        // Double-buffered background: bgCurrent is always fully opaque and shows
        // the last committed level; bgIncoming fades 0->1 on top of it during a
        // transition, then the roles conceptually swap (bgCurrent's texture is
        // replaced and bgIncoming resets to 0, ready for the next crossfade).
        private RawImage bgCurrent;
        private RawImage bgIncoming;
        private readonly Sprite[] stockSprites = new Sprite[4];
        private StockLevel currentLevel = StockLevel.Empty;
        private StockLevel? transitioningTo;
        private float transitionElapsed;
        private int lastScreenWidth = -1, lastScreenHeight = -1;

        private Image tintImage;
        private Image heatWarningImage;
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

            LoadStockSprites();
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

            RecropOnResize();
            UpdateStockCrossfade(dt);

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

            UpdateHeatWarning(t);
        }

        /// <summary>A slow red wash that only appears once heat is genuinely climbing toward trouble.</summary>
        private void UpdateHeatWarning(float t)
        {
            int heat = gm.State != null ? gm.State.heat : 0;
            float threshold = Systems.Events.HeatEventSystem.HeatThreshold;
            float rampStart = threshold * HeatWarningRampStart;

            float fraction = threshold > rampStart
                ? Mathf.Clamp01((heat - rampStart) / (threshold - rampStart))
                : 0f;

            float pulse = fraction > 0f ? 1f + 0.2f * Mathf.Sin(2f * Mathf.PI * t / HeatWarningPulsePeriod) : 1f;
            float alpha = fraction * MaxHeatWarningAlpha * pulse;
            heatWarningImage.color = new Color(UITheme.Danger.r, UITheme.Danger.g, UITheme.Danger.b, alpha);
        }

        // ---- Inventory-driven crossfade ---------------------------------------

        /// <summary>Re-crops both layers only when the window actually changes size — a cheap int compare.</summary>
        private void RecropOnResize()
        {
            if (Screen.width == lastScreenWidth && Screen.height == lastScreenHeight) return;

            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            float screenAspect = Screen.width / (float)Screen.height;

            if (bgCurrent.texture != null)
            {
                bgCurrent.uvRect = CoverUvRect(screenAspect, bgCurrent.texture.width / (float)bgCurrent.texture.height);
            }

            if (bgIncoming.texture != null)
            {
                bgIncoming.uvRect = CoverUvRect(screenAspect, bgIncoming.texture.width / (float)bgIncoming.texture.height);
            }
        }

        private void UpdateStockCrossfade(float dt)
        {
            // No campaign yet (main menu, before New Game/Continue): stay Empty.
            var desired = gm.State != null ? LevelFor(gm.State.inventory.Count) : StockLevel.Empty;

            if (desired != currentLevel && transitioningTo != desired)
            {
                // Starts a fresh 2s fade FROM the last committed image TO the
                // newest target. If the target changes again mid-fade (a buying
                // spree crossing two buckets in a few seconds), this restarts
                // cleanly rather than fighting a second overlapping transition.
                transitioningTo = desired;
                transitionElapsed = 0f;
                SetIncomingTexture(desired);
            }

            if (transitioningTo == null) return;

            transitionElapsed += dt;
            float t = Mathf.Clamp01(transitionElapsed / CrossfadeDuration);
            float eased = t * t * (3f - 2f * t); // smoothstep: gentle in, gentle out

            var color = bgIncoming.color;
            color.a = eased;
            bgIncoming.color = color;

            if (t < 1f) return;

            // Commit: the incoming image becomes the new baseline.
            bgCurrent.texture = bgIncoming.texture;
            bgCurrent.uvRect = bgIncoming.uvRect;
            var idle = bgIncoming.color;
            idle.a = 0f;
            bgIncoming.color = idle;

            currentLevel = transitioningTo.Value;
            transitioningTo = null;
        }

        private static StockLevel LevelFor(int count)
        {
            if (count <= 0) return StockLevel.Empty;
            if (count < StockedAtCount) return StockLevel.Sparse;
            if (count < FullAtCount) return StockLevel.Stocked;
            return StockLevel.Full;
        }

        // ---- Construction ------------------------------------------------------

        private void LoadStockSprites()
        {
            string[] suffixes = { "Empty", "Sparse", "Stocked", "Full" };
            var fallback = Resources.Load<Sprite>(BackgroundResourceFallbackPath);
            bool anyFound = false;

            for (int i = 0; i < suffixes.Length; i++)
            {
                var sprite = Resources.Load<Sprite>(BackgroundResourcePathPrefix + suffixes[i]);
                if (sprite == null) sprite = fallback; // graceful degrade: missing variants borrow the base image
                stockSprites[i] = sprite;
                anyFound |= sprite != null;
            }

            if (!anyFound)
            {
                Debug.LogWarning($"[ShopSceneBackdrop] No background sprites found under Resources/{BackgroundResourcePathPrefix}* " +
                    $"or {BackgroundResourceFallbackPath} — showing a flat fallback color instead of the room.");
            }
        }

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

            BuildBackgroundLayers(root);
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
            heatWarningImage = CreateFullscreen(root, "HeatWarning", new Color(UITheme.Danger.r, UITheme.Danger.g, UITheme.Danger.b, 0f));
        }

        /// <summary>Two full-bleed RawImage layers for the crossfade; bgCurrent starts on Empty, fully opaque.</summary>
        private void BuildBackgroundLayers(Transform root)
        {
            bgCurrent = CreateBackgroundLayer(root, "RoomPhotoCurrent");
            bgIncoming = CreateBackgroundLayer(root, "RoomPhotoIncoming");

            var incomingColor = bgIncoming.color;
            incomingColor.a = 0f;
            bgIncoming.color = incomingColor;

            ApplyTexture(bgCurrent, stockSprites[(int)StockLevel.Empty]);
        }

        private static RawImage CreateBackgroundLayer(Transform root, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(root, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            return image;
        }

        private void SetIncomingTexture(StockLevel level)
        {
            ApplyTexture(bgIncoming, stockSprites[(int)level]);
            var color = bgIncoming.color;
            color.a = 0f;
            bgIncoming.color = color;
        }

        private void ApplyTexture(RawImage image, Sprite sprite)
        {
            if (sprite != null)
            {
                image.texture = sprite.texture;
                image.uvRect = CoverUvRect(Screen.width / (float)Screen.height,
                    sprite.texture.width / (float)sprite.texture.height);
            }
            else
            {
                image.texture = null;
                image.color = new Color(UITheme.Background.r, UITheme.Background.g, UITheme.Background.b, image.color.a);
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
