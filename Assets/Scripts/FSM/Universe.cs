using UnityEngine;

public class Universe : MonoBehaviour
{
    public static Universe Instance {get; private set;}
    [SerializeField] private float _width = 60f;
    [SerializeField] private float _height = 35f;
    [SerializeField] private float _respawnDistanceFromHunter = 10f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public Vector3 GetRandomPositionAwayFrom(int maxAttempts = 100)
    {
        float halfWidth = _width * 0.5f;
        float halfHeight = _height * 0.5f;
        float minimumDistanceSqr = _respawnDistanceFromHunter * _respawnDistanceFromHunter;

        Vector3 bestPosition = Vector3.zero;
        float bestDistanceSqr = -1f;

        for (int i = 0; i < maxAttempts; i++)
        {
            Vector3 candidate = new Vector3(Random.Range(-halfWidth, halfWidth),0f, Random.Range(-halfHeight, halfHeight));
            float distanceSqr = (candidate - HunterNPC.Instance.transform.position).sqrMagnitude;

            if (distanceSqr >= minimumDistanceSqr) return candidate;

            if (distanceSqr > bestDistanceSqr)
            {
                bestDistanceSqr = distanceSqr;
                bestPosition = candidate;
            }
        }

        Debug.LogWarning("No se encontró una posición que cumpla la distancia mínima. Se utilizará la posición más alejada encontrada.");
        return bestPosition;
    }

    public Vector3 CalculateLimitPosition(Vector3 position)
    {
        Vector3 newPosition = position;

        if (position.x > _width / 2f) newPosition.x = -_width / 2f;
        if (position.x < -_width / 2f) newPosition.x = _width / 2f;
        if (position.z > _height / 2f) newPosition.z = -_height / 2f;
        if (position.z < -_height / 2f) newPosition.z = _height / 2f;

        return newPosition;
    }

    public void CalculateBounce(ref Vector3 position, ref Vector3 velocity)
    {
        float halfWidth = _width * 0.5f;
        float halfHeight = _height * 0.5f;

        if (position.x > halfWidth)
        {
            position.x = halfWidth;
            velocity.x = -Mathf.Abs(velocity.x);
        }
        else if (position.x < -halfWidth)
        {
            position.x = -halfWidth;
            velocity.x = Mathf.Abs(velocity.x);
        }

        if (position.z > halfHeight)
        {
            position.z = halfHeight;
            velocity.z = -Mathf.Abs(velocity.z);
        }
        else if (position.z < -halfHeight)
        {
            position.z = -halfHeight;
            velocity.z = Mathf.Abs(velocity.z);
        }
    }

}
