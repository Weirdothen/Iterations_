using UnityEngine;

namespace Iterations.Core
{
    public class LevelConfig : MonoBehaviour
    {
        [Tooltip("Scene to load when THIS level is won. Leave empty if this is the last level.")]
        [SerializeField] private string nextLevelSceneName;

        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetNextLevel(nextLevelSceneName);
        }
    }
}