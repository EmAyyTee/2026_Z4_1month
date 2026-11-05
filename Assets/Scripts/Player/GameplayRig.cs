using Game;
using UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    [DisallowMultipleComponent, DefaultExecutionOrder(-100)]
    public sealed class GameplayRig : MonoBehaviour
    {
        [SerializeField] private PlayerMovement player;
        [SerializeField] private Camera gameplayCamera;
        [SerializeField] private CinemachineCamera followCamera;
        [SerializeField] private HUDManager hud;

        public PlayerMovement Player => player;
        public Camera GameplayCamera => gameplayCamera;
        public CinemachineCamera FollowCamera => followCamera;
        public HUDManager Hud => hud;

        private void Awake()
        {
            if (followCamera != null && player != null)
                followCamera.Follow = player.transform;
            if (hud == null || player == null)
                return;
            var session = hud.Session != null ? hud.Session : GameSession.Instance;
            if (session == null)
                session = FindAnyObjectByType<GameSession>();
            if (session == null)
            {
                var owner = new GameObject("GameSession");
                SceneManager.MoveGameObjectToScene(owner, gameObject.scene);
                session = owner.AddComponent<GameSession>();
            }
            hud.Bind(player.GetComponent<PlayerHealth>(), session);
        }
    }
}
