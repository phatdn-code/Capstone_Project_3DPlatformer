using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Input Manager")]
    public class PlayerInputManager : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INPUT ACTIONS ===

        public InputActionAsset actions;

        protected InputAction m_movement;
        protected InputAction m_run;
        protected InputAction m_jump;
        protected InputAction m_dive;
        protected InputAction m_swimUpward;
        protected InputAction m_spin;
        protected InputAction m_pickAndDrop;
        protected InputAction m_crouch;
        protected InputAction m_airDive;
        protected InputAction m_stomp;
        protected InputAction m_releaseLedge;
        protected InputAction m_pause;
        protected InputAction m_look;
        protected InputAction m_glide;
        protected InputAction m_dash;
        protected InputAction m_grindBrake;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME REFERENCES ===

        protected Player m_player;
        protected Camera m_camera;

        private float m_movementDirectionUnlockTime;
        private float? m_lastJumpTime;

        private bool m_isLocked = false;
        public bool IsLocked => m_isLocked;

        private bool m_canMove = true;
        public bool CanMove => m_canMove;

        private const string k_mouseDeviceName = "Mouse";
        private const float k_jumpBuffer = 0.15f;

        #endregion

        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        protected virtual void Awake()
        {
            CacheActions();
            InitializePlayer();
        }

        protected virtual void Start()
        {
            EnsureCamera();
            LockAllInputs(m_isLocked);
        }

        private void EnsureCamera()
        {
            if (m_camera != null)
                return;

            m_camera = Camera.main;
        }

        protected virtual void Update()
        {
            EnsureCamera();

            if (m_jump.WasPressedThisFrame())
                m_lastJumpTime = Time.time;
        }

        protected virtual void OnEnable() => LockAllInputs(m_isLocked);

        protected virtual void OnDisable() => LockAllInputs(m_isLocked);

        private void CacheActions()
        {
            m_movement = actions["Movement"];
            m_run = actions["Run"];
            m_jump = actions["Jump"];
            m_dive = actions["Dive"];
            m_swimUpward = actions["Swim Up"];
            m_spin = actions["Spin"];
            m_pickAndDrop = actions["PickAndDrop"];
            m_crouch = actions["Crouch"];
            m_airDive = actions["AirDive"];
            m_stomp = actions["Stomp"];
            m_releaseLedge = actions["ReleaseLedge"];
            m_pause = actions["Pause"];
            m_look = actions["Look"];
            m_glide = actions["Glide"];
            m_dash = actions["Dash"];
            m_grindBrake = actions["Grind Brake"];
        }

        protected virtual void InitializePlayer() => m_player = GetComponent<Player>();

        #endregion

        //─────────────────────────────────────────────
        #region === MOVEMENT & CAMERA DIRECTION ===

        public virtual Vector3 GetMovementDirection()
        {
            if (Time.time < m_movementDirectionUnlockTime)
                return Vector3.zero;

            Vector2 value = m_movement.ReadValue<Vector2>();
            return GetAxisWithCrossDeadZone(value);
        }

        public virtual Vector3 GetLookDirection()
        {
            Vector2 value = m_look.ReadValue<Vector2>();
            return IsLookingWithMouse()
                ? new Vector3(value.x, 0, value.y)
                : GetAxisWithCrossDeadZone(value);
        }

        public virtual Vector3 GetMovementCameraDirection(bool localSpace = true) =>
            GetMovementCameraDirection(out _, localSpace);

        public virtual Vector3 GetMovementCameraDirection(out float magnitude, bool localSpace = true)
        {
            EnsureCamera();

            return m_player.movingMode switch
            {
                PlayerMovementMode.SideScroller => GetHorizontalMovementCameraDirection(out magnitude),
                _ => GetLateralMovementCameraDirection(out magnitude, localSpace),
            };
        }

        public virtual Vector3 GetLateralMovementCameraDirection(out float magnitude, bool localSpace = true)
        {
            EnsureCamera();

            Vector3 direction = GetMovementDirection();
            magnitude = 0;

            if (m_camera == null || direction.sqrMagnitude == 0)
                return Vector3.zero;

            Quaternion rotation = Quaternion.FromToRotation(m_camera.transform.up, transform.up);
            direction = rotation * m_camera.transform.rotation * direction;

            if (localSpace)
            {
                direction = Vector3.ProjectOnPlane(direction, transform.up);
                direction = Quaternion.FromToRotation(transform.up, Vector3.up) * direction;
            }

            magnitude = direction.magnitude;
            return direction / magnitude;
        }

        public virtual Vector3 GetHorizontalMovementCameraDirection(out float magnitude)
        {
            EnsureCamera();

            Vector3 direction = GetMovementDirection();
            direction.z = magnitude = 0;

            if (direction.x != 0)
            {
                direction = m_player.pathForward * direction.x;
                magnitude = direction.magnitude;
                direction /= magnitude;
            }

            return direction;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === AXIS & INPUT CHECKERS ===

        public virtual Vector3 GetAxisWithCrossDeadZone(Vector2 axis)
        {
            float dz = InputSystem.settings.defaultDeadzoneMin;

            axis.x = Mathf.Abs(axis.x) > dz ? RemapToDeadzone(axis.x, dz) : 0;
            axis.y = Mathf.Abs(axis.y) > dz ? RemapToDeadzone(axis.y, dz) : 0;

            return new Vector3(axis.x, 0, axis.y);
        }

        public virtual Vector2 GetLookAxisRaw()
        {
            return m_look.ReadValue<Vector2>();
        }

        public virtual bool IsLookingWithMouse()
        {
            return m_look.activeControl != null &&
                   m_look.activeControl.device.name.Equals(k_mouseDeviceName);
        }

        public virtual bool GetRun() => m_run.IsPressed();
        public virtual bool GetRunUp() => m_run.WasReleasedThisFrame();
        public virtual bool GetJumpUp() => m_jump.WasReleasedThisFrame();
        public virtual bool GetSwimUpward() => m_swimUpward.IsPressed();
        public virtual bool GetDive() => m_dive.IsPressed();
        public virtual bool GetSpinDown() => m_spin.WasPressedThisFrame();
        public virtual bool GetPickAndDropDown() => m_pickAndDrop.WasPressedThisFrame();
        public virtual bool GetCrouchAndCraw() => m_crouch.IsPressed();
        public virtual bool GetAirDiveDown() => m_airDive.WasPressedThisFrame();
        public virtual bool GetStompDown() => m_stomp.WasPressedThisFrame();
        public virtual bool GetReleaseLedgeDown() => m_releaseLedge.WasPressedThisFrame();
        public virtual bool GetGlide() => m_glide.IsPressed();
        public virtual bool GetDashDown() => m_dash.WasPressedThisFrame();
        public virtual bool GetGrindBrake() => m_grindBrake.IsPressed();
        public virtual bool GetPauseDown() => m_pause.WasPressedThisFrame();

        public virtual bool GetJumpDown()
        {
            if (m_lastJumpTime != null && Time.time - m_lastJumpTime < k_jumpBuffer)
            {
                m_lastJumpTime = null;
                return true;
            }
            return false;
        }

        public virtual bool EscPressed()
        {
#if UNITY_STANDALONE
            return Keyboard.current.escapeKey.wasPressedThisFrame;
#else
            return false;
#endif
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MISC ===

        public void LockAllInputs(bool locked)
        {
            m_isLocked = locked;
            if (locked) actions.Disable();
            else actions.Enable();
        }

        protected float RemapToDeadzone(float value, float deadzone) =>
            Mathf.Sign(value) * ((Mathf.Abs(value) - deadzone) / (1 - deadzone));

        public virtual void LockMovementDirection(float duration = 0.25f)
        {
            m_movementDirectionUnlockTime = Time.time + duration;
        }

        public void DisableMovementTemporarily(float duration)
        {
            StartCoroutine(DisableMovementRoutine(duration));
        }

        private IEnumerator DisableMovementRoutine(float duration)
        {
            m_canMove = false;
            yield return new WaitForSeconds(duration);
            m_canMove = true;
        }

        #endregion
    }
}
