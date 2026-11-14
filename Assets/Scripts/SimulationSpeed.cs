
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationSpeed : MonoBehaviour
{
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;

    void Start()
    {
        Time.timeScale = 1f;
        speedSlider.minValue = 1f;
        speedSlider.maxValue = 2f;
        speedSlider.value = 1f;
        speedSlider.onValueChanged.AddListener(ChangeSpeed);
        ChangeSpeed(speedSlider.value);
    }

    public void ChangeSpeed(float speed)
    {
        Time.timeScale = speed;
        speedText.text = speed.ToString("0.0") + "x";
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }
}
