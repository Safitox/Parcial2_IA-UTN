using UnityEngine;

public class AgentFeedback : MonoBehaviour
{
    Renderer[] renderers;
    Color lastColor;
    bool initialized;

    void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>();
    }

    public void SetColor(Color color)
    {
        if (initialized && color == lastColor) return;
        lastColor = color;
        initialized = true;
        foreach (Renderer item in renderers)
        {
            if (item != null && item.enabled) item.material.color = color;
        }
    }
}
