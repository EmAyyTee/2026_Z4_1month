using Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class ObstacleProjectile : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float lifetime = 6f;

    private Rigidbody2D projectileBody;
    private Transform source;
    private int damage;

    private void Awake()
    {
        projectileBody = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 velocity, int projectileDamage, Transform projectileSource)
    {
        damage = projectileDamage;
        source = projectileSource;
        projectileBody.linearVelocity = velocity;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (source != null && (other.transform == source || other.transform.IsChildOf(source)))
            return;

        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();

        if (playerHealth != null)
            playerHealth.TakeDamage(damage, source == null ? transform : source);

        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
