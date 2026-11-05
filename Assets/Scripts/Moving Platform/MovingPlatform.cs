using UnityEngine;

namespace Moving_Platform
{
    [RequireComponent(typeof(Rigidbody2D))]
    public class MovingPlatform : MonoBehaviour
    {
        public enum MoveDirection
        {
            Horizontal,
            Vertical
        }

        [Header("Movement")]
        public MoveDirection direction = MoveDirection.Vertical;
        public float distance = 3f;
        public float speed = 2f;

        public Vector2 CurrentVelocity { get; private set; }

        private Rigidbody2D _rb;
        private Vector2 _startPosition;
        private Vector2 _previousPosition;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _rb.bodyType = RigidbodyType2D.Kinematic;
            _rb.simulated = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }

        private void Start()
        {
            _startPosition = _rb.position;
            _previousPosition = _rb.position;
        }

        private void FixedUpdate()
        {
            Vector2 axis = direction == MoveDirection.Vertical ? Vector2.up : Vector2.right;

            float offset = Mathf.PingPong(Time.fixedTime * speed, distance * 2f) - distance;

            Vector2 target = _startPosition + axis * offset;

            _rb.MovePosition(target);

            CurrentVelocity = (target - _previousPosition) / Time.fixedDeltaTime;

            _previousPosition = target;
        }
    }
}