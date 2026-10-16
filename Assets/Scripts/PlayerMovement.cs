using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int YVelocity = Animator.StringToHash("YVelocity");
    private static readonly int Jump = Animator.StringToHash("Jump");

    [Header("Movement")] 
    public float speed = 3f;

    [Header("Jump")] 
    public float jumpForce = 3f;
    public int maxJumps = 2;

    [Header("Dash")] 
    public float dashMultiplier = 4f;
    public float dashDuration = 0.1f;
    public float dashCooldown = 1f;
    public float doubleTapTime = 0.25f;

    [HideInInspector] public bool isKnockedBack;
    [HideInInspector] public bool isGrounded;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private int _jumpsRemaining;
    private float _horizontal; // Kierunek ruchu postaci -1 w lewo, 0 bez ruchu, 1 w prawo.
    private float _lastRightPressTime = -1f;
    private float _lastLeftPressTime = -1f;
    private float _nextDashTime;
    private float _dashTimer;
    private float _dashDirection;

    private bool _jumpPressed;
    private bool _isDashing;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        if (_animator == null)
            _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        if (_spriteRenderer == null)
            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        isKnockedBack = false;
        _jumpsRemaining = maxJumps;
    }

    // Co klatkę odczytuje sterowanie, obsługuje dash i skok oraz aktualizuje wygląd postaci.
    private void Update()
    {
        HandleMovement();
        HandleJump();
        ReadMovementInput();
        ReadDashInput();
        ReadJumpInput();
        UpdateDash();
        UpdateFacingDirection();
        UpdateAnimations();
    }
    
    private void ReadMovementInput()
    {
        _horizontal = 0f; // Wartość ta ma być resetowana przy każdym wywołaniu metody.

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            _horizontal = -1f;
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            _horizontal = 1f;
    }

    private void ReadJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            _jumpPressed = true;
    }
    
    private void ReadDashInput()
    {
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            HandleDash(1f, ref _lastRightPressTime);
        
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            HandleDash(-1f, ref _lastLeftPressTime);
    }

    private void HandleMovement()
    {
        // Jeżeli postac jest odrzucana, nie można się poruszać.
        if (isKnockedBack)
            return;

        float currentSpeed = speed;

        // Dash w zasadzie jest chwilowym zwiększeniem prędkości postaci.
        if (_isDashing)
            currentSpeed = speed * dashMultiplier;

        // Blokowanie kierunku, w którym postać dashuje.
        float direction = _isDashing ? _dashDirection : _horizontal;

        // Zachowuje aktualną prędkość pionową, aby nie anulować grawitacji ani skoku.
        _rb.linearVelocity = new Vector2(direction * currentSpeed, _rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (!_jumpPressed)
            return;
        
        if (_jumpsRemaining > 0 && !isKnockedBack)
        {
            // Nadanie postaci prędkości skierowanej do góry.
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

            _jumpsRemaining--;
            isGrounded = false;
            
            // Aktywowanie triggera do animacji skoku.
            if (_jumpsRemaining != 0 && _animator != null)
                    _animator.SetTrigger(Jump);
        }
        _jumpPressed = false;
    }

    private void HandleDash(float direction, ref float lastPressTime)
    {
        float currentTime = Time.time;
        
        bool isDoubleTap = currentTime - lastPressTime <= doubleTapTime;
        bool canDash = currentTime >= _nextDashTime;

        if (isDoubleTap && canDash && !isKnockedBack && !_isDashing)
        {
            _isDashing = true;
            _dashDirection = direction;
            _dashTimer = dashDuration;
            _nextDashTime = currentTime + dashCooldown;

            // Odwraca postać zgodnie z kierunkiem dasha.
            if (_spriteRenderer != null)
                _spriteRenderer.flipX = direction < 0f;
        }
        // Zapamiętuje czas aktualnego naciśnięcia, aby przy następnym sprawdzić, czy to double click.
        lastPressTime = currentTime;
    }
    
    private void UpdateDash()
    {
        // Odlicza pozostały czas dasha.
        _dashTimer -= Time.deltaTime;
        
        // Wyłącza dash.
        if (_dashTimer <= 0f)
        {
            _isDashing = false;
            _dashTimer = 0f;
        }
    }

    private void UpdateFacingDirection()
    {
        // Odwraca postać w prawo lub lewo zgodnie z ruchem, o ile postać jest w ruchu.
        if (_spriteRenderer != null && _horizontal != 0f && !_isDashing)
            _spriteRenderer.flipX = _horizontal < 0f;
    }

    private void UpdateAnimations()
    {
        // Przekazuje animatorowi informację odnośnie prędkości postaci tego, czy jest w powietrzu i prędkości pionowej.
        _animator.SetFloat(Speed, _horizontal);
        _animator.SetBool(IsGrounded, isGrounded);
        _animator.SetFloat(YVelocity, _rb.linearVelocity.y);
    }
    
    // Kolizja postaci z obiektami mającymi tag Ground
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            _jumpsRemaining = maxJumps;
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = false;
    }
}