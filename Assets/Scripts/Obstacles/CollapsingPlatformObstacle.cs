using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class CollapsingPlatformObstacle : MonoBehaviour
{
    [SerializeField, Min(0f)] private float collapseDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float destroyDelay = 3f;

    private Rigidbody2D platformBody;
    private bool collapseStarted;

    private void Awake()
    {
        platformBody = GetComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collapseStarted || collision.gameObject.GetComponentInParent<PlayerHealth>() == null)
            return;

        collapseStarted = true;
        StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        yield return new WaitForSeconds(collapseDelay);
        platformBody.bodyType = RigidbodyType2D.Dynamic;
        Destroy(gameObject, destroyDelay);
    }
}
