using Game;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameButton : MonoBehaviour
{
    [SerializeField] private string firstLevelScene = "Level 1";

    public void StartNewGame()
    {
        if (!CheckpointSave.TrySave(firstLevelScene, 0, 0))
            return;

        SceneManager.LoadScene(firstLevelScene);
    }
}