using UnityEngine;

namespace PawnshopKing.Core
{
    /// <summary>
    /// Entry point for the Bootstrap scene (GDD 37): keeps the manager rig alive
    /// across scene loads and hands off to the main menu (Continue / New Game).
    /// Sits on the root object that also carries GameManager and DayManager.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Skip the main menu and start a fresh campaign immediately — dev shortcut only.")]
        [SerializeField] private bool skipMenuAndStartNewGame;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (skipMenuAndStartNewGame)
            {
                GameManager.Instance.StartNewGame();
                return;
            }

            UI.MainMenuUIManager.Instance.Show();
        }
    }
}
