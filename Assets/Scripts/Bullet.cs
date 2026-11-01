using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float _life = 1f;
    private Vector3 _direction;
    private float _speed;
    private float _damage;

    public void Init(Vector3 dir, float speed, float dmg)
    {
        _direction = dir.normalized;
        _speed = speed;
        _damage = dmg;

        Destroy(gameObject, _life);
    }

    private void Update()
    {
        transform.position += _direction * _speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        Alien agent = other.GetComponent<Alien>();
        if (agent != null)
        {
            agent.TakeDamage(_damage);
        }

        Destroy(gameObject);
    }
}