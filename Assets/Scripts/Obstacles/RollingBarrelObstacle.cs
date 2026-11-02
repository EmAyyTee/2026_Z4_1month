using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class RollingBarrelObstacle : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 15;
    [SerializeField, Min(0.1f)] private float lifetime = 12f;
    [SerializeField] private float despawnHeight = -20f;
    [SerializeField] private float initialSpeed = 0.8f;

    private void Start()
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.right * initialSpeed;
    }

    private void OnEnable()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (transform.position.y < despawnHeight)
            Destroy(gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (playerHealth == null)
            return;

        playerHealth.TakeDamage(damage, transform);
        Destroy(gameObject);
    }
}
