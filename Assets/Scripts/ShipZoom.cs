using UnityEngine;

public class ShipZoom : MonoBehaviour
{
    public Transform player;

    [Header("Scaling")]
    public float scaleIncrease = 0.1f;
    public float smoothSpeed = 3f;

    [Header("Movement")]
    public float heightFollowFactor = 0.3f;

    private float _startPlayerY;
    private float _startShipY;
    private Vector3 _initialScale;

    void Start()
    {
        _startPlayerY = player.position.y;
        _startShipY = transform.position.y;
        _initialScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (player == null)
            return;

        float heightDifference = player.position.y - _startPlayerY;

        // Skalowanie statku
        float scaleMultiplier = 1f + heightDifference * scaleIncrease;
        scaleMultiplier = Mathf.Max(0.01f, scaleMultiplier);

        Vector3 targetScale = _initialScale * scaleMultiplier;

        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        // Podnoszenie statku razem z graczem
        float targetY = _startShipY + heightDifference * heightFollowFactor;

        float newY = Mathf.Lerp(transform.position.y, targetY, 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime));

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}