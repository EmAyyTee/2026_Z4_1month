using System.Collections.Generic;
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
        private readonly Dictionary<Collider2D, MovingPlatform> _supports = new Dictionary<Collider2D, MovingPlatform>();

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
            RefreshSupportState();
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
            if (!Input.GetKeyDown(KeyCode.LeftShift))
                return;

            if (_horizontal == 0f)
                return;

            if (Time.time < _nextDashTime || isKnockedBack || _isDashing)
                return;

            _isDashing = true;
            _dashDirection = _horizontal;
            _dashTimer = dashDuration;
            _nextDashTime = Time.time + dashCooldown;

            if (_spriteRenderer != null)
                _spriteRenderer.flipX = _dashDirection < 0f;
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
            if (standingOnPlatform)
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
                {
                    // Przejmujemy prędkość platformy tylko wtedy, gdy porusza się w górę.
                    platformVelocityY = Mathf.Max(
                        0f,
                        _movingPlatform.CurrentVelocity.y
                    );
                }

                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce + platformVelocityY);
            
                _jumpsRemaining--;

                isGrounded = false;
                onSomething = false;

                _movingPlatform = null;
                _supports.Clear();

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

        private void TrackSupport(Collision2D collision)
        {
            Collider2D other = collision.collider;

            if (other == null)
                return;

            _supports.Remove(other);

            MovingPlatform platform =
                other.GetComponentInParent<MovingPlatform>();

            bool walkable =
                HasTagInParents(other.transform, "Ground") ||
                HasTagInParents(other.transform, "Box") ||
                HasTagInParents(other.transform, "Platform") ||
                platform != null;

            if (!walkable)
            {
                RefreshSupportState();
                return;
            }

            bool standingOnTop = false;

            for (int i = 0; i < collision.contactCount; i++)
            {
                if (collision.GetContact(i).normal.y > 0.5f)
                {
                    standingOnTop = true;
                    break;
                }
            }

            float supportVelocityY = 0f;

            if (platform != null)
            {
                supportVelocityY = platform.CurrentVelocity.y;
            }
            else if (collision.rigidbody != null)
            {
                supportVelocityY = collision.rigidbody.linearVelocity.y;
            }

            // Nie rejestrujemy podłoża, jeśli gracz właśnie od niego odskakuje.
            bool movingAwayUpwards =
                _rb.linearVelocity.y > supportVelocityY + 0.1f;

            if (standingOnTop && !movingAwayUpwards)
            {
                _supports[other] = platform;
                _jumpsRemaining = maxJumps;
            }

            RefreshSupportState();
        }

        private void RefreshSupportState()
        {
            isGrounded = false;
            onSomething = false;
            _movingPlatform = null;

            foreach (var support in _supports)
            {
                Collider2D collider = support.Key;

                if (collider == null ||
                    !collider.enabled ||
                    !collider.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Transform supportTransform = collider.transform;

                bool ground = HasTagInParents(supportTransform, "Ground");
                bool box = HasTagInParents(supportTransform, "Box");
                bool platformTag = HasTagInParents(supportTransform, "Platform");

                isGrounded |= ground || support.Value != null;

                onSomething |= box || platformTag;

                if (support.Value != null)
                    _movingPlatform = support.Value;
            }

            if (_movingPlatform != null)
                isGrounded = true;
        }
        
        private bool HasTagInParents(Transform current, string tag)
        {
            while (current != null)
            {
                if (current.CompareTag(tag))
                    return true;

                current = current.parent;
            }

            return false;
        }

        public bool IsTouchingGround()
        {
            foreach (var support in _supports.Keys)
                if (support != null && support.enabled && support.gameObject.activeInHierarchy && support.CompareTag("Ground"))
                    return true;
            return false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TrackSupport(collision);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            TrackSupport(collision);
        }

        private void OnCollisionExit2D(Collision2D collision)
        {
            _supports.Remove(collision.collider);
            RefreshSupportState();
        }
    }
}
