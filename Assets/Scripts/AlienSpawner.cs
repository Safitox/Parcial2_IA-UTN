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

            if (NavGraph.Instance != null)
            {
                int randomIndex = Random.Range(0, NavGraph.Instance.HuntingNodes.Count);
                spawnPosition = NavGraph.Instance.HuntingNodes[randomIndex].position;
            }
            else
            {
                spawnPosition = Universe.Instance.GetRandomPositionAwayFrom();
            }

            Instantiate(_alienPrefab, spawnPosition, Quaternion.identity, _parent);
        }
    }
}
