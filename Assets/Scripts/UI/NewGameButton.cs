using UnityEngine;
using UnityEngine.SceneManagement;

public class NewGameButton : MonoBehaviour
{
    [SerializeField] private string firstLevelScene = "Level01";

    public void StartNewGame()
    {
        SceneManager.LoadScene(firstLevelScene);
    }
}