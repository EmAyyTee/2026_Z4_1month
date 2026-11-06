using UnityEngine;

namespace Background
{
    public class HorizonZoom : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

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

            float targetY = _startBackgroundY + difference * 1.05f;

            // Ograniczenie wysokości tła
            targetY = Mathf.Clamp(targetY, 0f, 15f);

            transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
        }
    }
}