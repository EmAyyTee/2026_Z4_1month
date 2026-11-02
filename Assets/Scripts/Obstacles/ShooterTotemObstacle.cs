using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShooterTotemObstacle : MonoBehaviour
{
    [SerializeField] private ObstacleProjectile projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField, Min(0.1f)] private float detectionRadius = 8f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(0.1f)] private float fireCooldown = 1.5f;
    [SerializeField, Min(1)] private int damage = 10;

    private PlayerHealth target;
    private float nextFireTime;

    private void Update()
    {
        if (target == null)
            target = FindAnyObjectByType<PlayerHealth>();

        if (target == null || projectilePrefab == null || firePoint == null)
            return;

        Vector2 offset = target.transform.position - firePoint.position;

        if (offset.sqrMagnitude > detectionRadius * detectionRadius || Time.time < nextFireTime)
            return;

        ObstacleProjectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
        projectile.transform.localScale = transform.lossyScale;
        projectile.Launch(offset.normalized * projectileSpeed, damage, transform);
        nextFireTime = Time.time + fireCooldown;
    }
}
