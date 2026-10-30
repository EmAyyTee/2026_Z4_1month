using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BarrelHazard : MonoBehaviour
{
    [Header("Movement Detection")]
    [SerializeField] private float movementThreshold = 0.05f;

    private Rigidbody2D _rb;
    private bool _stopped;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _stopped = false;
    }

    private bool IsMoving()
    {
        if (_rb.linearVelocity.sqrMagnitude == 0)
            _stopped = true;
        return _rb.linearVelocity.sqrMagnitude > movementThreshold * movementThreshold;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        CheckPlayer(collision.collider);
    }

    private void CheckPlayer(Collider2D other)
    {
        // Jeśli beczka stoi, nie zadaje obrażeń.
        if (!IsMoving())
            return;
        
        if(_stopped)
            return;

        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        player.TakeDamage(int.MaxValue, transform);
    }
}