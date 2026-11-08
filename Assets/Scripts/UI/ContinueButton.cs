using Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ContinueButton : MonoBehaviour
{
    private void OnEnable()
    {
        GetComponent<Button>().interactable = CheckpointSave.TryLoad(out _);
    }

    public void ContinueGame()
    {
        if (!CheckpointSave.TryLoad(out var checkpoint))
        {
            GetComponent<Button>().interactable = false;
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(checkpoint.sceneName);
    }
}