using Game;
using Player;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class HUDManager : MonoBehaviour
    {
        [SerializeField] private GameSession _session;
        [SerializeField] private TMP_Text _coinsText;
        [SerializeField] private PlayerHealth _playerHealth;
        [SerializeField] private Slider _healthBar;
        private GameSession _subscribedSession;

        public GameSession Session => _session;
        public PlayerHealth PlayerHealth => _playerHealth;
        public Slider HealthBar => _healthBar;
        public TMP_Text CoinsText => _coinsText;

        public void Bind(PlayerHealth playerHealth, GameSession session)
        {
            Unsubscribe();
            _playerHealth = playerHealth;
            _session = session;
            ConnectHealthBar();
            Subscribe();
            RefreshHUD();
        }

        private void OnEnable()
        {
            ConnectHealthBar();
            Subscribe();
            RefreshHUD();
        }

        private void Start()
        {
            Bind(_playerHealth != null ? _playerHealth : FindAnyObjectByType<PlayerHealth>(),
                _session != null ? _session : GameSession.Instance);
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ConnectHealthBar()
        {
            if (_healthBar == null)
                _healthBar = GetComponentInChildren<Slider>(true);
            if (_playerHealth == null || _healthBar == null)
                return;
            _playerHealth.healthBar = _healthBar;
            _healthBar.minValue = 0;
            _healthBar.maxValue = _playerHealth.maxHealth;
            _healthBar.value = Application.isPlaying ? _playerHealth.CurrentHealth : _playerHealth.maxHealth;
        }

        private void Subscribe()
        {
            if (!isActiveAndEnabled || _session == null || _subscribedSession == _session)
                return;
            _session.StatsChanged += RefreshHUD;
            _subscribedSession = _session;
        }

        private void Unsubscribe()
        {
            if (_subscribedSession != null)
                _subscribedSession.StatsChanged -= RefreshHUD;
            _subscribedSession = null;
        }

        private void RefreshHUD()
        {
            if (_session == null || _coinsText == null)
                return;

            _coinsText.text = _session.Coins.ToString();
        }
    }
}
