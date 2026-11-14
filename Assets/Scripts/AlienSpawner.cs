using UnityEngine;

public class AlienSpawner : MonoBehaviour
{
    [SerializeField] private GameObject _alienPrefab;
    [SerializeField] private int _alienCount = 5;
    [SerializeField] private Transform _parent;

    void Start()
    {
        for (int i = 0; i < _alienCount; i++)
        {
            Vector3 spawnPosition;

            if (NavGraph.Instance != null && NavGraph.Instance.HuntingNodes.Count > 0)
            {
                int randomIndex = Random.Range(0, NavGraph.Instance.HuntingNodes.Count);
                spawnPosition = NavGraph.Instance.HuntingNodes[randomIndex].position;
            }
            else if (Universe.Instance != null)
            {
                spawnPosition = Universe.Instance.GetRandomPositionAwayFrom();
            }
            else
            {
                spawnPosition = transform.position;
            }

            Instantiate(_alienPrefab, spawnPosition, Quaternion.identity, _parent);
        }
    }
}
