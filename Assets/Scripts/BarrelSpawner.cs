using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
    [Header("Barrel Prefab")]
    public GameObject barrelPrefab;

    [Header("Spawn Coordinates")]
    public Vector2 spawnPosition = new Vector2(5f, 2f);

    private bool _hasSpawned;

    public void SpawnBarrel()
    {
        if (_hasSpawned || barrelPrefab == null)
            return;
        
        Instantiate(barrelPrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0f), Quaternion.identity);

        _hasSpawned = true;
    }
}