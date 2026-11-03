
using UnityEngine;
using UnityEngine.UI;

public class BossHealth : MonoBehaviour
{
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dead = Animator.StringToHash("Dead");

    [Header("Health")]
    public int maxHealth = 500;

    [Header("HUD")]
    public Slider healthBar;

    [Header("Death")]
    public float destroyDelay = 1f;

    private int _currentHealth;
    private bool _isDead;
    private bool _destroyOnDeath = true;
    
    private Animator _animator;
    private Rigidbody2D _rb;

    public bool IsDead => _isDead;

    private void Awake()
    {
        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _rb = GetComponent<Rigidbody2D>();

        maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = maxHealth;

        UpdateHealthUI();
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        _animator.SetTrigger(Hit);

        if (_isDead || damage <= 0)
            return;

        _currentHealth = Mathf.Max(0, _currentHealth - damage);

        Debug.Log("BOSS HP: " + _currentHealth + "/" + maxHealth);
        
        UpdateHealthUI();

        if (_currentHealth <= 0)
            Die();
    }

    private void UpdateHealthUI()
    {
        if (healthBar == null)
            return;

        healthBar.minValue = 0;
        healthBar.maxValue = maxHealth;
        healthBar.value = _currentHealth;
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        _currentHealth = 0;
        UpdateHealthUI();

        if (_animator != null)
        {
            _animator.ResetTrigger(Hit);
            _animator.SetTrigger(Dead);
        }

        if (_rb != null)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.simulated = false;
        }

        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();

        foreach (Collider2D col in colliders)
            col.enabled = false;

        if (_destroyOnDeath)
            Destroy(gameObject, destroyDelay);
    }
}