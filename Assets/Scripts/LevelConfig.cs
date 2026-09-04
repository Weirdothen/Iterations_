using UnityEngine;

namespace Iterations.Core
{
    public class LevelConfig : MonoBehaviour
    {
        public static LevelConfig Instance { get; private set; }

        [Tooltip("Scene to load when THIS level is won. Leave empty if this is the last level.")]
        [SerializeField] private string nextLevelSceneName;

        [Tooltip("How many pickups must be collected to finish this level.")]
        [SerializeField] private int totalPickups = 10;

        public int TotalPickups => totalPickups;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetNextLevel(nextLevelSceneName);
        }
    }
}