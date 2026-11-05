using Moving_Platform;
using UnityEngine;

namespace Player
{
    public class PlayerMovement : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int OnSomething = Animator.StringToHash("OnSomething");
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
        [HideInInspector] public bool onSomething;

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;
        private MovingPlatform _movingPlatform;

        private int _jumpsRemaining;

        private float _horizontal;
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

            _animator = GetComponent<Animator>();

            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();

            _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            isKnockedBack = false;
            _jumpsRemaining = maxJumps;
        }

        private void Update()
        {
            ReadMovementInput();
            ReadDashInput();
            ReadJumpInput();

            UpdateDash();
            UpdateFacingDirection();
            UpdateAnimations();
        }

        private void FixedUpdate()
        {
            HandleMovement();
            HandleJump();
        }

        private void ReadMovementInput()
        {
            _horizontal = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                _horizontal = -1f;

            else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                _horizontal = 1f;
        }

        private void ReadJumpInput()
        {
            if (Input.GetKeyDown(KeyCode.W) ||
                Input.GetKeyDown(KeyCode.UpArrow) ||
                Input.GetKeyDown(KeyCode.Space))
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
            if (isKnockedBack)
                return;

            float currentSpeed = _isDashing ? speed * dashMultiplier : speed;

            float direction = _isDashing ? _dashDirection : _horizontal;

            Vector2 platformVelocity = Vector2.zero;

            bool standingOnPlatform = _movingPlatform != null && (isGrounded || onSomething);

            if (standingOnPlatform)
                platformVelocity = _movingPlatform.CurrentVelocity;

            float velocityX = direction * currentSpeed + platformVelocity.x;

            float velocityY = _rb.linearVelocity.y;

            // Przenoszenie gracza razem z platformą.
            // Nie nadpisujemy prędkości pionowej, jeśli gracz skacze.
            if (standingOnPlatform && _rb.linearVelocity.y <= 0f)
                velocityY = platformVelocity.y;
        
            _rb.linearVelocity = new Vector2(velocityX, velocityY);
        }

        private void HandleJump()
        {
            if (!_jumpPressed)
                return;

            if (_jumpsRemaining > 0 && !isKnockedBack)
            {
                float platformVelocityY = 0f;

                if (_movingPlatform != null)
                    platformVelocityY = _movingPlatform.CurrentVelocity.y;

                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce + platformVelocityY);
            
                _jumpsRemaining--;

                isGrounded = false;
                onSomething = false;

                _movingPlatform = null;

                if (_animator != null)
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

                if (_spriteRenderer != null)
                    _spriteRenderer.flipX = direction < 0f;
            }

            lastPressTime = currentTime;
        }

        private void UpdateDash()
        {
            if (!_isDashing)
                return;

            _dashTimer -= Time.deltaTime;

            if (_dashTimer <= 0f)
            {
                _isDashing = false;
                _dashTimer = 0f;
            }
        }

        private void UpdateFacingDirection()
        {
            if (_spriteRenderer != null && _horizontal != 0f && !_isDashing)
                _spriteRenderer.flipX = _horizontal < 0f;
        }

        private void UpdateAnimations()
        {
            if (_animator == null)
                return;

            _animator.SetFloat(Speed, Mathf.Abs(_horizontal));
            _animator.SetBool(IsGrounded, isGrounded || onSomething);
            _animator.SetBool(OnSomething, onSomething);
            _animator.SetFloat(YVelocity, _rb.linearVelocity.y);
        }

        // Wykrywanie platformy, na której stoi gracz.
        private void CheckMovingPlatform(Collision2D collision)
        {
            if (!collision.gameObject.CompareTag("Platform"))
                return;

            for (int i = 0; i < collision.contactCount; i++)
            {
                ContactPoint2D contact = collision.GetContact(i);

                // Normalna skierowana do góry oznacza,
                // że gracz stoi na platformie.
                if (contact.normal.y > 0.5f)
                {
                    _movingPlatform = collision.gameObject.GetComponent<MovingPlatform>();

                    if (_movingPlatform != null)
                    {
                        onSomething = true;
                        isGrounded = true;
                        _jumpsRemaining = maxJumps;
                    }
                    return;
                }
            }
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            CheckMovingPlatform(collision);

            if (collision.gameObject.CompareTag("Ground"))
            {
                isGrounded = true;
                _jumpsRemaining = maxJumps;
            }

            if (collision.gameObject.CompareTag("Box") || collision.gameObject.CompareTag("Platform"))
            {
                onSomething = true;
                _jumpsRemaining = maxJumps;
            }
        }
    
    
        public bool IsTouchingGround()
        {
            Collider2D playerCollider = GetComponent<Collider2D>();

            if (playerCollider == null)
                return false;

            ContactFilter2D filter = new ContactFilter2D();
            filter.useTriggers = false;

            Collider2D[] results = new Collider2D[10];

            int count = playerCollider.Overlap(filter, results);

            for (int i = 0; i < count; i++)
                if (results[i] != null && results[i].CompareTag("Ground"))
                    return true;
        
            return false;
        }
    
        private void OnCollisionStay2D(Collision2D collision)
        {
            CheckMovingPlatform(collision);

            if (collision.gameObject.CompareTag("Ground"))
                isGrounded = true;

            if (collision.gameObject.CompareTag("Box"))
                onSomething = true;
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
                isGrounded = false;

            if (collision.gameObject.CompareTag("Box"))
                onSomething = false;

            if (collision.gameObject.CompareTag("Platform"))
            {
                MovingPlatform platform = collision.gameObject.GetComponent<MovingPlatform>();

                if (_movingPlatform == platform)
                {
                    _movingPlatform = null;
                    onSomething = false;
                }
            }
        }
    }
}