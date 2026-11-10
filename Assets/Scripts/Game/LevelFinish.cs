using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFinish : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    private bool finished;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponentInParent<PlayerHealth>();

        Transform current = other.transform;
        
        if (current.CompareTag("Enemy") || current.CompareTag("Boss"))
            return;
            
        if (finished || player == null || player.IsDead)
            return;

        if (victoryPanel == null)
        {
            Debug.LogError("Przypisz Victory Panel w Inspectorze.", this);
            return;
        }

        finished = true;
        victoryPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}