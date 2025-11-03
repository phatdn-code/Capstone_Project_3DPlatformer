using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Centralized hub that caches all Player subsystems and
    /// provides global access to control player state, input, and camera.
    /// </summary>
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
        #region === Cached Subsystems ===

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
        #region === Unity Lifecycle ===

        protected override void Awake()
        {
            base.Awake();
            CacheSubsystems();
        }

        private void Start()
        {
            // Cache PlayerCamera (once per scene)
            m_camera = FindFirstObjectByType<PlayerCamera>();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === Core Methods ===

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

        /// <summary>
        /// Lock or unlock player input and camera movement.
        /// Useful for cutscenes, menus, or death states.
        /// </summary>
        public void LockPlayer(bool locked)
        {
            if (InputManager != null)
                InputManager.LockAllInputs(locked);

            if (m_camera != null)
                m_camera.SetFreeze(locked);
        }

        #endregion
    }
}
