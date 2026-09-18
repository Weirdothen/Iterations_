using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;


public class ArenaSelectoinUi : MonoBehaviour
{
    private Outline[] buttonoutlines;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        buttonoutlines = new Outline[transform.childCount];
        for(int i =0; i< transform.childCount; i++)
        {
            buttonoutlines[i] =  transform.GetChild(i).GetComponent<Outline>();
            int index = i;
            if (CharacterSelectReady.Instance.IsPartyLeader)
            {
                transform.GetChild(i).GetComponent<Button>().onClick.AddListener(() =>
                {
                    DisableOtherOutlines(index);
                    CharacterSelectReady.Instance.SelectArena(index);
                });
            }
            else
            {
                transform.GetChild(i).GetComponent<Button>().interactable = false;
            }

        }
        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);

        CharacterSelectReady.Instance.OnAnyReadyStateChanged += Instance_OnAnyReadyStateChanged;
        readyButtonText.SetText("not Ready");
    }

    private void Instance_OnAnyReadyStateChanged()
    {
        Debug.Log("the event is work");
        // ready button
        readyButtonText.SetText( CharacterSelectReady.Instance.IsPlayerReady(NetworkManager.Singleton.LocalClientId)? "Ready": "not Ready");

        DisableOtherOutlines(CharacterSelectReady.Instance.SelectedArenaIndex);
    }

    void DisableOtherOutlines(int activeOutline)
    {
        for(int i = 0;i<buttonoutlines.Length; i++)
        {
            if(i == activeOutline)
            {
                buttonoutlines[i].enabled = true;
            }
            else
            {
                buttonoutlines[i].enabled = false;
            }
        }
    }
}
