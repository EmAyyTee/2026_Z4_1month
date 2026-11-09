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
    [Header("Shooting")]
    [SerializeField, Min(0.1f)] private float detectionRadius = 8f;
    [SerializeField, Min(0.1f)] private float projectileSpeed = 7f;
    [SerializeField, Min(0.1f)] private float fireCooldown = 1.5f;
    [SerializeField, Min(1)] private int damage = 10;

    private PlayerHealth target;
    private float nextFireTime;
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (target == null)
            target = FindAnyObjectByType<PlayerHealth>();

        if (target == null ||
            target.IsDead ||
            cannonBallPrefab == null ||
            firePoint == null)
            return;

        Vector2 offset = target.transform.position - firePoint.position;

        if (offset.sqrMagnitude > detectionRadius * detectionRadius ||
            Time.time < nextFireTime)
            return;

        if (_animator != null)
            _animator.SetTrigger(Attack);

        CannonBall cannonBall = Instantiate(
            cannonBallPrefab,
            firePoint.position,
            Quaternion.identity
        );

        cannonBall.transform.localScale = transform.lossyScale;

        cannonBall.LaunchFromTrap(
            offset.normalized * projectileSpeed,
            damage,
            transform
        );

        nextFireTime = Time.time + fireCooldown;
    }
}