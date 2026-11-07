using UnityEngine;

namespace Enemy
{
    public class EnemyMovement : MonoBehaviour
    {
        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int YVelocity = Animator.StringToHash("YVelocity");


        private Transform _player;
        [SerializeField] private float _stopDistance = 0.8f;
        [SerializeField] private float _detectionRange = 5f;

        [Header("Movement")]
        public float speed = 2f;

        [HideInInspector] public bool isKnockedBack;
        [HideInInspector] public bool isGrounded;

        private Rigidbody2D _rb;
        private Animator _animator;
        private SpriteRenderer _spriteRenderer;

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

        private void Start()
        {
            // Szukamy gracza w uruchomionej scenie, a nie w pliku prefabu.
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                _player = player.transform;
            else
                Debug.LogWarning("Nie znaleziono aktywnego gracza z tagiem Player.", this);
        }

        private void Update()
        {
            FollowPlayer();
            HandleMovement();
            UpdateFacingDirection();
            UpdateAnimations();
        }



        private void FollowPlayer()
        {
            _horizontal = 0f;

            if (_player == null)
                return;

            float distance = Vector2.Distance(
                transform.position,
                _player.position
            );

            if (distance > _detectionRange)
                return;

            float distanceX = _player.position.x - transform.position.x;

            if (Mathf.Abs(distanceX) > _stopDistance)
                _horizontal = Mathf.Sign(distanceX);
        }



        private void HandleMovement()
        {
            // Jeżeli postac jest odrzucana, nie nadpisuje jej ruchu.
            if (isKnockedBack)
                return;

            // Zachowuje aktualną prędkość pionową, aby nie anulować grawitacji ani skoku.
            _rb.linearVelocity = new Vector2(_horizontal * speed, _rb.linearVelocity.y);
        }



        private void UpdateFacingDirection()
        {
            // Odwraca postać w prawo lub lewo zgodnie z ruchem, o ile postać jest w ruchu.
            if (_spriteRenderer != null && _horizontal != 0f)
                _spriteRenderer.flipX = _horizontal > 0f;
        }

        private void UpdateAnimations()
        {
            _animator.SetFloat(Speed, Mathf.Abs(_horizontal));
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
}
