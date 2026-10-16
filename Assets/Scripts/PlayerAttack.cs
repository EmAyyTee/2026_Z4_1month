using UnityEngine;

public class PlayerAttack : MonoBehaviour
{
    private static readonly int IsAttacking = Animator.StringToHash("IsAttacking");
    private static readonly int AttackID = Animator.StringToHash("AttackID");
    private static readonly int Attack = Animator.StringToHash("Attack");
    private static readonly int AirAttackID = Animator.StringToHash("AirAttackID");
    private static readonly int AirAttack = Animator.StringToHash("AirAttack");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");

    [Header("Attack")] 
    public float attackRange = 0.2f;
    public float attackCooldown = 1f;
    public int damage = 10;

    private readonly float _attackDuration = 0.35f;
    private readonly Collider2D[] _hits = new Collider2D[10]; // Przechowuje collidery wykryte podczas sprawdzania trafień.

    private Animator _animator;
    private PlayerHealth _playerHealth;
    private EnemyHealth _enemyHealth;
    private Transform _enemyTarget;

    private bool _attackInput;
    private float _attackTimer;
    private float _attackCooldownTimer;
    
    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _playerHealth = GetComponent<PlayerHealth>();
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
        if (Input.GetMouseButtonDown(0))
            _attackInput = true;
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

        // Bez kliknięcia, z cooldownem, lub w trakcie trwającego ataku nie zaatakujemy.
        if (!_attackInput || _attackCooldownTimer > 0f || _animator.GetBool(IsAttacking))
        {
            // Jeden klik, jeden atak.
            _attackInput = false;
            return;
        }
        // Jeżeli gracz jest martwy, nie zaatakujemy. 
        if (_playerHealth == null && _playerHealth.IsDead)
            return;

        bool isGrounded = _animator.GetBool(IsGrounded);
        
        // Losowy wybór ataku lub powietrznego ataku.
        int attackIndex = isGrounded ? Random.Range(0, 3) : Random.Range(0, 2);

        _animator.SetFloat(isGrounded ? AttackID : AirAttackID, attackIndex);
        _animator.SetBool(IsAttacking, true);
        _animator.SetTrigger(isGrounded ? Attack : AirAttack);

        _attackTimer = _attackDuration;
        _attackCooldownTimer = attackCooldown;
        
        PerformAttack();
    }

    private void PerformAttack()
    {
        // Jeżeli przeciwnik jest martwy lub nie ma żadnych celów, nie zada obrażeń.
        if (_playerHealth != null && _playerHealth.IsDead && _enemyTarget == null)
            return;
        
        // Określa, ilu przeciwników jest w zasięgu ataku postaci.
        int hitCount = Physics2D.OverlapCircle(transform.position, attackRange, new ContactFilter2D { useTriggers = true }, _hits);

        // Przejście po wszystkich wykrytych przeciwnikach.
        for (int i = 0; i < hitCount; i++)
        {
            Collider2D hit = _hits[i];

            if (hit == null || !hit.CompareTag("Enemy"))
                continue;

            // Szuka EnemyHealth na trafionym obiekcie
            _enemyHealth = hit.GetComponentInParent<EnemyHealth>();

            // Zapisanie do transform.
            _enemyTarget = _enemyHealth != null ? _enemyHealth.transform : null;

            // Jeżeli nie ma systemu zdrowia to koniec.
            if (_enemyTarget == null || _enemyHealth == null)
                return;
            
            // Pomija martwe postaci.
            if (_enemyHealth == null || _enemyHealth.IsDead)
                continue;

            _enemyHealth.TakeDamage(damage, transform);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Rysowanie zasięgu ataku.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector2(attackRange * 2f, attackRange * 2f));
    }
}