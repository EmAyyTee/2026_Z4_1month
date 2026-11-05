using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class MovingPlatform2D : MonoBehaviour
{
    [SerializeField] private Vector2 localTravel = new Vector2(6f, 0f);
    [SerializeField, Min(0.01f)] private float speed = 0.65f;
    [SerializeField, Min(0f)] private float endpointPause = 0.5f;
    private readonly Dictionary<Collider2D, Rigidbody2D> riders = new Dictionary<Collider2D, Rigidbody2D>();
    private readonly HashSet<Rigidbody2D> carriedBodies = new HashSet<Rigidbody2D>();
    private Rigidbody2D body;
    private Vector2 origin;
    private Vector2 destination;
    private bool returning;
    private float pauseRemaining;

    public Vector2 StartPoint => origin;
    public Vector2 EndPoint => destination;

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        origin = body.position;
        destination = origin + (Vector2)transform.TransformVector(localTravel);
        pauseRemaining = endpointPause;
    }

    private void FixedUpdate()
    {
        if (pauseRemaining > 0f)
        {
            pauseRemaining -= Time.fixedDeltaTime;
            body.linearVelocity = Vector2.zero;
            return;
        }
        var target = returning ? origin : destination;
        var next = Vector2.MoveTowards(body.position, target, speed * Time.fixedDeltaTime);
        var delta = next - body.position;
        carriedBodies.Clear();
        foreach (var pair in riders)
        {
            var rider = pair.Value;
            if (pair.Key == null || rider == null || !rider.simulated || !carriedBodies.Add(rider))
                continue;
            if (rider.linearVelocity.y > Mathf.Max(0f, body.linearVelocity.y) + 0.1f)
                continue;
            rider.position += new Vector2(delta.x, 0f);
        }
        body.MovePosition(next);
        if ((next - target).sqrMagnitude < 0.000001f)
        {
            returning = !returning;
            pauseRemaining = endpointPause;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TrackRider(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TrackRider(collision);
    }

    private void TrackRider(Collision2D collision)
    {
        var rider = collision.rigidbody;
        if (rider == null || rider.bodyType != RigidbodyType2D.Dynamic)
            return;
        for (int i = 0; i < collision.contactCount; i++)
            if (collision.GetContact(i).normal.y < -0.5f)
            {
                riders[collision.collider] = rider;
                return;
            }
        riders.Remove(collision.collider);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        riders.Remove(collision.collider);
    }

    private void OnDisable()
    {
        riders.Clear();
        if (body != null)
            body.linearVelocity = Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        var start = Application.isPlaying ? origin : (Vector2)transform.position;
        var end = Application.isPlaying ? destination : start + (Vector2)transform.TransformVector(localTravel);
        Gizmos.color = new Color(0.2f, 0.9f, 1f);
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(start, 0.08f);
        Gizmos.DrawWireSphere(end, 0.08f);
    }
}
