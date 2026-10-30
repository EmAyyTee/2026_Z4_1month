using UnityEngine;

public class TriggerPoint : MonoBehaviour
{
    public EnemySpawner spawnerE;
    
    public BarrelSpawner spawnerB;


    private void OnTriggerEnter2D(Collider2D other)
    {

        if (!other.CompareTag("Player"))
            return;

        if (spawnerE != null)
        {
            spawnerE.SpawnEnemy();
        }
        
        if (spawnerB != null)
        {
            spawnerB.SpawnBarrel();
        }
    }
}