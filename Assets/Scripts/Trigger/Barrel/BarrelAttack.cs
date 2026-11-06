using Player;
using UnityEngine;

namespace Trigger.Barrel
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class BarrelHazard : MonoBehaviour
    {
        private Rigidbody2D _rb;
            
        private bool _stopped;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _stopped = false;
        }

        private bool IsMoving()
        {
            if (_rb.linearVelocity.sqrMagnitude == 0) _stopped = true;
        
            return _rb.linearVelocity.sqrMagnitude > 0.025f;
        }
        private void CheckPlayer(Collider2D other)
        {
            // Jeśli beczka stanie, nie zada obrażeń
            if (!IsMoving() || _stopped)
                return;

            PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

            if (player == null || player.IsDead)
                return;

            player.TakeDamage(int.MaxValue, transform);
        }
    
        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckPlayer(collision.collider);
        }
    }
}