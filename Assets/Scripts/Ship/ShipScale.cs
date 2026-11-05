using UnityEngine;

namespace Ship
{
    public class ShipScale : MonoBehaviour
    {
        public Transform player;

        [Header("Scaling")]
        public float scaleIncrease = 0.1f;
        public float smoothSpeed = 3f;

        [Header("Vertical Follow")]
        public float followFactor = 0.3f;

        [Header("Position Correction")]
        public float scaleCorrection = 0.5f;

        private float _startPlayerY;
        private float _startShipY;
        private Vector3 _initialScale;

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
            if (player == null)
                return;

            float difference = player.position.y - _startPlayerY;

            float smooth = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

            // 1. Skalowanie statku
            float scaleMultiplier = 1f + difference * scaleIncrease;
            scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);

            Vector3 targetScale = _initialScale * scaleMultiplier;

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, smooth);

            // 2. Podążanie za graczem - tak jak HorizonZoom
            float targetY = _startShipY + difference * followFactor;

            // 3. Korekta wysokości wynikająca z powiększania
            float scaleDifference = transform.localScale.y - _initialScale.y;

            targetY += scaleDifference * scaleCorrection;

            float newY = Mathf.Lerp(transform.position.y, targetY, smooth);

            // Nie zmieniamy X ani Z
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
}