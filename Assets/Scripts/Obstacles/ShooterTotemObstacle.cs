
using Player;
using Ship.Cannon_Ball;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ShooterTotemObstacle : MonoBehaviour
{
    private static readonly int Attack =
        Animator.StringToHash("Attack");

    [Header("Projectile")]
    [SerializeField] private CannonBall cannonBallPrefab;
    [SerializeField] private Transform firePoint;

    [Header("Shooting")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 8f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(0.1f)] private float fireCooldown = 1.5f;
    [SerializeField, Min(1)] private int damage = 10;

    private PlayerHealth target;
    private float nextFireTime;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Wyszukiwanie gracza.
        if (target == null)
            target = FindAnyObjectByType<PlayerHealth>();

        if (target == null ||
            target.IsDead ||
            cannonBallPrefab == null ||
            firePoint == null)
        {
            return;
        }

        // Obliczamy kierunek od pułapki do gracza.
        Vector2 offset =
            (Vector2)target.transform.position - (Vector2)firePoint.position;

        // Sprawdzamy zasięg wykrywania.
        if (offset.sqrMagnitude > detectionRadius * detectionRadius)
            return;

        // Sprawdzamy cooldown.
        if (Time.time < nextFireTime)
            return;

        ShootAtPlayer(offset);

        nextFireTime = Time.time + fireCooldown;
    }

    private void ShootAtPlayer(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        // Animacja ataku.
        if (animator != null)
            animator.SetTrigger(Attack);

        // Tworzymy pocisk.
        CannonBall cannonBall = Instantiate(
            cannonBallPrefab,
            firePoint.position,
            Quaternion.identity
        );

        // Zachowujemy skalę pułapki.
        cannonBall.transform.localScale = transform.lossyScale;

        // Wystrzeliwujemy pocisk w kierunku gracza.
        cannonBall.LaunchFromTrap(
            direction.normalized * projectileSpeed,
            damage,
            transform
        );
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;

        Vector3 position = firePoint != null
            ? firePoint.position
            : transform.position;

        Gizmos.DrawWireSphere(position, detectionRadius);
    }
}
