using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    private static readonly int Hit = Animator.StringToHash("Hit");
    private static readonly int Dead = Animator.StringToHash("Dead");
    private static readonly int DeadGround = Animator.StringToHash("DeadGround");

    [Header("Health")] 
    public int maxHealth = 100;

    [Header("Knockback")] 
    public float knockbackForce = 2f;
    public float knockbackUpward = 1f;
    public float knockbackDuration = 0.2f;

    [Header("Fall Damage")] 
    public float minimumFallHeight = 1f;
    public float fallDamagePerUnit = 10f;
    
    [Header("HUD")] 
    public Slider healthBar;
    public GameObject deathScreen;

    [Header("Health Regeneration")] 
    public float regenDelay = 5f;
    public float regenInterval = 0.1f;
    public int regenAmount = 1;

    [Header("Damage Immunity")] 
    public float invulnerabilityDuration = 1f;

    private Rigidbody2D _rb;
    private Animator _animator;
    private PlayerMovement _playerMovement;
    private Coroutine _knockbackCoroutine;

    private int _currentHealth;
    private bool _isDead;

    private float _fallStartY;
    private bool _isFalling;

    private float _lastDamageTime;
    private float _nextRegenTime;
    private float _invulnerabilityTimer;
    private bool IsInvulnerable => _invulnerabilityTimer > 0f;

    public bool IsDead => _isDead;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();
        
        _playerMovement = GetComponent<PlayerMovement>();
        
        maxHealth = Mathf.Max(1, maxHealth);
        _currentHealth = maxHealth;
        _fallStartY = transform.position.y;
        
        UpdateHealthUI();
    }
    
    private void Start()
    {
        // Początek odliczania czasu.
        _lastDamageTime = Time.time;
        _nextRegenTime = Time.time + regenInterval;
        
        // Ukrycie ekranu śmierci na początku gry.
        if (deathScreen != null)
            deathScreen.SetActive(false);
    }
    
    // Co klatkę odświeża nieśmiertelność oraz regenerację.
    private void Update()
    {
        HandleFallDamage();
        UpdateInvulnerability();
        UpdateHealthRegeneration();
    }

    public void TakeDamage(int damage, Transform attacker)
    {
        // Martwa lub nieśmiertelna postać nie otrzymuje dodatkowych obrażeń oraz obrażeń ujemnych.
        if (_isDead || IsInvulnerable || damage <= 0)
            return;

        // Każde przyjęte obrażenie resetuje zegar.
        _lastDamageTime = Time.time;
        _invulnerabilityTimer = invulnerabilityDuration;

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
        if (!_playerMovement.isGrounded && !_isFalling && _rb.linearVelocity.y < 0f)
        {
            _fallStartY = transform.position.y;
            _isFalling = true;
            return;
        }
        
        // Jeżeli postać przestała spadać nie wykonuje dalszej części kodu.
        if (!_playerMovement.isGrounded || !_isFalling)
            return;

        float fallHeight = _fallStartY - transform.position.y;
        _isFalling = false;

        if (fallHeight < minimumFallHeight)
            return;

        // Przeliczenie obrażeń od upadku.
        int damage = Mathf.CeilToInt((fallHeight - minimumFallHeight) * fallDamagePerUnit);

        if (damage <= 0)
            return;
        
        // Jeżeli obrażenia są większe niż aktualne życie mamy śmierć od upadku.
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
        if (_playerMovement != null)
            _playerMovement.isKnockedBack = true;
        
        _knockbackCoroutine = StartCoroutine(ResetKnockback());
    }

    private IEnumerator ResetKnockback()
    {
        yield return new WaitForSeconds(knockbackDuration);
        
        if (_rb != null && !_isDead)
            // Blokada ruchu poziomego.
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);

        // Odblokowanie możliwości normalnego poruszania się.
        if (_playerMovement != null && !_isDead)
            _playerMovement.isKnockedBack = false;

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
        if (_playerMovement != null)
        {
            _playerMovement.isKnockedBack = false;
            _playerMovement.enabled = false;
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

            // Wybór animacji śmierci w zależności od przyczyny.
            _animator.SetTrigger(fromFall ? DeadGround : Dead);

            // Wymuszenie natychmiastowej aktualizacji Animatora.
            _animator.Update(0f);
        }

        if (deathScreen != null)
            deathScreen.SetActive(true);
    }

    private void UpdateHealthRegeneration()
    {
        // Regeneracja zaczyna się po upływie określonego czasu.
        if (Time.time < _lastDamageTime + regenDelay)
            return;

        // Intensywność regeneracji.
        if (Time.time < _nextRegenTime)
            return;

        // Blokada regeneracji życia w nieskończoność.
        _currentHealth = Mathf.Min(_currentHealth + regenAmount, maxHealth);

        UpdateHealthUI();
        _nextRegenTime = Time.time + regenInterval;
    }

    private void UpdateInvulnerability()
    {
        // Odliczanie czasu odporności.
        if (_invulnerabilityTimer <= 0f)
            return;

        _invulnerabilityTimer = Mathf.Max(0f, _invulnerabilityTimer - Time.deltaTime);
    }

    private void UpdateHealthUI()
    {
        if (healthBar == null)
            return;

        healthBar.minValue = 0;
        healthBar.maxValue = maxHealth;
        healthBar.value = _currentHealth;
    }
}