using UnityEngine;

namespace Iterations.UI
{
    public class OpenURLButton : MonoBehaviour
    {
        
        [SerializeField] private string url;

        public void OpenURL()
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Debug.LogWarning($"OpenURLButton on '{gameObject.name}' was clicked but no URL is set.", this);
                return;
            }

            Application.OpenURL(url);
        }
    }
}