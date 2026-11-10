
using System.Collections;
using Enemy;
using UnityEngine;

namespace Ship.Cannon_Ball
{
    public class CannonBallSpawner : MonoBehaviour
    {
        [Header("Cannon Balls")]
        public GameObject cannonBallPrefab;
        public GameObject reflectableCannonBallPrefab;

        [Header("Cannon Visuals")]
        public GameObject cannonShoot;
        public GameObject cannonReload;

        [Header("Fixed World Spawn Area")]
        [Tooltip("Globalna wysokość spawnowania kul.")]
        public float spawnHeight = 14f;

        [Tooltip("Minimalna globalna pozycja X.")]
        public float minX = -4f;

        [Tooltip("Maksymalna globalna pozycja X.")]
        public float maxX = 4f;

        [Tooltip("Globalna pozycja Z.")]
        public float spawnZ = 0f;

        [Header("Spawn Settings")]
        public float spawnInterval = 1.5f;

        [Header("Special CannonBall")]
        public int reflectableBallInterval = 5;

        [Header("Spawn Visualization")]
        public bool showSpawnArea = true;
        public float spawnMarkerRadius = 0.25f;

        private BossHealth _bossHealth;

        private float _spawnTimer;
        private int _spawnedBalls;

        private bool _isSpawning;
        private bool _isShooting;
        private bool _isPaused;

        private void Awake()
        {
            _bossHealth = FindFirstObjectByType<BossHealth>();
        }

        private void Update()
        {
            if (_bossHealth != null && _bossHealth.IsDead)
            {
                StopSpawning();
                return;
            }

            if (!_isSpawning || _isPaused)
                return;

            _spawnTimer -= Time.deltaTime;

            if (_spawnTimer <= 0f && !_isShooting)
                StartCoroutine(ShootCannonBall());
        }

        public void StartSpawning()
        {
            _spawnedBalls = 0;
            _spawnTimer = 0f;
            _isSpawning = true;
        }

        private void StopSpawning()
        {
            _isSpawning = false;
        }

        public void PauseSpawning()
        {
            _isPaused = true;
        }

        public void ResumeSpawning()
        {
            _isPaused = false;
        }

        private IEnumerator ShootCannonBall()
        {
            _isShooting = true;

            // Animacja armaty.
            if (cannonShoot != null)
                cannonShoot.SetActive(false);

            if (cannonReload != null)
                cannonReload.SetActive(true);

            if (cannonShoot != null)
                cannonShoot.SetActive(true);

            if (cannonReload != null)
                cannonReload.SetActive(false);

            yield return new WaitForSeconds(0.25f);

            // Czekamy, jeśli boss otrzymał obrażenia.
            while (_isPaused)
                yield return null;

            // Animacja wystrzału.
            if (cannonShoot != null)
                cannonShoot.SetActive(false);

            if (cannonReload != null)
                cannonReload.SetActive(true);

            // Tworzenie kuli.
            SpawnCannonBall();

            _spawnedBalls++;
            _spawnTimer = spawnInterval;

            _isShooting = false;
        }

        private void SpawnCannonBall()
        {
            if (cannonBallPrefab == null)
                return;

            // Co określoną liczbę kul pojawia się kula odbijalna.
            int interval = Mathf.Max(1, reflectableBallInterval);

            bool isReflectable =
                (_spawnedBalls + 1) % interval == 0;

            GameObject selectedPrefab =
                isReflectable && reflectableCannonBallPrefab != null
                    ? reflectableCannonBallPrefab
                    : cannonBallPrefab;

            if (selectedPrefab == null)
                return;

            // Losujemy globalną pozycję X.
            float left = Mathf.Min(minX, maxX);
            float right = Mathf.Max(minX, maxX);

            float randomX = Random.Range(left, right);

            // Stała pozycja świata, niezależna od gracza.
            Vector3 spawnPosition = new Vector3(
                randomX,
                spawnHeight,
                spawnZ
            );

            Instantiate(
                selectedPrefab,
                spawnPosition,
                Quaternion.identity
            );

            Debug.Log(
                "[CannonBallSpawner] Spawn at WORLD position: "
                + spawnPosition
            );
        }

        // WIZUALIZACJA OBSZARU SPAWNU W SCENE VIEW.
        private void OnDrawGizmos()
        {
            if (!showSpawnArea)
                return;

            float left = Mathf.Min(minX, maxX);
            float right = Mathf.Max(minX, maxX);

            Vector3 leftPoint = new Vector3(
                left,
                spawnHeight,
                spawnZ
            );

            Vector3 rightPoint = new Vector3(
                right,
                spawnHeight,
                spawnZ
            );

            Vector3 center = new Vector3(
                (left + right) * 0.5f,
                spawnHeight,
                spawnZ
            );

            Gizmos.color = Color.cyan;

            // Linia przedstawiająca cały obszar spawnowania.
            Gizmos.DrawLine(leftPoint, rightPoint);

            // Granice obszaru.
            Gizmos.DrawWireSphere(
                leftPoint,
                spawnMarkerRadius
            );

            Gizmos.DrawWireSphere(
                rightPoint,
                spawnMarkerRadius
            );

            // Środkowy punkt obszaru.
            Gizmos.DrawWireSphere(
                center,
                spawnMarkerRadius * 0.7f
            );

            // Pionowe znaczniki wysokości.
            Gizmos.DrawLine(
                leftPoint,
                leftPoint + Vector3.down * 0.6f
            );

            Gizmos.DrawLine(
                rightPoint,
                rightPoint + Vector3.down * 0.6f
            );
        }
    }
}
