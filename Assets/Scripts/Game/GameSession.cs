using System;
using UnityEngine;

namespace Game
{
    public class GameSession : MonoBehaviour
    {
        public static GameSession Instance { get; private set; }

        [SerializeField] private int _coins;
        [SerializeField] private int _score;

        public int Coins => _coins;
        public int Score => _score;

        public event Action StatsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _coins = 0;
            _score = 0;

            if (CheckpointSave.TryLoad(out var checkpoint) &&
                checkpoint.sceneName == gameObject.scene.name)
            {
                _coins = checkpoint.coins;
                _score = checkpoint.score;
                StatsChanged?.Invoke();
            }
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0)
                return;

            _coins += amount;
            _score += amount * 100;

            StatsChanged?.Invoke();
        }

        public void AddScore(int amount)
        {
            if (amount <= 0)
                return;

            _score += amount;
            StatsChanged?.Invoke();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}