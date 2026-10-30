using UnityEngine;

public class CloudMovement : MonoBehaviour
{
    public float speed = 1f;

    private float _width;

    void Start()
    {
        _width = GetComponent<SpriteRenderer>().bounds.size.x;
    }

    void Update()
    {
        transform.position += Vector3.left * (speed * Time.deltaTime);

        if (transform.position.x <= -_width)
            transform.position += Vector3.right * (_width * 2f);
    }
}