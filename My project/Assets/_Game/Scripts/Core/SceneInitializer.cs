using UnityEngine;

namespace PawnshopKing.Core
{
    /// <summary>
    /// Self-assembling entry point for the zero-editor-wiring workflow: spawns itself
    /// before the first scene loads, then builds the whole manager rig in Awake.
    /// Press Play in any scene — even an empty one — and the game constructs itself.
    /// If a hand-built rig already exists in the scene, this does nothing.
    /// </summary>
    public class SceneInitializer : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void SpawnSelf()
        {
            new GameObject("SceneInitializer").AddComponent<SceneInitializer>();
        }

        private void Awake()
        {
            if (GameManager.Instance == null)
            {
                BuildManagerRig();
            }

            // One-shot: the rig persists across scenes, the initializer doesn't.
            Destroy(gameObject);
        }

        private static void BuildManagerRig()
        {
            var root = new GameObject("GameManagers");

            // AddComponent runs Awake immediately, so add in dependency order:
            // DayManager first, so GameManager.Awake can find it on the same object.
            var dayManager = root.AddComponent<DayManager>();
            dayManager.LoadArchetypes();

            root.AddComponent<GameManager>();

            // Audio before the UI, so button wiring can find the managers.
            root.AddComponent<Systems.Audio.AudioManager>();
            root.AddComponent<Systems.Audio.MusicManager>();

            // UI after GameManager (they grab the instance in Awake), before GameBootstrap
            // so the HUD is subscribed before Start auto-launches the campaign.
            root.AddComponent<UI.HUDUIManager>();
            root.AddComponent<UI.InventoryUIManager>();
            root.AddComponent<UI.UpgradeUIManager>();
            root.AddComponent<UI.DaySummaryUIManager>();
            root.AddComponent<UI.MainMenuUIManager>();
            root.AddComponent<UI.PauseMenuUIManager>();
            root.AddComponent<UI.AtmosphereOverlay>();
            root.AddComponent<UI.ShopSceneBackdrop>();

            // Marks the rig DontDestroyOnLoad in Awake, shows the main menu in Start.
            root.AddComponent<GameBootstrap>();
        }
    }
}
