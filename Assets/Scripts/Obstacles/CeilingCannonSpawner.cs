using System.Collections;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [SerializeField] private GameObject _projectilePrefab;

    [SerializeField, Min(0.1f)]
    private float _cooldown = 1.5f;

    private void Start()
    {
        if (_projectilePrefab == null)
        {
            Debug.LogError("Przypisz prefab pocisku do SpawnPoint.", this);
            return;
        }

        StartCoroutine(SpawnCoroutine());
    }

    private IEnumerator SpawnCoroutine()
    {
        while (true)
        {
            Instantiate(
                _projectilePrefab,
                transform.position,
                transform.rotation
            );

            yield return new WaitForSeconds(_cooldown);
        }
    }
}