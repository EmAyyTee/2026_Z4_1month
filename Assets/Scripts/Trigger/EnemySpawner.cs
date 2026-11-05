using UnityEngine;

namespace Trigger
{
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Enemy Prefab")]
        public GameObject enemyPrefab; // Wybór szablonu przeciwnika, który ma się pojawić

        [Header("Spawn Coordinates")]
        public Vector2 spawnPosition; // Pozycja, w której ma się pojawić

        private bool _hasSpawned;

        public void SpawnEnemy()
        {
            // Zabezpieczenie żeby beczka nie pojawiła się więcej niż raz
            if (_hasSpawned || enemyPrefab == null)
                return;

            // Tworzy kopię obiektu w wyznaczonym miejscu, bez żadnego obrotu/rotacji (Quaternion.identity)
            Instantiate(enemyPrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0f), Quaternion.identity);

            _hasSpawned = true;
        }
    }
}