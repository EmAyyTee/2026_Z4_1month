
using UnityEngine;

namespace Ship
{
    public class ShipZoom : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Scaling")]
        [SerializeField]
        public float scaleIncrease = 0.1f;

        public bool active;

        [SerializeField, Min(0f)]
        private float smoothSpeed = 1f;

        [SerializeField, Min(0.001f)]
        private float minScaleMultiplier = 0.01f;

        [Header("Vertical Movement")]
        [SerializeField]
        private float verticalFollowMultiplier = 1.27f;

        private Vector3 _initialScale;

        private float _startPlayerY;
        private float _startShipY;

        private void Start()
        {
            if (player == null)
            {
                enabled = false;
                return;
            }

            _startPlayerY = player.position.y;
            _startShipY = transform.position.y;

            _initialScale = transform.localScale;
        }

        private void LateUpdate()
        {
            if (active || player == null)
                return;

            float difference =
                player.position.y - _startPlayerY;

            float smooth =
                1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

            float multiplier = Mathf.Max(
                minScaleMultiplier,
                1f + difference * scaleIncrease
            );

            Vector3 targetScale =
                _initialScale * multiplier;

            transform.localScale = Vector3.Lerp(
                transform.localScale,
                targetScale,
                smooth
            );

            float targetY =
                _startShipY +
                difference * verticalFollowMultiplier;

            float newY = Mathf.Lerp(
                transform.position.y,
                targetY,
                smooth
            );

            transform.position = new Vector3(
                transform.position.x,
                newY,
                transform.position.z
            );
        }
    }
}
