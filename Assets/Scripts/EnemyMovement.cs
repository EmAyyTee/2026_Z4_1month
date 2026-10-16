using UnityEngine;

public class EnemyMovement : MonoBehaviour
{
    private static readonly int Speed = Animator.StringToHash("Speed");
    private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int YVelocity = Animator.StringToHash("YVelocity");
    private static readonly int Jump = Animator.StringToHash("Jump");

    [Header("Movement")] 
    public float speed = 2f;

    [Header("Jump")] 
    public float jumpForce = 2f;

    [HideInInspector] public bool isKnockedBack;
    [HideInInspector] public bool isGrounded;

    private Rigidbody2D _rb;
    private Animator _animator;
    private SpriteRenderer _spriteRenderer;

    private bool _jumpPressed;
    private float _horizontal; // Kierunek ruchu postaci -1 w lewo, 0 bez ruchu, 1 w prawo.

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();

        _animator = GetComponent<Animator>();

        if (_animator == null)
            _animator = GetComponentInChildren<Animator>();

        _spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        isKnockedBack = false;
    }

    private void Update()
    {
        HandleMovement();
        HandleJump();
        ReadMovementInput();
        ReadJumpInput();
        UpdateFacingDirection();
        UpdateAnimations();
    }

    // Dla testu dodałem możliwość poruszania się przeciwnika przez input.

    private void ReadMovementInput()
    {
        _horizontal = 0f; // Wartość ta ma być resetowana przy każdym wywołaniu metody.

        if (Input.GetKey(KeyCode.J))
            _horizontal = -1f;
        else if (Input.GetKey(KeyCode.L))
            _horizontal = 1f;
    }

    private void ReadJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.I))
            _jumpPressed = true;
    }

    private void HandleMovement()
    {
        // Jeżeli postac jest odrzucana, nie nadpisuje jej ruchu.
        if (isKnockedBack)
            return;
        
        // Zachowuje aktualną prędkość pionową, aby nie anulować grawitacji ani skoku.
        _rb.linearVelocity = new Vector2(_horizontal * speed, _rb.linearVelocity.y);
    }

    private void HandleJump()
    {
        if (!_jumpPressed)
            return;

        if (!isKnockedBack && isGrounded)
        {
            // Nadanie postaci prędkości skierowanej do góry.
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

            isGrounded = false;

            // Aktywuje trigger od animacji skoku.
            if (_animator != null)
                _animator.SetTrigger(Jump);
        }
        _jumpPressed = false;
    }
    
    private void UpdateFacingDirection()
    {
        // Odwraca postać w prawo lub lewo zgodnie z ruchem, o ile postać jest w ruchu.
        if (_spriteRenderer != null && _horizontal != 0f)
            _spriteRenderer.flipX = _horizontal > 0f;
    }

    private void UpdateAnimations()
    {
        _animator.SetFloat(Speed, _horizontal);
        _animator.SetBool(IsGrounded, isGrounded);
        _animator.SetFloat(YVelocity, _rb.linearVelocity.y);
    }
    
    // Kolizja postaci z obiektami mającymi tag Ground
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
            isGrounded = true;
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