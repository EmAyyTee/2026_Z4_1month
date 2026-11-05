using Player;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BarrelSpawner : MonoBehaviour
{
    [SerializeField] private RollingBarrelObstacle prefab;
    [SerializeField, Min(0.5f)] private float spawnInterval = 4f;
    [SerializeField, Min(0.1f)] private float activationDistance = 4f;
    private PlayerHealth target;
    private float nextSpawnTime;
    private float nextSearchTime;

    private void Update()
    {
        if (target == null && Time.time >= nextSearchTime)
        {
            target = FindAnyObjectByType<PlayerHealth>();
            nextSearchTime = Time.time + 1f;
        }
        if (prefab == null || target == null || target.IsDead || Time.time < nextSpawnTime)
            return;
        if ((target.transform.position - transform.position).sqrMagnitude > activationDistance * activationDistance)
            return;
        var barrel = Instantiate(prefab, transform.position, Quaternion.identity);
        barrel.transform.localScale = transform.lossyScale;
        nextSpawnTime = Time.time + spawnInterval;
    }
}
