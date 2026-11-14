using UnityEngine;
using UnityEngine.UI;

public class HunterHUD : MonoBehaviour
{
    [SerializeField] private HunterNPC hunter;

    public TMPro.TextMeshProUGUI stateText;
    public TMPro.TextMeshProUGUI gatherText;
    public TMPro.TextMeshProUGUI wolfText;

    float nextUpdate;

    void Update()
    {
        if (Time.time < nextUpdate) return;
        nextUpdate = Time.time + 0.2f;
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