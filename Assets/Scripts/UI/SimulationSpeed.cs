
using UnityEngine;


public class SimulationSpeed : MonoBehaviour
{

    void Start()
    {
        Time.timeScale = 1f;
    }

    public void ChangeSpeed(float speed)
    {
        Time.timeScale = speed;
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
