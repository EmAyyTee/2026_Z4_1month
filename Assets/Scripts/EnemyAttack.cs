using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int Attack = Animator.StringToHash("Attack");

    [Header("Attack")]
    public float attackRange = 0.17f;
    public float attackCooldown = 1.1f;
    public int damage = 10;

    private readonly float _attackDuration = 0.35f;
    private readonly Collider2D[] _hits = new Collider2D[10]; // Przechowuje collidery wykryte podczas sprawdzania trafień.
    
    private Animator _animator;
    private EnemyHealth _enemyHealth;
    private PlayerHealth _playerHealth;
    private Transform _playerTarget;

    private bool _attackInput;
    private float _attackTimer;
    private float _attackCooldownTimer;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _enemyHealth = GetComponent<EnemyHealth>();
    }

    // Co klatkę sprawdza czy zaatakowano oraz obsługuje atak.
    private void Update()
    {
        ReadAttackInput();
        HandleAttack();
    }

    private void ReadAttackInput()
    {
        // Określa ilu przeciwników, jest w zasięgu ataku postaci.
        int hitCount = Physics2D.OverlapCircle(transform.position, attackRange, new ContactFilter2D(), _hits);

        // Usunięcie danych z poprzedniej klatki.
        _playerTarget = null;
        _playerHealth = null;
        _attackInput = false;

        // Przejście po wszystkich wykrytych przeciwnikach.
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _hits[i];

            if (hit == null || !hit.CompareTag("Player"))
                continue;

            // Szuka EnemyHealth na trafionym obiekcie
            _playerHealth = hit.GetComponentInParent<PlayerHealth>();

            // Pomija martwe postaci.
            if (_playerHealth == null || _playerHealth.IsDead)
                continue;

            _playerTarget = hit.transform;
            _attackInput = true;

            break;
        }
    }
    
    private void HandleAttack()
    {
        if (_animator != null && _animator.GetBool(IsAttacking))
        {
            // Odlicza czas trwania ataku (Dla animacji ataku).
            _attackTimer -= Time.deltaTime;

            // Po upływie czasu wyłączony zostaje parametr IsAttacking.
            if (_attackTimer <= 0f)
                _animator.SetBool(IsAttacking, false);
        }

        // Zmniejszanie cooldownu.
        if (_attackCooldownTimer > 0f)
            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
        
        // Bez znalezionych postaci, z cooldownem, lub w trakcie trwającego ataku nie zaatakujemy.
        if (!_attackInput || _attackCooldownTimer > 0f || _animator.GetBool(IsAttacking))
            return;
        
        // Jeżeli przeciwnik jest martwy lub gracz są martwi, nie zaatakujemy. 
        if (_enemyHealth != null && _enemyHealth.IsDead || _playerHealth == null && _playerHealth.IsDead)
            return;
        
        _animator.SetBool(IsAttacking, true);
        _animator.SetTrigger(Attack);

        _attackTimer = _attackDuration;
        _attackCooldownTimer = attackCooldown;

        PerformAttack();
    }

    private void PerformAttack()
    {
        // Jeżeli przeciwnik jest martwy lub nie ma żadnych celów, nie zada obrażeń.
        if (_playerHealth.IsDead && _playerTarget == null)
            return;

        _playerHealth.TakeDamage(damage, transform);
    }

    private void OnDrawGizmosSelected()
    {
        // Rysowanie zasięgu ataku.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector2(attackRange * 2f, attackRange * 2f));
    }
}