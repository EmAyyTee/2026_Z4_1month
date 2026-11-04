
using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;

public class ShipMovement : MonoBehaviour
{
    public Transform background;

    [Header("Movement")]
    public float moveDistance = 5f;
    public float moveSpeed = 5f;
    
    [Header("Final Position")]
    public float targetX = 8f;

    [Header("Visuals")]
    public GameObject toWind;
    public GameObject wind;
    public GameObject idle;
    public GameObject health;
    public GameObject wall;
    [FormerlySerializedAs("wall_1")] public GameObject wall1;
    public GameObject cannon; // wystrzał
    [FormerlySerializedAs("cannon_1")] public GameObject cannon1; // statyczny cannon
    [FormerlySerializedAs("cannon_2")] public GameObject cannon2; // Przerwa pomiędzy wystrzałami
    public GameObject cannonHit;
    public GameObject sailHit; 
    
    private SpriteRenderer[] _spriteRenderers;
    private bool _isMoving;
    private bool _online = true;
    private bool _shipLocked;
    public Transform player;

    private Vector3 _lastPlayerPosition;
    
    [Header("Ship Offset")]
    public float verticalOffset = -2f;
    
    [SerializeField] private CannonBallSpawner cannonBallSpawner;

    void Start()
    {
        if (player != null)
            _lastPlayerPosition = player.position;
        
        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        if (toWind != null) toWind.SetActive(false);
        if (wind != null) wind.SetActive(false);
        if (idle != null) idle.SetActive(true);
        if (health != null) health.SetActive(false);
        if (wall != null) wall.SetActive(false);
        if (wall1 != null) wall1.SetActive(false);
        if (cannon1 != null) cannon1.SetActive(true);
        if (cannon2 != null) cannon2.SetActive(false);
        if (cannonHit != null) cannonHit.SetActive(false);
        if (sailHit != null) sailHit.SetActive(false);
    }

    void LateUpdate()
    {
        if (!_shipLocked || player == null)
            return;

        Vector3 playerMovement = player.position - _lastPlayerPosition;

        // Blokujemy ruch poziomy, ale pozwalamy na pionowy.
        transform.position += new Vector3(-playerMovement.x, playerMovement.y, 0f);

        // Utrzymanie statku poniżej gracza
        Vector3 pos = transform.position;
        pos.y = player.position.y + verticalOffset;
        transform.position = pos;

        _lastPlayerPosition = player.position;
    }

    public void Move()
    {
        if (_isMoving || !_online) 
            return;
        
        if (wall != null) wall.SetActive(true);
        if (wall1 != null) wall1.SetActive(true);
        
        StartCoroutine(MoveShip());
    }

    private IEnumerator MoveShip()
    {
        _online = false;
        _isMoving = true;

        // Przejście przygotowujące
        if (idle != null) idle.SetActive(false);
        if (toWind != null) toWind.SetActive(true);
        if (health != null) health.SetActive(true);

        yield return new WaitForSeconds(0.25f);

        // Właściwy lot
        if (toWind != null) toWind.SetActive(false);
        if (wind != null) wind.SetActive(true);

        float moved = 0f;

        while (moved < moveDistance)
        {
            float step = Mathf.Min(moveSpeed * Time.deltaTime, moveDistance - moved);

            transform.position += Vector3.right * step;
            moved += step;

            yield return null;
        }
        
        _shipLocked = true;

        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        _lastPlayerPosition = player.position;
        
        _isMoving = false;
        
        if (cannon1 != null) cannon1.SetActive(false);

        UpdateFacingDirection();
    
        if (cannon != null) cannon.SetActive(true);

        // Rozpoczęcie ataku:
        cannonBallSpawner.StartSpawning();
    }

    private void UpdateFacingDirection()
    {
        foreach (SpriteRenderer sr in _spriteRenderers)
            sr.flipX = !sr.flipX;
    }
}