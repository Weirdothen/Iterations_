using Iterations.Core;
using UnityEngine;

public class SinglePlayerManagerSwitch : MonoBehaviour
{
    private void Start()
    {
        SetSinglePlayerManagers(true);
    }

    public void OnMultiplayerPressed()
    {
        SetSinglePlayerManagers(false);
    }

    private void SetSinglePlayerManagers(bool active)
    {
        GameManager[] managers = FindObjectsByType<GameManager>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (GameManager manager in managers)
        {
            manager.gameObject.SetActive(active);
        }
    }
}