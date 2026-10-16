using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EnemyHealth : MonoBehaviour
{
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int DeadGround = Animator.StringToHash("DeadGround");

    [Header("Health")] 
    public int maxHealth = 30;

    [Header("Knockback")] 
    public float knockbackForce = 2f;
    public float knockbackUpward = 1f;
    public float knockbackDuration = 0.2f;

    [Header("Fall Damage")] 
    public float minimumFallHeight = 1f;
    public float fallDamagePerUnit = 10f;
    
    [Header("HUD")] 
    public Slider healthBar;

    private Rigidbody2D _rb;
    private Animator _animator;
    private EnemyMovement _enemyMovement;
    private Coroutine _knockbackCoroutine;

    private int _currentHealth;
    private bool _isDead;

    private float _fallStartY;
    private bool _isFalling;
    
    public bool IsDead => _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        
        _enemyMovement = GetComponent<EnemyMovement>();

        maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = maxHealth;
        _fallStartY = transform.position.y;

        UpdateHealthUI();
    }

    // Co klatkę odświeża obrażenia z upadku.
    private void Update()
    {
        HandleFallDamage();
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        // Martwa postać nie otrzymuje dodatkowych obrażeń oraz obrażeń ujemnych.
        if (_isDead || damage <= 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);
        UpdateHealthUI();

        // Animacja śmierci ma pierwszeństwo przed każdą inną animacją.
        if (_currentHealth <= 0)
        {
            Die(false);
            return;
        }

        _animator.SetTrigger(Hit);
        ApplyKnockback(attacker);
    }

    private void HandleFallDamage()
    {
        // Początek naliczania wysokości.
        if (!_enemyMovement.isGrounded && !_isFalling && _rb.linearVelocity.y < 0f)
        {
            _fallStartY = transform.position.y;
            _isFalling = true;
            return;
        }
        
        // Jeżeli postać przestała spadać nie wykonuje dalszej części kodu.
        if (!_enemyMovement.isGrounded || !_isFalling)
            return;

        float fallHeight = _fallStartY - transform.position.y;
        _isFalling = false;

        if (fallHeight < minimumFallHeight)
            return;
        
        // Przeliczenie obrażeń od upadku.
        int damage = Mathf.CeilToInt((fallHeight - minimumFallHeight) * fallDamagePerUnit);

        if (damage <= 0)
            return;
        
        // Jeżeli obrażenia są większe, niż aktualne życie mamy śmierć od upadku.
        if (damage >= _currentHealth)
        {
            Die(true);
            return;
        }

        TakeDamage(damage, null);
    }
    
    private void ApplyKnockback(Transform attacker)
    {
        // Postać zostaje odrzucona w przeciwną stronę od strony atakującej.
        float direction = Mathf.Sign(transform.position.x - attacker.position.x);
        
        // Odepchnięcie postaci.
        _rb.linearVelocity = new Vector2(direction * knockbackForce, knockbackUpward);

        // Blokada ruchu podczas odrzutu.
        if (_enemyMovement != null)
            _enemyMovement.isKnockedBack = true;

        _knockbackCoroutine = StartCoroutine(ResetKnockback());
    }

    private IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(knockbackDuration);

        if (_rb != null && !_isDead)
            // Blokada ruchu poziomego.
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
        
        // Odblokowanie możliwości normalnego poruszania się.
        if (_enemyMovement != null && !_isDead)
            _enemyMovement.isKnockedBack = false;

        _knockbackCoroutine = null;
    }

    private void Die(bool fromFall)
    {
        _isDead = true;
        _currentHealth = 0;
        UpdateHealthUI();

        // Przerwanie odrzutu.
        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
        }

        // Blokada ruchu.
        if (_enemyMovement != null)
        {
            _enemyMovement.isKnockedBack = false;
            _enemyMovement.enabled = false;
        }

        // Wyłączenie kolizji po śmierci.
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
            col.enabled = false;

        // Zatrzymanie symulacji fizyki.
        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        if (_animator != null)
        {
            // Usunięcie oczekujących triggerów.
            _animator.ResetTrigger(Hit);
            _animator.ResetTrigger(Dead);
            _animator.ResetTrigger(DeadGround);

            _animator.SetTrigger(fromFall ? DeadGround : Dead);

            _animator.Update(0f);
        }
    }
    
    private void UpdateHealthUI()
    {
        healthBar.maxValue = maxHealth;
        healthBar.value = _currentHealth;
    }
}