using UnityEngine;

namespace PawnshopKing.Core
{
    /// <summary>
    /// Entry point for the Bootstrap scene (GDD 37): keeps the manager rig alive
    /// across scene loads and kicks off a campaign. Sits on the root object that
    /// also carries GameManager and DayManager.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [Tooltip("Start a new campaign immediately on play — handy until a main menu exists.")]
        [SerializeField] private bool autoStartNewGame = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (autoStartNewGame)
            {
                GameManager.Instance.StartNewGame();
            }
        }
    }
}
