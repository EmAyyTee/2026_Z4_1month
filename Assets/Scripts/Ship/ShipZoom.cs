
using UnityEngine;

namespace Ship
{
    public class ShipZoom : MonoBehaviour
    {
        public Transform player;

        [Header("Scaling")]
        public float scaleIncrease = 0.1f;
        public float smoothSpeed = 3f;

        [Header("Movement")]
        public float heightFollowFactor = 0.3f;
        public float fixedX = 8f;

        [Header("Scale Position Correction")]
        public float scaleHeightCorrection = 0.5f;

        private float _startPlayerY;
        private float _startShipY;
        private Vector3 _initialScale;

        private bool _active;
        private float _debugTimer;

        void Start()
        {
            enabled = false;
        }

        public void Activate()
        {
            if (player == null)
                return;
        
            _startPlayerY = player.position.y;
            _startShipY = transform.position.y;
            _initialScale = transform.localScale;

            _active = true;
            enabled = true;
        }

        void LateUpdate()
        {
            if (!_active || player == null)
                return;

            float heightDifference = player.position.y - _startPlayerY;

            // Skalowanie statku
            float scaleMultiplier = 1f + heightDifference * scaleIncrease;
            scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);

            Vector3 targetScale = _initialScale * scaleMultiplier;

            float smooth = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

            transform.localScale = Vector3.Lerp(transform.localScale, targetScale, smooth);

            // Obliczanie wysokości statku
            float targetY = _startShipY + heightDifference * heightFollowFactor;

            // Korekta pozycji przy powiększaniu
            float scaleDifference = transform.localScale.y - _initialScale.y;

            targetY += scaleDifference * scaleHeightCorrection;

            float newY = Mathf.Lerp(transform.position.y, targetY, smooth);

            // Ustawienie pozycji
            transform.position = new Vector3(fixedX, newY, transform.position.z);
        }
    }
}
