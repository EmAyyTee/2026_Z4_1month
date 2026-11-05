using System.Collections;
using Player;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class CollapsingPlatformObstacle : MonoBehaviour
{
    [SerializeField, Min(0f)] private float collapseDelay = 0.35f;
    [SerializeField, Min(0.1f)] private float destroyDelay = 3f;

    private Rigidbody2D platformBody;
    private bool collapseStarted;
    private SpriteRenderer platformRenderer;

    private void Awake()
    {
        platformBody = GetComponent<Rigidbody2D>();
        platformBody.bodyType = RigidbodyType2D.Kinematic;
        platformRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collapseStarted || collision.gameObject.GetComponentInParent<PlayerHealth>() == null)
            return;

        bool landedOnTop = false;
        for (int i = 0; i < collision.contactCount; i++)
            landedOnTop |= collision.GetContact(i).normal.y < -0.5f;
        if (!landedOnTop)
            return;

        collapseStarted = true;
        if (platformRenderer != null)
            platformRenderer.color = new Color(1f, 0.45f, 0.25f);
        StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        yield return new WaitForSeconds(collapseDelay);
        platformBody.bodyType = RigidbodyType2D.Dynamic;
        Destroy(gameObject, destroyDelay);
    }
}
