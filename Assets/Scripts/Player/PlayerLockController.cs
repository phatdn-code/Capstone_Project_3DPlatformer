using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Global handler to lock and unlock the player & camera controls.
    /// </summary>
    public class PlayerLockController : SingletonMonobehaviour<PlayerLockController>
    {
        private PlayerInputManager m_inputs;
        private PlayerCamera m_camera;

        private void Start()
        {
            m_inputs = GetComponent<PlayerInputManager>();
            m_camera = FindFirstObjectByType<PlayerCamera>();
        }

        public void LockPlayer(bool locked)
        {
            if (m_inputs != null)
                m_inputs.LockAllInputs(locked);

            if (m_camera != null)
                m_camera.SetFreeze(locked);
        }
    }
}
