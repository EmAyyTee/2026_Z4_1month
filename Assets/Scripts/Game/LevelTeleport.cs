using Game;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTeleport : MonoBehaviour
{
    [SerializeField] private string nextScene;
    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (isLoading || player == null || player.IsDead)
            return;

        isLoading = true;

        GameSession session = GameSession.Instance;

        if (session == null)
        {
            Debug.LogError("Brakuje GameSession na tym poziomie.", this);
            return;
        }

        if (!CheckpointSave.TrySave(nextScene, session.Coins, session.Score))
            return;

        SceneManager.LoadScene(nextScene);
    }
}