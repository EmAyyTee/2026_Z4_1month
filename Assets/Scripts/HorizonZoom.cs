using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform player;
    public float followFactor = 0.3f;

    private float _startPlayerY;
    private float _startBackgroundY;

    void Start()
    {
        _startPlayerY = player.position.y;
        _startBackgroundY = transform.position.y;
    }

    void LateUpdate()
    {
        float difference = player.position.y - _startPlayerY;

        float targetY = _startBackgroundY + difference * followFactor;

        transform.position = new Vector3(transform.position.x, targetY, transform.position.z);
    }
}