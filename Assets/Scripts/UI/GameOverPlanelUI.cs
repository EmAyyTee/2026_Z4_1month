using TMPro;
using UnityEngine;

public class GameOverPanelUI : MonoBehaviour
{
    [SerializeField] private TMP_Text _scoreText;

    private void OnEnable()
    {
        if (_scoreText == null)
        {
            Debug.LogError("Nie przypisano tekstu wyniku.", this);
            return;
        }

        GameSession session = GameSession.Instance;

        if (session == null)
        {
            _scoreText.text = "Score: 0";
            return;
        }

        _scoreText.text = $"Score: {session.Score}";
    }
}