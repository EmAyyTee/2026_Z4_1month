using UnityEngine;

namespace Trigger.Barrel
{
    public class BarrelSpawner : MonoBehaviour
    {
        [Header("Barrel Prefab")]
        public GameObject barrelPrefab; // Wybór szablonu spadającej beczki

        [Header("Spawn Coordinates")]
        public Vector2 spawnPosition; // Pozycja, w której ma się pojawić

        private bool _hasSpawned;

        public void SpawnBarrel()
        {
            // Zabezpieczenie, żeby beczka nie pojawiła się więcej niż raz
            if (_hasSpawned || barrelPrefab == null)
                return;
        
            // Tworzy kopię obiektu w wyznaczonym miejscu, bez żadnego obrotu/rotacji (Quaternion.identity)
            Instantiate(barrelPrefab, new Vector3(spawnPosition.x, spawnPosition.y, 0f), Quaternion.identity);

            _hasSpawned = true;
        }
    }
}