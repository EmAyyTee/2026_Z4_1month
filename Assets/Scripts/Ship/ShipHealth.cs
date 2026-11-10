using Ship.Cannon_Ball;
using UnityEngine;
using UnityEngine.UI;

namespace Ship
{
    public class BossHealth : MonoBehaviour
    {
        private static readonly int Hit = Animator.StringToHash("Hit");
        private static readonly int Dead = Animator.StringToHash("Dead");

        [Header("Health")]
        public int maxHealth = 100;

        [Header("HUD")]
        public Slider healthBar;
        
        private Rigidbody2D _rb;
        private Animator _animator;
        private ShipMovement _shipMovement;
        private CannonBallSpawner _cannonBallSpawner;
        
        private bool _isDead;
        private int _currentHealth;
        private bool _isHitting;
        
        // Udostępnia _isDead pod isdead
        public bool IsDead => _isDead;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _animator = GetComponent<Animator>();
            _shipMovement = GetComponent<ShipMovement>();
            _cannonBallSpawner = FindFirstObjectByType<CannonBallSpawner>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            
            maxHealth = Mathf.Max(1, maxHealth);
            _currentHealth = maxHealth;

            UpdateHealthUI();
        }

        private void UpdateHealthUI()
        {
            if (healthBar == null)
                return;

            healthBar.minValue = 0;
            healthBar.maxValue = maxHealth;
            healthBar.value = _currentHealth;
        }

        public void TakeDamage(int damage, Transform attacker)
        {
            if (_isDead || damage <= 0)
                return;
        
            _currentHealth = Mathf.Max(0, _currentHealth - damage);
        
            StartCoroutine(HitSequence());
        
            UpdateHealthUI();
        
            if (_currentHealth <= 0)
                Die();
        }

        private System.Collections.IEnumerator HitSequence()
        {
            if (_isHitting || _shipMovement == null)
                yield break;

            _isHitting = true;
            
            // Zatrzymujemy respienie nowych kul na czas "animacji" otrzymania obrażeń
            if (_cannonBallSpawner != null) _cannonBallSpawner.PauseSpawning();
            
            // Wyłączenie normalnych sprite
            if (_shipMovement.wind != null) _shipMovement.wind.SetActive(false);
            if (_shipMovement.cannonShoot != null) _shipMovement.cannonShoot.SetActive(false);
            if (_shipMovement.cannonReload != null) _shipMovement.cannonReload.SetActive(false);
        
            // Odtworzenie "animacji" otrzymania obrażeń
            if (_animator != null) _animator.SetTrigger(Hit);
            
            // Włączenie sprite otrzymywania obrażeń
            if (_shipMovement.sailHit != null) _shipMovement.sailHit.SetActive(true);
            if (_shipMovement.cannonHit != null) _shipMovement.cannonHit.SetActive(true);
            
            // Czekamy
            yield return new WaitForSeconds(0.35f);
            
            // Powrót do normalnych sprite
            if (_shipMovement.sailHit != null) _shipMovement.sailHit.SetActive(false);
            if (_shipMovement.cannonHit != null) _shipMovement.cannonHit.SetActive(false);
            if (_shipMovement.wind != null) _shipMovement.wind.SetActive(true);
        
            _isHitting = false;
        
            if (_cannonBallSpawner != null) _cannonBallSpawner.ResumeSpawning();
        }

        private void Die()
        {
            if (_isDead)
                return;

            _isDead = true;

            _currentHealth = 0;
            UpdateHealthUI();

            if (_animator != null) _animator.SetTrigger(Dead);

            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
                _rb.simulated = false;
            }

            Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

            foreach (Collider2D col in colliders)
                col.enabled = false;

            Destroy(gameObject);
            Destroy(healthBar.transform.parent.gameObject);
            if (_shipMovement.wall != null) _shipMovement.wall.SetActive(false);
        }
    }
}