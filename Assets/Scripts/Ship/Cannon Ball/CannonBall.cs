using Player;
using UnityEngine;

namespace Ship.Cannon_Ball
{
    public class CannonBall : MonoBehaviour
    {
        [Header("Reflection")]
        public bool canBeReflected;

        [Header("BOOM")]
        public GameObject explosionPrefab;

        [Header("Damage")]
        public int damageToPlayer = 15;
        public int damageToBoss = 20;

        private Rigidbody2D _rb;
        private BossHealth _bossHealth;
        
        private bool _isReflected;
        private bool _hasExploded;
        
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            GameObject boss = GameObject.FindGameObjectWithTag("Boss");

            if (boss != null)
                _bossHealth = boss.GetComponent<BossHealth>();
        }

        private void Start()
        {
            if (_rb != null)
                _rb.linearVelocity = Vector2.down * 0.1f;
        }
    
        private BossHealth FindBossHealth()
        {
            // Szuka wszystkich bosshealth
            BossHealth[] bosses = FindObjectsByType<BossHealth>(FindObjectsSortMode.None);

            foreach (BossHealth boss in bosses)
                if (boss != null && !boss.IsDead)
                    return boss;
        
            return null;
        }

        public void TakeDamage(int damage, Transform attacker)
        {
            if (!canBeReflected || _hasExploded)
                return;

            if (damage <= 0)
                return;
        
            // Odbicie kuli w stronę bossa
            Reflect(attacker);
        }

        private void Reflect(Transform attacker)
        {
            if (_hasExploded || _rb == null)
                return;

            _bossHealth = FindBossHealth();

            if (_bossHealth == null)
                return;

            _isReflected = true;

            // Kierunek
            float horizontalDirection = Mathf.Sign(_bossHealth.transform.position.x - transform.position.x);

            if (horizontalDirection == 0)
                horizontalDirection = 1f;

            // Kąt odbicia
            float angleRadians = 80f * Mathf.Deg2Rad;

            // Nowa prędkość początkowa
            Vector2 velocity = new Vector2(horizontalDirection * Mathf.Cos(angleRadians), Mathf.Sin(angleRadians)) * 1;

            // Dodatkowy knockback
            if (attacker != null)
            {
                float knockbackDirection = Mathf.Sign(transform.position.x - attacker.position.x);

                if (knockbackDirection == 0)
                    knockbackDirection = horizontalDirection;

                velocity += new Vector2(knockbackDirection * 0.5f, 1.5f);
            }

            // Ustawienie fizyki
            _rb.gravityScale = 0.15f;

            // Zerujemy poprzedni ruch
            _rb.linearVelocity = Vector2.zero;
            _rb.angularVelocity = 0f;

            // Nadajemy nową prędkość
            _rb.linearVelocity = velocity;
        }

        private void HitBoss()
        {
            if (_hasExploded)
                return;

            _hasExploded = true;

            if (_bossHealth == null)
                _bossHealth = FindBossHealth();

            if (_bossHealth != null && !_bossHealth.IsDead)
                _bossHealth.TakeDamage(damageToBoss, transform);
        
            Destroy(gameObject);
        }

        private void Explode()
        {
            if (_hasExploded)
                return;

            _hasExploded = true;

            if (explosionPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);

                Animator animator = explosion.GetComponent<Animator>();

                if (animator != null)
                    animator.Play(0, 0, 0f);

                Destroy(explosion, 0.5f);
            }

            Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.3f);

            foreach (Collider2D hit in hits)
            {
                if (!hit.CompareTag("Player"))
                    continue;

                PlayerHealth playerHealth = hit.GetComponentInParent<PlayerHealth>();

                if (playerHealth != null && !playerHealth.IsDead)
                    playerHealth.TakeDamage(damageToPlayer, transform);
            }
            Destroy(gameObject);
        }

    
        private void OnCollisionEnter2D(Collision2D collision)
        {
            HandleContact(collision.gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            HandleContact(other.gameObject);
        }

        private void HandleContact(GameObject other)
        {
            if (_hasExploded)
                return;

            BossHealth boss = other.GetComponentInParent<BossHealth>();

            // Boss otrzymuje obrażenia tylko od odbitej kuli
            if (boss != null)
            {
                if (_isReflected)
                {
                    _bossHealth = boss;
                    HitBoss();
                }
                return;
            }

            // Kontakt z graczem
            if (other.CompareTag("Player"))
            {
                if (!canBeReflected)
                    Explode();

                return;
            }

            // Kontakt z podłożem.
            Transform current = other.transform;

            while (current != null)
            {
                if (current.CompareTag("Ground"))
                {
                    Explode();
                    return;
                }
                current = current.parent;
            }
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}