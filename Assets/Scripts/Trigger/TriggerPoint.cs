using Ship;
using UnityEngine;

namespace Trigger
{
    public class TriggerPoint : MonoBehaviour
    {
        public EnemySpawner spawnerE;
        public Trigger.Barrel.BarrelSpawner spawnerB;
        public ShipMovement movementS;
    
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (spawnerE != null)
                spawnerE.SpawnEnemy();
        
            if (spawnerB != null)
                spawnerB.SpawnBarrel();
        
            if (movementS != null)
                movementS.Move();
        }
    }
}
