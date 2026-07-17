using PawnshopKing.Core;
using PawnshopKing.Data;
using PawnshopKing.Systems.Audio;
using PawnshopKing.Systems.DifficultyTier;
using PawnshopKing.Systems.Localization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PawnshopKing.UI
{
    /// <summary>
    /// Esc-triggered pause overlay (built entirely from code): Resume, Settings
    /// (Master/SFX/Music sliders backed by GameAudioSettings), and Quit to Main
    /// Menu. Opening freezes Time.timeScale; closing restores it. All menu motion
    /// runs on unscaled time, so the UI stays alive while the game is frozen.
    /// A deep dim stands in for a blur — a real blur would need a shader asset,
    /// which the zero-editor-wiring workflow avoids.
    /// </summary>
    public class PauseMenuUIManager : MonoBehaviour
    {
        public static PauseMenuUIManager Instance { get; private set; }

        private GameManager gm;
        private GameObject screenRoot;
        private GameObject mainView;
        private GameObject settingsView;
        private RectTransform mainContent;
        private RectTransform settingsContent;
        private TextMeshProUGUI musicToggleLabel;
        private TextMeshProUGUI difficultyLabel;

        public bool IsOpen => screenRoot != null && screenRoot.activeSelf;

        private void Awake()
        {
            Instance = this;
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("PauseMenuUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            GameAudioSettings.Apply();
            BuildScreen();
            LanguageManager.LanguageChanged += RefreshDynamicSettings;
        }

        private void OnDestroy()
        {
            LanguageManager.LanguageChanged -= RefreshDynamicSettings;
            if (Instance == this) Instance = null;
            if (IsOpen) Time.timeScale = 1f; // never leave the game frozen behind us
        }

        /// <summary>Settings whose button text depends on state (not just language): music on/off, difficulty.</summary>
        private void RefreshDynamicSettings()
        {
            Loc.Set(musicToggleLabel, Loc.T(GameAudioSettings.MusicEnabled
                ? LanguageManager.Keys.On
                : LanguageManager.Keys.Off));

            var difficulty = gm.State != null ? gm.State.difficulty : DifficultyTuning.Current;
            Loc.Set(difficultyLabel, Loc.T(difficulty == Difficulty.Easy
                ? LanguageManager.Keys.DifficultyEasy
                : LanguageManager.Keys.DifficultyHard));

            // Localized labels just rewrote their text (this runs after them —
            // subscribed later); relayout both views now so nothing clips or
            // drifts while the menu is open.
            if (screenRoot != null && screenRoot.activeSelf)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(mainContent);
                LayoutRebuilder.ForceRebuildLayoutImmediate(settingsContent);
            }
        }

        private void Update()
        {
            if (!EscapePressed()) return;
            if (MainMenuUIManager.Instance != null && MainMenuUIManager.Instance.IsVisible) return;

            if (!IsOpen) Open();
            else if (settingsView.activeSelf) ShowMainView(); // Esc backs out of Settings first
            else Close();
        }

        private static bool EscapePressed()
        {
#if ENABLE_INPUT_SYSTEM
            return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public void Open()
        {
            Time.timeScale = 0f;
            ShowMainView();
            RefreshDynamicSettings();
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        public void Close()
        {
            screenRoot.SetActive(false);
            Time.timeScale = 1f;
        }

        private void ShowMainView()
        {
            mainView.SetActive(true);
            settingsView.SetActive(false);
        }

        private void ShowSettingsView()
        {
            mainView.SetActive(false);
            settingsView.SetActive(true);
        }

        private void OnQuitToMenuClicked()
        {
            Close(); // restores timeScale before anything else runs
            MainMenuUIManager.Instance?.Show();
        }

        private void OnHowToPlayClicked() => HowToPlayUIManager.Instance?.Open();

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("PauseCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50; // above every gameplay screen, below nothing

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Deep dim: heavier than the day summary so "paused" reads instantly.
            var dim = HUDUIManager.CreatePanel(canvasGO.transform, "PauseScreen", new Color(0f, 0f, 0f, 0.85f));
            dim.anchorMin = Vector2.zero;
            dim.anchorMax = Vector2.one;
            dim.offsetMin = dim.offsetMax = Vector2.zero;
            screenRoot = dim.gameObject;

            mainView = BuildView(dim, "MainView", new Vector2(520f, 480f), out mainContent);
            BuildMainView(mainContent);

            settingsView = BuildView(dim, "SettingsView", new Vector2(620f, 620f), out settingsContent);
            BuildSettingsView(settingsContent);

            screenRoot.SetActive(false);
        }

        /// <summary>A centered noir panel (with its shadow contained) inside a toggleable wrapper.</summary>
        private static GameObject BuildView(RectTransform parent, string name, Vector2 size, out RectTransform content)
        {
            var wrapper = new GameObject(name, typeof(RectTransform));
            wrapper.transform.SetParent(parent, false);
            var wrapperRect = (RectTransform)wrapper.transform;
            wrapperRect.anchorMin = wrapperRect.anchorMax = new Vector2(0.5f, 0.5f);
            wrapperRect.pivot = new Vector2(0.5f, 0.5f);
            wrapperRect.sizeDelta = size;

            var panel = HUDUIManager.CreatePanel(wrapperRect, name + "Panel", HUDUIManager.PanelColor, rounded: true);
            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.one;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
            HUDUIManager.AddPanelShadow(panel); // sibling inside the wrapper, so it toggles with the view

            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 32, 32);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            // Control heights so LayoutElement.preferredHeight actually drives the
            // stack — with this false, rows position by raw rect sizes and drift
            // off-panel when localized text dirties them.
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            content = panel;
            return wrapper;
        }

        private void BuildMainView(RectTransform content)
        {
            var title = HUDUIManager.CreateText(content, "Title", 40f, TextAlignmentOptions.Center, FontStyles.Bold, header: true);
            title.color = UITheme.NeonCyan;
            SetRowHeight(title.rectTransform, 56f);
            LocalizedLabel.Bind(title, LanguageManager.Keys.Paused);

            AddSpacer(content, 12f);
            CreateMenuButton(content, LanguageManager.Keys.Resume, Close);
            CreateMenuButton(content, LanguageManager.Keys.Settings, ShowSettingsView);
            CreateMenuButton(content, LanguageManager.Keys.HowToPlayTitle, OnHowToPlayClicked);
            CreateMenuButton(content, LanguageManager.Keys.QuitToMenu, OnQuitToMenuClicked);

            var note = HUDUIManager.CreateText(content, "Note", 16f, TextAlignmentOptions.Center, FontStyles.Italic);
            note.color = HUDUIManager.MutedColor;
            SetRowHeight(note.rectTransform, 40f);
            LocalizedLabel.Bind(note, LanguageManager.Keys.PauseNote);
        }

        private void BuildSettingsView(RectTransform content)
        {
            var title = HUDUIManager.CreateText(content, "Title", 34f, TextAlignmentOptions.Center, FontStyles.Bold, header: true);
            title.color = UITheme.NeonCyan;
            SetRowHeight(title.rectTransform, 48f);
            LocalizedLabel.Bind(title, LanguageManager.Keys.Settings);

            AddSpacer(content, 10f);
            CreateVolumeRow(content, LanguageManager.Keys.Master, GameAudioSettings.Master, v => GameAudioSettings.Master = v);
            CreateVolumeRow(content, LanguageManager.Keys.Sfx, GameAudioSettings.Sfx, v => GameAudioSettings.Sfx = v);
            CreateVolumeRow(content, LanguageManager.Keys.Music, GameAudioSettings.Music, v => GameAudioSettings.Music = v);

            AddSpacer(content, 10f);
            musicToggleLabel = CreateSettingRow(content, LanguageManager.Keys.Music, OnMusicToggleClicked);
            difficultyLabel = CreateSettingRow(content, LanguageManager.Keys.DifficultyLabel, OnDifficultyClicked);

            var languageButton = CreateSettingRow(content, LanguageManager.Keys.Language, LanguageManager.Toggle);
            LocalizedLabel.Bind(languageButton, LanguageManager.Keys.LanguageName);

            AddSpacer(content, 14f);
            CreateMenuButton(content, LanguageManager.Keys.Back, ShowMainView);
        }

        private void OnMusicToggleClicked()
        {
            GameAudioSettings.MusicEnabled = !GameAudioSettings.MusicEnabled;
            RefreshDynamicSettings();
        }

        private void OnDifficultyClicked()
        {
            var current = gm.State != null ? gm.State.difficulty : DifficultyTuning.Current;
            gm.SetDifficulty(current == Difficulty.Easy ? Difficulty.Hard : Difficulty.Easy);
            RefreshDynamicSettings();
        }

        /// <summary>A settings row: bound label on the left, a state-showing toggle button on the right.</summary>
        private static TextMeshProUGUI CreateSettingRow(Transform parent, string labelKey, UnityAction onClick)
        {
            var rowGO = new GameObject(labelKey + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(parent, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 44f;

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var labelText = HUDUIManager.CreateText(rowGO.transform, "Label", 21f, TextAlignmentOptions.Left);
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 150f;
            LocalizedLabel.Bind(labelText, labelKey);

            var spacerGO = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacerGO.transform.SetParent(rowGO.transform, false);
            spacerGO.GetComponent<LayoutElement>().flexibleWidth = 1f;

            var buttonLabel = HUDUIManager.CreateSmallButton(rowGO.transform, labelKey + "Toggle", 200f, onClick);
            buttonLabel.transform.parent.GetComponent<LayoutElement>().preferredHeight = 40f;
            return buttonLabel;
        }

        private static void CreateMenuButton(Transform parent, string key, UnityAction onClick)
        {
            var text = HUDUIManager.CreateSmallButton(parent, LanguageManager.T(key), 360f, onClick);
            var layoutElement = text.transform.parent.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 58f;
            text.fontSize = 23f;
            LocalizedLabel.Bind(text, key);
        }

        private void CreateVolumeRow(Transform parent, string labelKey, float initial, UnityAction<float> onChanged)
        {
            var rowGO = new GameObject(labelKey + "Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            rowGO.transform.SetParent(parent, false);
            rowGO.AddComponent<LayoutElement>().preferredHeight = 36f;

            var layout = rowGO.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 14f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var labelText = HUDUIManager.CreateText(rowGO.transform, "Label", 21f, TextAlignmentOptions.Left);
            labelText.gameObject.AddComponent<LayoutElement>().preferredWidth = 120f;
            LocalizedLabel.Bind(labelText, labelKey);

            var valueText = HUDUIManager.CreateText(rowGO.transform, "Value", 20f, TextAlignmentOptions.Right);
            valueText.text = $"{Mathf.RoundToInt(initial * 100)}%";
            valueText.color = UITheme.Gold;

            BuildSlider(rowGO.transform, initial, v =>
            {
                onChanged(v);
                valueText.text = $"{Mathf.RoundToInt(v * 100)}%";
            });

            // Slider between label and value: reorder (built last for the closure).
            rowGO.transform.GetChild(2).SetSiblingIndex(1);
            valueText.gameObject.AddComponent<LayoutElement>().preferredWidth = 64f;
        }

        private static void BuildSlider(Transform parent, float initial, UnityAction<float> onChanged)
        {
            var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGO.transform.SetParent(parent, false);
            var sliderLayout = sliderGO.AddComponent<LayoutElement>();
            sliderLayout.flexibleWidth = 1f;
            sliderLayout.preferredHeight = 28f;

            var bgGO = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGO.transform.SetParent(sliderGO.transform, false);
            var bgRect = (RectTransform)bgGO.transform;
            bgRect.anchorMin = new Vector2(0f, 0.5f);
            bgRect.anchorMax = new Vector2(1f, 0.5f);
            bgRect.sizeDelta = new Vector2(0f, 10f);
            var bg = bgGO.GetComponent<Image>();
            bg.color = UITheme.SurfaceRaised;
            bg.sprite = UITheme.RoundedSprite;
            bg.type = Image.Type.Sliced;

            var areaGO = new GameObject("Fill Area", typeof(RectTransform));
            areaGO.transform.SetParent(sliderGO.transform, false);
            var areaRect = (RectTransform)areaGO.transform;
            areaRect.anchorMin = new Vector2(0f, 0.5f);
            areaRect.anchorMax = new Vector2(1f, 0.5f);
            areaRect.sizeDelta = new Vector2(-22f, 10f);

            var fillGO = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGO.transform.SetParent(areaGO.transform, false);
            var fillRect = (RectTransform)fillGO.transform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.sizeDelta = new Vector2(11f, 0f);
            var fill = fillGO.GetComponent<Image>();
            fill.color = new Color(UITheme.NeonCyan.r, UITheme.NeonCyan.g, UITheme.NeonCyan.b, 0.55f);
            fill.sprite = UITheme.RoundedSprite;
            fill.type = Image.Type.Sliced;

            var handleAreaGO = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleAreaGO.transform.SetParent(sliderGO.transform, false);
            var handleAreaRect = (RectTransform)handleAreaGO.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-22f, 0f);

            var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(Image));
            handleGO.transform.SetParent(handleAreaGO.transform, false);
            var handleRect = (RectTransform)handleGO.transform;
            handleRect.sizeDelta = new Vector2(22f, 22f);
            var handle = handleGO.GetComponent<Image>();
            handle.color = UITheme.NeonCyan;
            handle.sprite = UITheme.RoundedSprite;
            handle.type = Image.Type.Sliced;

            var slider = sliderGO.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = initial;
            slider.onValueChanged.AddListener(onChanged);
        }

        private static void SetRowHeight(RectTransform rect, float height)
        {
            rect.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
        }

        private static void AddSpacer(Transform parent, float height)
        {
            var spacer = new GameObject("Spacer", typeof(RectTransform), typeof(LayoutElement));
            spacer.transform.SetParent(parent, false);
            spacer.GetComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
