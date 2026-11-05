using UnityEngine;

namespace Background
{
    public class HorizonZoom : MonoBehaviour
    {
        public Transform player;
        public float followFactor = 0.3f;

        [Header("Background Height Limits")]
        public float maxBackgroundY = 10f;
        public float minBackgroundY = -10f;

        private float _startPlayerY;
        private float _startBackgroundY;

        void Start()
        {
            _startPlayerY = player.position.y;
            _startBackgroundY = transform.position.y;
        }

        void LateUpdate()
        {
            if (player == null)
                return;

            float difference = player.position.y - _startPlayerY;

            float targetY = _startBackgroundY + difference * followFactor;

            // Ograniczenie wysokości tła
            targetY = Mathf.Clamp(targetY, minBackgroundY, maxBackgroundY);

            transform.position = new Vector3(
                transform.position.x,
                targetY,
                transform.position.z
            );
        }
    }
}