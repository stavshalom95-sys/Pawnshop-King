using System.Collections.Generic;
using PawnshopKing.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// The living shop behind the HUD (canvas below everything): shelf and window
    /// silhouettes at whisper contrast, a pendant lamp with a slow breathing glow,
    /// drifting dust motes, and a time-of-day tint that warms as the customer
    /// queue drains. The shelves stock themselves from the live inventory count,
    /// so buying fills the room and a seizure visibly empties it.
    /// All procedural, all motion continuous/eased on unscaled time — no jitter,
    /// no discrete jumps (the eye-strain lesson from the film grain).
    /// </summary>
    public class ShopSceneBackdrop : MonoBehaviour
    {
        // Contrast is tuned knowing the gameplay panel dims this by ~60%.
        private const float BoardContrast = 0.08f;
        private const float BlobContrast = 0.11f;
        private const float PaneContrast = 0.14f;

        private const int MoteCount = 7;
        private const float MoteAlpha = 0.045f;
        private const float GlowBaseAlpha = 0.055f;
        private const float GlowBreathePeriod = 7f;   // seconds per breath
        private const float DayTintDamping = 3f;      // seconds to settle toward the target tint

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
        private Image tintImage;
        private Image lampGlow;
        private readonly List<Mote> motes = new List<Mote>();
        private readonly List<GameObject> stockBlobs = new List<GameObject>();
        private int shownStock = -1;
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

            // Lamp breathing: ±15% alpha on a 7s sine — felt, never seen.
            float breathe = 1f + 0.15f * Mathf.Sin(2f * Mathf.PI * t / GlowBreathePeriod);
            var glowColor = lampGlow.color;
            glowColor.a = GlowBaseAlpha * breathe;
            lampGlow.color = glowColor;

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

            // Shelves mirror the live inventory count.
            int stock = gm.State != null ? Mathf.Min(gm.State.inventory.Count, stockBlobs.Count) : 0;
            if (stock != shownStock)
            {
                shownStock = stock;
                for (int i = 0; i < stockBlobs.Count; i++) stockBlobs[i].SetActive(i < stock);
            }
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
            Color board = Tinted(BoardContrast);
            Color blob = Tinted(BlobContrast);

            // Window, upper right: lighter pane, dark mullions, a soft shaft of light.
            CreateImage(root, "WindowPane", new Vector2(700f, 380f), new Vector2(260f, 180f), Tinted(PaneContrast), UITheme.RoundedSprite);
            Color frame = Color.Lerp(UITheme.Background, Color.black, 0.35f);
            CreateImage(root, "MullionV", new Vector2(700f, 380f), new Vector2(5f, 180f), frame);
            CreateImage(root, "MullionH", new Vector2(700f, 380f), new Vector2(260f, 5f), frame);
            var shaft = CreateImage(root, "LightShaft", new Vector2(520f, 220f), new Vector2(460f, 180f),
                new Color(1f, 1f, 1f, 0.035f), BuildHorizontalGradientSprite(), rotation: 152f);
            shaft.type = Image.Type.Simple;

            // Pendant lamp over the counter: cord, shade, breathing glow.
            CreateImage(root, "LampCord", new Vector2(0f, 438f), new Vector2(3f, 80f), frame);
            CreateImage(root, "LampShade", new Vector2(0f, 388f), new Vector2(92f, 30f), frame, UITheme.RoundedSprite);
            lampGlow = CreateImage(root, "LampGlow", new Vector2(0f, 330f), new Vector2(360f, 250f),
                new Color(UITheme.Gold.r, UITheme.Gold.g, UITheme.Gold.b, GlowBaseAlpha), UITheme.SoftShadowSprite);

            // Shelves flanking the counter, three boards a side.
            BuildShelfUnit(root, -700f, new[] { 280f, 120f, -40f }, board, blob);
            BuildShelfUnit(root, 700f, new[] { 100f, -60f, -220f }, board, blob);

            // Dust motes: soft blobs, individually slow.
            for (int i = 0; i < MoteCount; i++)
            {
                var mote = CreateImage(root, "Mote" + i,
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

            // Day tint on top of the scene (still under every gameplay canvas).
            tintImage = CreateImage(root, "DayTint", Vector2.zero, Vector2.zero, MorningTint);
            var tintRect = (RectTransform)tintImage.transform;
            tintRect.anchorMin = Vector2.zero;
            tintRect.anchorMax = Vector2.one;
            tintRect.offsetMin = tintRect.offsetMax = Vector2.zero;
        }

        /// <summary>Three boards with four stock slots each; slots fill bottom-up as inventory grows.</summary>
        private void BuildShelfUnit(Transform root, float centerX, float[] boardHeights, Color board, Color blob)
        {
            // Deterministic silhouette variety: box, bottle, squat item, book stack.
            Vector2[] variants = { new Vector2(34f, 24f), new Vector2(14f, 38f), new Vector2(24f, 22f), new Vector2(40f, 16f) };

            for (int b = boardHeights.Length - 1; b >= 0; b--) // bottom board first, so stock fills upward
            {
                float y = boardHeights[b];
                CreateImage(root, "Shelf", new Vector2(centerX, y), new Vector2(330f, 9f), board, UITheme.RoundedSprite);

                for (int s = 0; s < 4; s++)
                {
                    var size = variants[(b + s) % variants.Length];
                    float x = centerX - 120f + s * 80f;
                    var item = CreateImage(root, "Stock", new Vector2(x, y + 5f + size.y * 0.5f), size, blob, UITheme.RoundedSprite);
                    item.gameObject.SetActive(false);
                    stockBlobs.Add(item.gameObject);
                }
            }
        }

        private static Color Tinted(float toward) => Color.Lerp(UITheme.Background, Color.white, toward);

        private static Image CreateImage(Transform parent, string name, Vector2 center, Vector2 size,
            Color color, Sprite sprite = null, float rotation = 0f)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            if (rotation != 0f) rect.localRotation = Quaternion.Euler(0f, 0f, rotation);

            var image = go.GetComponent<Image>();
            image.color = color;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.type = Image.Type.Sliced;
            }

            image.raycastTarget = false;
            return image;
        }

        /// <summary>Alpha falls off left-to-right with an ease — rotated, it becomes the window's light shaft.</summary>
        private static Sprite BuildHorizontalGradientSprite()
        {
            const int width = 128;
            var texture = new Texture2D(width, 8, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color[width * 8];
            for (int x = 0; x < width; x++)
            {
                float t = x / (float)(width - 1);
                float alpha = (1f - t) * (1f - t);
                for (int y = 0; y < 8; y++) pixels[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }

            texture.SetPixels(pixels);
            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0, 0, width, 8),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
