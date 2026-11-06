using UnityEngine;

namespace Background.Clouds
{
    public class SmallCloudMovement : MonoBehaviour
    {
        [Header("Camera")]
        public Camera cam;
        
        [Header("Speed")]
        public float speed = 1f;

        void Start()
        {
            if (cam == null)
                cam = Camera.main;
        }

        void Update()
        {
            transform.Translate(Vector3.left * (speed * Time.deltaTime));

            float leftLimit = cam.transform.position.x - 4.5f;
            float rightLimit = cam.transform.position.x + 4.5f;

            if (transform.position.x < leftLimit)
                transform.position = new Vector3(rightLimit, transform.position.y, transform.position.z);
        }
    }
}