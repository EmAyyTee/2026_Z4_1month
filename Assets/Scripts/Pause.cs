using UnityEngine;

namespace Game
{
    public class PauseMenu : MonoBehaviour
    {
        [Header("Pause UI")]
        [SerializeField] private GameObject pausePanel;

        private bool _isPaused;

        public bool IsPaused => _isPaused;

        private void Awake()
        {
            // Przywrócenie normalnego czasu gry.
            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (pausePanel != null)
                pausePanel.SetActive(false);
        }

        private void Update()
        {
            // ESC - pauza / wznowienie.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        public void TogglePause()
        {
            if (_isPaused)
                Resume();
            else
                Pause();
        }

        public void Pause()
        {
            if (_isPaused)
                return;

            _isPaused = true;

            Time.timeScale = 0f;
            AudioListener.pause = true;

            if (pausePanel != null)
                pausePanel.SetActive(true);

            Debug.Log("[PauseMenu] Game Paused");
        }

        public void Resume()
        {
            if (!_isPaused)
                return;

            _isPaused = false;

            Time.timeScale = 1f;
            AudioListener.pause = false;

            if (pausePanel != null)
                pausePanel.SetActive(false);

            Debug.Log("[PauseMenu] Game Resumed");
        }

        private void OnDestroy()
        {
            // Zabezpieczenie przy zmianie sceny lub niszczeniu obiektu.
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}