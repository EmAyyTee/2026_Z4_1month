using UnityEngine;

namespace Ship
{
    public class ShipZoom : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Scaling")]
        public float scaleIncrease = 0.1f;
        public bool active;

        private Vector3 _initialScale;

        private float _startPlayerY;
        private float _startShipY;

        void Start()
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

        void LateUpdate()
        {
            if (active)
                return;

            if (player == null)
                return;

            float difference = player.position.y - _startPlayerY;

            float smooth = 1f - Mathf.Exp(-1f * Time.deltaTime);
            
            float multiplier = Mathf.Max(0.01f, 1f + difference * scaleIncrease);

            Vector3 targetScale = _initialScale * multiplier;

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, smooth);
            
            float targetY = _startShipY + difference * 1.27f;

            float newY = Mathf.Lerp(transform.position.y, targetY, smooth);
            
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}