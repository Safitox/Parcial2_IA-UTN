using UnityEngine;

public class ObjectOfInterest : MonoBehaviour
{
    [SerializeField] private float _flickSpeed = 5f;
    private float _flickTimer;
    private bool _flicker;
    private MeshRenderer display;
    public float health = 5f;

    void Awake()
    {
        display = GetComponent<MeshRenderer>();
    }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0f) Destroy(gameObject);
    }

    void Update()
    {
        _flickTimer += Time.deltaTime;
        if (_flickTimer < _flickSpeed) return;
        _flickTimer = 0f;
        _flicker = !_flicker;
        if (display == null) return;
        if (_flicker)
        {
            display.material.color = Color.red;
        }
        else
        {
            display.material.color = Color.white;
        }
    }
}
