using UnityEngine;

namespace Background.Clouds
{
    public class SmallCloudMovement : MonoBehaviour
    {
        public float speed = 1f;
        public Camera cam;

        public float leftOffset = 12f;
        public float rightOffset = 12f;

        void Start()
        {
            if (cam == null)
                cam = Camera.main;
        }

        void Update()
        {
            transform.Translate(Vector3.left * (speed * Time.deltaTime));

            float leftLimit = cam.transform.position.x - leftOffset;
            float rightLimit = cam.transform.position.x + rightOffset;

            if (transform.position.x < leftLimit)
                transform.position = new Vector3(rightLimit, transform.position.y, transform.position.z);
        }
    }
}