using System.Collections;
using Ship.Cannon_Ball;
using UnityEngine;

namespace Ship
{
    public class ShipMovement : MonoBehaviour
    {
        [Header("Movement")]
        public float moveDistance = 6.5f;
        public float moveSpeed = 2f;

        [Header("Final Position")]
        public float targetX = 8f;

        [Header("Visuals")]
        public GameObject sail;
        public GameObject toWind;
        public GameObject wind;
        public GameObject shipIdle;
        public GameObject healthBar;
        public GameObject wall;
        public GameObject cannonShoot;
        public GameObject cannonReload;
        public GameObject cannonIdle;
        public GameObject cannonHit;
        public GameObject sailHit;

        [Header("Ship")]
        public Transform ship;
        public ShipScale shipScale;
        public ShipZoom shipZoom;
        
        private SpriteRenderer[] _spriteRenderers;
        public CannonBallSpawner cannonBallSpawner;

        private bool _isMoving;
        private bool _online = true;

        void Start()
        {
            _spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            cannonBallSpawner = FindFirstObjectByType<CannonBallSpawner>();

            // Stany żagla
            if (sail != null) sail.SetActive(true);
            if (toWind != null) toWind.SetActive(false);
            if (wind != null) wind.SetActive(false);
            
            // Stany armaty 
            if (cannonIdle != null) cannonIdle.SetActive(true);
            if (cannonReload != null) cannonReload.SetActive(false);
            if (cannonHit != null) cannonHit.SetActive(false);
            
            // Reszta
            if (sailHit != null) sailHit.SetActive(false);
            if (shipIdle != null) shipIdle.SetActive(true);
            if (healthBar != null) healthBar.SetActive(false);
            if (wall != null) wall.SetActive(false);
        }

        public void Move()
        {
            if (_isMoving || !_online)
                return;
        
            // Zamknięcie gracza przez aktywowanie "niewidzialnych ścian"
            if (wall != null) wall.SetActive(true);

            StartCoroutine(MoveShip());
        }

        private IEnumerator MoveShip()
        {
            _online = false;
            _isMoving = true;

            if (healthBar != null) healthBar.SetActive(true);
            
            // Przejście ze stanu No_wind do stanu Wind
            if (sail != null) sail.SetActive(false);
            if (toWind != null) toWind.SetActive(true);

            yield return new WaitForSeconds(0.25f);

            if (toWind != null) toWind.SetActive(false);
            if (wind != null) wind.SetActive(true);

            // Przejście z lewej do prawej strony
            float moved = 0f;

            while (moved < moveDistance)
            {
                float step = Mathf.Min(moveSpeed * Time.deltaTime, moveDistance - moved);

                transform.position += Vector3.right * step;
                moved += step;

                yield return null;
            }

            transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

            _isMoving = false;
            
            // Odłączenie statku i wyłączenie skalowania
            if (shipScale != null)
            {
                ship.transform.SetParent(null, true);
                shipScale.Activate();
                if (shipZoom != null)
                    shipZoom.active = true;
            }

            // Wyłączenie sprite armaty celującej w prawo
            if (cannonIdle != null) cannonIdle.SetActive(false);

            // Odwrócenie wszystkich rzeczy na statku
            foreach (SpriteRenderer sr in _spriteRenderers)
                sr.flipX = !sr.flipX;

            // Rozpoczęcie ostrzału
            if (cannonBallSpawner != null) cannonBallSpawner.StartSpawning();
        }
    }
}