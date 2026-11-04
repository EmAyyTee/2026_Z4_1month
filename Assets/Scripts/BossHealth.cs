
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

    [Header("Death")] 
    public float wait;

    private int _currentHealth;
    private bool _isDead;
    private bool _destroyOnDeath = true;
    
    private Animator _animator;
    private Rigidbody2D _rb;

    public bool IsDead => _isDead;
    private ShipMovement _shipMovement;
    private bool isHitting;
    private CannonBallSpawner _cannonBallSpawner;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _shipMovement = GetComponent<ShipMovement>();
        _cannonBallSpawner = FindFirstObjectByType<CannonBallSpawner>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _rb = GetComponent<Rigidbody2D>();

        maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = maxHealth;

        UpdateHealthUI();
    }

    private void Update()
    {
        ReadMovementInput();
    }

    private void ReadMovementInput()
    {
        if (Input.GetKeyDown(KeyCode.Q) && !isHitting)
            StartCoroutine(HitSequence());
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
        if (_cannonBallSpawner != null)
            _cannonBallSpawner.PauseSpawning();
        
        if (isHitting || _shipMovement == null)
            yield break;
        
        isHitting = true;

        if (_shipMovement.wind != null)
            _shipMovement.wind.SetActive(false);

        if (_shipMovement.cannon != null)
            _shipMovement.cannon.SetActive(false);

        if (_shipMovement.cannon2 != null)
            _shipMovement.cannon2.SetActive(false);

        
        if (_animator != null)
            _animator.SetTrigger(Hit);
        
        if (_shipMovement.sailHit != null)
            _shipMovement.sailHit.SetActive(true);
        
        if (_shipMovement.cannonHit != null)
            _shipMovement.cannonHit.SetActive(true);
        
        
        // Czekamy
        yield return new WaitForSeconds(wait);
        
        
        if (_shipMovement.sailHit != null)
            _shipMovement.sailHit.SetActive(false);

        if (_shipMovement.cannonHit != null)
            _shipMovement.cannonHit.SetActive(false);

        if (_shipMovement.wind != null)
            _shipMovement.wind.SetActive(true);
        
        if (_shipMovement.cannon2 != null) 
            _shipMovement.cannon2.SetActive(true);
        
        isHitting = false;
        
        if (_cannonBallSpawner != null)
            _cannonBallSpawner.ResumeSpawning();
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