using PawnshopKing.Core;
using PawnshopKing.Systems.SaveLoad;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PawnshopKing.UI
{
    /// <summary>
    /// Boot menu (built entirely from code): Continue resumes the autosaved
    /// campaign at the next morning, New Game starts fresh — the single save slot
    /// belongs to whichever campaign runs. Sits above every other canvas and is
    /// shown by GameBootstrap instead of auto-starting a campaign.
    /// </summary>
    public class MainMenuUIManager : MonoBehaviour
    {
        public static MainMenuUIManager Instance { get; private set; }

        private GameManager gm;
        private GameObject screenRoot;
        private TextMeshProUGUI continueLabel;
        private TextMeshProUGUI newGameLabel;
        private TextMeshProUGUI noticeText;

        private void Awake()
        {
            Instance = this;
            gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("MainMenuUIManager requires GameManager to exist first.");
                enabled = false;
                return;
            }

            BuildScreen();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool IsVisible => screenRoot != null && screenRoot.activeSelf;

        public void Show()
        {
            RefreshButtons();
            screenRoot.SetActive(true);
            UIFx.FadeIn(this, screenRoot);
        }

        public void Close() => screenRoot.SetActive(false);

        /// <summary>
        /// Continue only appears when the save actually reads back — HasSave alone
        /// would offer a corrupt file. New Game says up front that it takes the slot.
        /// </summary>
        private void RefreshButtons()
        {
            var saved = SaveLoadSystem.Load();
            bool hasSave = saved != null;

            var continueButton = continueLabel.transform.parent.gameObject;
            continueButton.SetActive(hasSave);
            if (hasSave)
            {
                continueLabel.text = $"Continue — Day {saved.currentDay + 1}";
            }

            newGameLabel.text = hasSave ? "New Game (erases save)" : "New Game";

            noticeText.text = !hasSave && SaveLoadSystem.HasSave()
                ? "A previous save exists but could not be read."
                : string.Empty;
        }

        private void OnContinueClicked()
        {
            if (!gm.ContinueFromSave())
            {
                RefreshButtons();  // save vanished or went bad since Show — re-offer honestly
                return;
            }

            Close();
        }

        private void OnNewGameClicked()
        {
            gm.StartNewGame();
            Close();
        }

        // ---- Construction ------------------------------------------------------

        private void BuildScreen()
        {
            var canvasGO = new GameObject("MainMenuCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);

            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 40; // above HUD, inventory, upgrades, and summary

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            // Fully opaque: at boot there is no shop behind it worth glimpsing.
            var root = HUDUIManager.CreatePanel(canvasGO.transform, "MainMenuScreen", UITheme.Background);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = root.offsetMax = Vector2.zero;
            screenRoot = root.gameObject;

            var panel = new GameObject("MenuPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
            panel.transform.SetParent(root, false);
            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(640f, 560f);

            var layout = panel.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            var title = HUDUIManager.CreateText(panel.transform, "Title", 72f, TextAlignmentOptions.Center, FontStyles.Bold, header: true);
            title.text = "PAWNSHOP KING";
            title.color = UITheme.NeonCyan;
            ((RectTransform)title.transform).sizeDelta = new Vector2(640f, 90f);

            var tagline = HUDUIManager.CreateText(panel.transform, "Tagline", 22f, TextAlignmentOptions.Center, FontStyles.Italic);
            tagline.text = "Every deal is a story. Most of them end badly for someone.";
            tagline.color = HUDUIManager.MutedColor;
            ((RectTransform)tagline.transform).sizeDelta = new Vector2(640f, 60f);

            continueLabel = CreateMenuButton(panel.transform, "Continue", OnContinueClicked);
            newGameLabel = CreateMenuButton(panel.transform, "New Game", OnNewGameClicked);

            noticeText = HUDUIManager.CreateText(panel.transform, "Notice", 18f, TextAlignmentOptions.Center, FontStyles.Italic);
            noticeText.color = UITheme.Danger;
            ((RectTransform)noticeText.transform).sizeDelta = new Vector2(640f, 30f);

            screenRoot.SetActive(false);
        }

        private static TextMeshProUGUI CreateMenuButton(Transform parent, string label, UnityEngine.Events.UnityAction onClick)
        {
            var text = HUDUIManager.CreateSmallButton(parent, label, 420f, onClick);
            var buttonRect = (RectTransform)text.transform.parent;
            buttonRect.sizeDelta = new Vector2(420f, 64f);
            var layoutElement = buttonRect.GetComponent<LayoutElement>();
            layoutElement.preferredHeight = 64f;
            text.fontSize = 26f;
            return text;
        }
    }
}
