using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerInputManager))]
    [RequireComponent(typeof(PlayerStatsManager))]
    [RequireComponent(typeof(PlayerStateManager))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Player))]
    [RequireComponent(typeof(PlayerAudio))]
    [RequireComponent(typeof(PlayerAnimator))]
    [RequireComponent(typeof(PlayerParticles))]
    public class PlayerHub : SingletonMonobehaviour<PlayerHub>
    {
        //─────────────────────────────────────────────
        #region === Inspector References ===

        [Header("Player Model Root")]
        [SerializeField] private GameObject playerModelRoot;

        #endregion
        //─────────────────────────────────────────────

        #region === Cached Subsystems (GetComponent) ===

        public PlayerInputManager InputManager { get; private set; }
        public PlayerStatsManager StatsManager { get; private set; }
        public PlayerStateManager StateManager { get; private set; }
        public Health Health { get; private set; }
        public Player Player { get; private set; }
        public PlayerAudio Audio { get; private set; }
        public PlayerAnimator Animator { get; private set; }
        public PlayerParticles Particles { get; private set; }

        private PlayerCamera m_camera;

        #endregion
        //─────────────────────────────────────────────

        #region === Runtime State ===

        public bool IsControllingWaterCannon { get; private set; }

        #endregion
        //─────────────────────────────────────────────

        #region === Unity Lifecycle ===

        protected override void Awake()
        {
            base.Awake();
            CacheSubsystems();
        }

        private void Start()
        {
            m_camera = FindFirstObjectByType<PlayerCamera>();
        }

        #endregion
        //─────────────────────────────────────────────

        #region === Public API ===

        /// <summary>Khóa/mở toàn bộ input + freeze camera (cutscene/pause/death...).</summary>
        public void LockPlayer(bool locked)
        {
            if (InputManager != null)
                InputManager.LockAllInputs(locked);

            if (m_camera != null)
                m_camera.SetFreeze(locked);
        }

        /// <summary>
        /// Bật/tắt điều khiển player kèm ẩn/hiện model.
        /// Lưu ý: logic hiện tại dùng "enable=true" để khóa input + ẩn model (khi vào cannon).
        /// </summary>
        public void SetPlayerControlAndModel(bool enable)
        {
            if (playerModelRoot != null)
                playerModelRoot.SetActive(!enable);

            LockPlayer(enable);
        }

        /// <summary>Set cờ trạng thái player đang điều khiển water cannon hay không.</summary>
        public void SetWaterCannonControl(bool isControlling)
        {
            IsControllingWaterCannon = isControlling;
        }

        #endregion
        //─────────────────────────────────────────────

        #region === Private Helpers ===

        /// <summary>Cache các component bắt buộc trên Player để truy cập nhanh.</summary>
        private void CacheSubsystems()
        {
            InputManager = GetComponent<PlayerInputManager>();
            StatsManager = GetComponent<PlayerStatsManager>();
            StateManager = GetComponent<PlayerStateManager>();
            Health = GetComponent<Health>();
            Player = GetComponent<Player>();
            Audio = GetComponent<PlayerAudio>();
            Animator = GetComponent<PlayerAnimator>();
            Particles = GetComponent<PlayerParticles>();
        }

        #endregion
        //─────────────────────────────────────────────
    }
}
