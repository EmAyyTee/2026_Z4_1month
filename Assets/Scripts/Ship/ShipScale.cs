using UnityEngine;

namespace Ship
{
    public class ShipScale : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Fixed Position")]
        public float fixedX = 8f;

        private float _startPlayerY;
        private float _startShipY;

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

            enabled = true;
        }

        void LateUpdate()
        {
            if (player == null)
                return;

            float difference = player.position.y - _startPlayerY;

            float smooth = 1f - Mathf.Exp(-100 * Time.deltaTime);

            float targetY;
            
            if (player.position.y >= 10f)
                targetY = _startShipY + difference * 1;
            else
                targetY = player.position.y;

            float newY = Mathf.Lerp(transform.position.y, targetY, smooth);

            transform.position = new Vector3(fixedX, newY, transform.position.z);
        }
    }
}