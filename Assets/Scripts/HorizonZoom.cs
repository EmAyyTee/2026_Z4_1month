using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
    public Transform player;
    public float followFactor = 0.3f;

    private float startPlayerY;
    private float startBackgroundY;

    void Start()
    {
        startPlayerY = player.position.y;
        startBackgroundY = transform.position.y;
    }

    void LateUpdate()
    {
        float difference = player.position.y - startPlayerY;

        float targetY = startBackgroundY + difference * followFactor;

        transform.position = new Vector3(
            transform.position.x,
            targetY,
            transform.position.z
        );
    }
}