using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(MovingPlatform))]
public class MovingPlatformAttack : MonoBehaviour
{
    private MovingPlatform _platform;

    [Header("Deadly Height")]
    [SerializeField] private float deadlyHeight = 3f;

    private void Awake()
    {
        _platform = GetComponent<MovingPlatform>();
    }

    private void CheckPlayer(Collider2D other)
    {
        // Platforma musi poruszać się w dół.
        if (_platform.CurrentVelocity.y >= 0f)
            return;

        // Platforma musi znajdować się na wysokości śmiertelnej.
        if (transform.position.y > deadlyHeight)
            return;

        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        PlayerMovement movement = player.GetComponent<PlayerMovement>();

        if (movement == null)
            return;

        // Sprawdzamy, czy gracz rzeczywiście dotyka ziemi.
        bool touchingGround = movement.IsTouchingGround();

        // Zabijamy tylko wtedy, gdy gracz dotyka ziemi.
        if (touchingGround)
            player.KillImmediately();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }
}