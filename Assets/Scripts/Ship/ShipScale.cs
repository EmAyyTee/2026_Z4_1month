using UnityEngine;

namespace Ship
{
    public class ShipScale : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Fixed Position")]
        public float fixedX = 8f;

        [Header("Vertical Movement")]
        [SerializeField]
        private float playerHeightThreshold = 10f;

        [SerializeField, Min(0f)]
        private float positionSmoothness = 100f;

        [SerializeField]
        private float verticalFollowMultiplier = 1f;

        private float _startPlayerY;
        private float _startShipY;

        private void Start()
        {
            enabled = false;
        }

        public void Activate()
        {
            if (player == null)
                return;

            _startPlayerY = player.position.y;
            _startShipY = transform.position.y;

            enabled = true;
        }

        private void LateUpdate()
        {
            if (player == null)
                return;

            float difference =
                player.position.y - _startPlayerY;

            float smooth =
                1f - Mathf.Exp(-positionSmoothness * Time.deltaTime);

            float targetY;

            if (player.position.y >= playerHeightThreshold)
            {
                targetY =
                    _startShipY +
                    difference * verticalFollowMultiplier;
            }
            else
            {
                targetY = player.position.y;
            }

            float newY = Mathf.Lerp(
                transform.position.y,
                targetY,
                smooth
            );

            transform.position = new Vector3(
                fixedX,
                newY,
                transform.position.z
            );
        }
    }
}