
using Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ObstacleProjectile : MonoBehaviour
{
    [Header("Projectile")]
    [SerializeField, Min(0.1f)] private float lifetime = 6f;

    [Header("Explosion")]
    public GameObject explosionPrefab;
    public float explosionAnimationDuration = 0.8f;

    private Rigidbody2D projectileBody;
    private Transform source;
    private int damage;
    private bool hasExploded;

    private void Awake()
    {
        projectileBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(
        Vector2 velocity,
        int projectileDamage,
        Transform projectileSource)
    {
        damage = projectileDamage;
        source = projectileSource;

        projectileBody.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded)
            return;

        // Ignorujemy obiekt, który wystrzelił pocisk.
        if (source != null &&
            (other.transform == source ||
             other.transform.IsChildOf(source)))
        {
            return;
        }

        PlayerHealth playerHealth =
            other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null && !playerHealth.IsDead)
        {
            playerHealth.TakeDamage(
                damage,
                source == null ? transform : source
            );

            Explode();
            return;
        }

        // Eksplozja po kontakcie z obiektem niebędącym triggerem.
        if (!other.isTrigger)
            Explode();
    }

    private void Explode()
    {
        if (hasExploded)
            return;

        hasExploded = true;

        if (explosionPrefab != null)
        {
            GameObject explosion = Instantiate(
                explosionPrefab,
                transform.position,
                Quaternion.identity
            );

            Animator animator = explosion.GetComponent<Animator>();

            if (animator != null)
                animator.Play(0, 0, 0f);

            Destroy(explosion, explosionAnimationDuration);
        }

        Destroy(gameObject);
    }
}