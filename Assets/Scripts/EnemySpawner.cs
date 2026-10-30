using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Coordinates")]
    public Vector2 spawnPosition = new Vector2(5f, 2f);

    private bool _hasSpawned;

    public void SpawnEnemy()
    {
        if (_hasSpawned || enemyPrefab == null)
            return;

        Instantiate(enemyPrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0f), Quaternion.identity);

        _hasSpawned = true;
    }
}