using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Ship.Cannon_Ball
{
    public class CannonBallSpawner : MonoBehaviour
    {
        [Header("References")]
        public GameObject cannonBallPrefab;
        public GameObject reflectableCannonBallPrefab;
        public Transform player;

        [Header("Cannon Visuals")]
        public GameObject cannon;
        [FormerlySerializedAs("cannon_2")] public GameObject cannon2;
        public float shootAnimationDuration = 0.5f;

        [Header("Spawn Position")]
        public float spawnHeight = 8f;
        public float minX = -5f;
        public float maxX = 5f;

        [Header("Spawn Settings")]
        public float spawnInterval = 1.5f;
        public int cannonBallCount = 10;

        [Header("Special CannonBall")]
        public int reflectableBallInterval = 5;

        private float _spawnTimer;
        private int _spawnedBalls;
        private bool _isSpawning;
        private bool _isShooting;
        private bool _isPaused;

        private void Update()
        {
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

        public void StopSpawning()
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
            if (cannon != null) cannon.SetActive(false);

            if (cannon2 != null) cannon2.SetActive(true);

            // Powrót do normalnego wyglądu armaty.
            if (cannon != null) cannon.SetActive(true);
        
            if (cannon2 != null) cannon2.SetActive(false);
        
            yield return new WaitForSeconds(shootAnimationDuration);
        
            // Jeśli boss otrzymał obrażenia, czekamy z wystrzałem.
            while (_isPaused)
                yield return null;
        
            if (cannon != null) cannon.SetActive(false);

            if (cannon2 != null) cannon2.SetActive(true);
            
            // Spawn kuli.
            SpawnCannonBall();

            _spawnedBalls++;
            _spawnTimer = spawnInterval;

            if (_spawnedBalls >= cannonBallCount)
                StopSpawning();

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