using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Prefab")]
    public GameObject enemyPrefab;

    [Header("Spawn Coordinates")]
    public Vector2 spawnPosition = new Vector2(5f, 2f);

    private bool hasSpawned = false;

    public void SpawnEnemy()
    {
        if (hasSpawned)
            return;

        if (enemyPrefab == null)
        {
            Debug.LogError("Nie przypisano prefabu przeciwnika!");
            return;
        }

        Instantiate(
            enemyPrefab,
            new Vector3(spawnPosition.x, spawnPosition.y, 0f),
            Quaternion.identity
        );

        hasSpawned = true;
    }
}