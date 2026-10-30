using UnityEngine;

public class BarrelSpawner : MonoBehaviour
{
    [Header("Barrel Prefab")]
    public GameObject barrelPrefab;

    [Header("Spawn Coordinates")]
    public Vector2 spawnPosition = new Vector2(5f, 2f);

    private bool hasSpawned = false;

    public void SpawnBarrel()
    {
        if (hasSpawned)
            return;

        if (barrelPrefab == null)
        {
            Debug.LogError("Nie przypisano prefabu przeciwnika!");
            return;
        }

        Instantiate(
            barrelPrefab,
            new Vector3(spawnPosition.x, spawnPosition.y, 0f),
            Quaternion.identity
        );

        hasSpawned = true;
    }
}