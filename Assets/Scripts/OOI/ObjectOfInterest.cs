using UnityEngine;

public class ObjectOfInterest : MonoBehaviour
{
    [SerializeField] private float _flickSpeed = 5f;
    private float _flickTimer;
    private bool _flicker;
    public float health = 5f;
    public void TakeDamage(float dmg)
    {
        health -= dmg;
        if (health <= 0)
            Destroy(gameObject);
    }

    private void Update()
    {
        _flickTimer += Time.deltaTime;
        if (_flickTimer >= _flickSpeed)
        {
            _flickTimer = 0f;
            _flicker = !_flicker;
            GetComponent<MeshRenderer>().material.color = _flicker ? Color.red : Color.white;
        }

    }

    private void OnEnable()
    {
        ObjectsOfInterestManager.Items.Add(this);
    }

    private void OnDisable()
    {
        ObjectsOfInterestManager.Items.Remove(this);
    }
}
