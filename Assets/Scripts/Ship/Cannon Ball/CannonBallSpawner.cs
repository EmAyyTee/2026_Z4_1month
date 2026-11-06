using System.Collections;
using UnityEngine;

namespace Ship.Cannon_Ball
{
    public class CannonBallSpawner : MonoBehaviour
    {
        [Header("Player")]
        public Transform player;

        [Header("Cannon Balls")]
        public GameObject cannonBallPrefab;
        public GameObject reflectableCannonBallPrefab;

        [Header("Cannon Visuals")]
        public GameObject cannonShoot;
        public GameObject cannonReload;

        [Header("Spawn Position")]
        public float spawnHeight = 14f;
        public float minX = -4f;
        public float maxX = 4f;

        [Header("Spawn Settings")]
        public float spawnInterval = 1.5f;

        [Header("Special CannonBall")]
        public int reflectableBallInterval = 5; // Co ile pojawia się czerwona kula armatnia

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
        
            // Animacja wystrzału.
            if (cannonShoot != null) cannonShoot.SetActive(false);
            if (cannonReload != null) cannonReload.SetActive(true);

            // Powrót do normalnego wyglądu armaty.
            if (cannonShoot != null) cannonShoot.SetActive(true);
            if (cannonReload != null) cannonReload.SetActive(false);
        
            yield return new WaitForSeconds(0.25f);
        
            // Jeśli boss otrzymał obrażenia, czekamy z wystrzałem.
            while (_isPaused) yield return null;
        
            // Animacja wystrzału.
            if (cannonShoot != null) cannonShoot.SetActive(false);
            if (cannonReload != null) cannonReload.SetActive(true);
            
            // Spawn kuli.
            SpawnCannonBall();

            _spawnedBalls++;
            _spawnTimer = spawnInterval;
            
            _isShooting = false;
        }

        private void SpawnCannonBall()
        {
            if (cannonBallPrefab == null || player == null)
                return;

            bool isReflectable = (_spawnedBalls + 1) % reflectableBallInterval == 0;

            GameObject selectedPrefab = isReflectable ? reflectableCannonBallPrefab : cannonBallPrefab;

            if (selectedPrefab == null)
                return;
            
            float randomX = Random.Range(minX, maxX);

            Vector3 spawnPosition = new Vector3(player.position.x + randomX, spawnHeight, 0f);

            Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
        }
    }
}