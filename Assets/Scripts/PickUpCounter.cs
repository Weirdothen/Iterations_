using UnityEngine;
using TMPro;
using Iterations.Events;

namespace Iterations.UI
{
    public class PickupCounterUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text counterText;
        [SerializeField] private int totalPickups = 10;
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
            if (counterText != null)
                counterText.text = $"{currentCount}/{totalPickups}";
        }
    }
}