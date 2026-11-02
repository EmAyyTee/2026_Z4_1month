using UnityEngine;

public class CoinPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int _value = 1;

    private bool _collected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_collected)
            return;

        PlayerHealth player =
            other.GetComponentInParent<PlayerHealth>();

        if (player == null || player.IsDead)
            return;

        GameSession session = GameSession.Instance;

        _collected = true;
        session.AddCoins(_value);

        Destroy(gameObject);
    }
}