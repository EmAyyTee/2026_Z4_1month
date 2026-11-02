using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public sealed class ContactDamage2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 20;
    [SerializeField, Min(0.1f)] private float repeatDelay = 1f;
    private readonly Dictionary<PlayerHealth, float> nextHitTimes = new Dictionary<PlayerHealth, float>();

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void TryDamage(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerHealth>();
        if (player == null || player.IsDead)
            return;
        if (nextHitTimes.TryGetValue(player, out float nextHit) && Time.time < nextHit)
            return;
        nextHitTimes[player] = Time.time + repeatDelay;
        player.TakeDamage(damage, transform);
    }

    private void OnDisable()
    {
        nextHitTimes.Clear();
    }
}
