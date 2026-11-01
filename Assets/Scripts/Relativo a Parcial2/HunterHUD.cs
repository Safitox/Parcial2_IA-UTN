using UnityEngine;
using UnityEngine.UI;

public class HunterHUD : MonoBehaviour
{
    public Text stateText;
    public Text gatherText;
    public Text wolfText;

    void Update()
    {
        HunterNPC hunter = HunterNPC.Instance;

        if (hunter == null)
        {
            return;
        }

        if (stateText != null && hunter.FSM.CurrentState != null)
        {
            string stateName = hunter.FSM.CurrentState.GetType().Name;
            stateName = stateName.Replace("P2", "");
            stateText.text = "Cazador: " + stateName;
        }

        if (gatherText != null)
        {
            gatherText.text = "Recolecciones: " + hunter.GatherCount + "/" + hunter.RequiredGathers;
        }

        if (wolfText != null)
        {
            if (hunter.DetectWolf())
            {
                wolfText.text = "Lobo detectado - Huyendo";
            }
            else
            {
                wolfText.text = "";
            }
        }
    }
}