using UnityEngine;
using TMPro;
using Iterations.Events;
using Iterations.Core;

namespace Iterations.UI
{
    public class PickupCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private IntEventChannelSO onPickupCollected;

        private void OnEnable()
        {
            if (onPickupCollected != null)
                onPickupCollected.OnEventRaised += HandlePickupCollected;

            UpdateText(0);
        }

        private void OnDisable()
        {
            if (onPickupCollected != null)
                onPickupCollected.OnEventRaised -= HandlePickupCollected;
        }

        private void HandlePickupCollected(int currentCount)
        {
            UpdateText(currentCount);
        }

        private void UpdateText(int currentCount)
        {
            if (counterText == null) return;

            int total = LevelConfig.Instance != null ? LevelConfig.Instance.TotalPickups : 0;
            counterText.text = $"{currentCount}/{total}";
        }
    }
}