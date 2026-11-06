using UnityEngine;

public class ShipScale : MonoBehaviour
{
    public Transform player;

    [Header("Scaling")]
    public float scaleIncrease = 0.1f;
    public float smoothSpeed = 3f;

    [Header("Movement")]
    public float heightFollowFactor = 0.3f;

    [Header("Fixed World Position")]
    public float fixedWorldX = 8f;

    private float _startPlayerY;
    private float _startShipY;
    private Vector3 _initialScale;

    void Start()
    {
        enabled = false;
    }

    public void Activate()
    {
        if (player == null)
        {
            Debug.LogError("[ShipScale] Player not assigned!", this);
            return;
        }

        _startPlayerY = player.position.y;
        _startShipY = transform.position.y;
        _initialScale = transform.localScale;

        enabled = true;

        Debug.Log("[ShipScale] Activated!", this);
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        float difference = player.position.y - _startPlayerY;

        float smooth = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);

        // SCALE
        float multiplier = Mathf.Max(
            0.01f,
            1f + difference * scaleIncrease
        );

        Vector3 targetScale = _initialScale * multiplier;

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            smooth
        );

        // Y FOLLOW
        float targetY = _startShipY + difference * heightFollowFactor;

        float newY = Mathf.Lerp(
            transform.position.y,
            targetY,
            smooth
        );

        // ABSOLUTE WORLD POSITION
        transform.position = new Vector3(
            fixedWorldX,
            newY,
            transform.position.z
        );
    }
}