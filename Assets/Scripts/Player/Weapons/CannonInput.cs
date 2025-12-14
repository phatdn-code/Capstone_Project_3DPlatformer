using UnityEngine;
using UnityEngine.InputSystem;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Input riêng cho cannon:
    /// - Look: xoay nòng súng.
    /// - Spin: bắn.
    /// - Interact: vào/thoát chế độ điều khiển cannon.
    /// </summary>
    public class CannonInput : MonoBehaviour
    {
        [Header("Input Actions")]
        [Tooltip("Gán asset input riêng cho cannon (có các action: Look, Spin, Interact).")]
        public InputActionAsset actions;

        private InputAction _look;
        private InputAction _fire;
        private InputAction _interact;

        private bool _isEnabled;

        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Cache action theo tên trong asset.</summary>
        private void Awake()
        {
            CacheActions();
        }

        /// <summary>Bật input khi script được enable.</summary>
        private void OnEnable()
        {
            EnableActions();
        }

        /// <summary>Tắt input khi script bị disable.</summary>
        private void OnDisable()
        {
            DisableActions();
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === INIT & ENABLE ===

        /// <summary>Lấy các action Look / Spin / Interact từ asset.</summary>
        private void CacheActions()
        {
            if (actions == null)
            {
                Debug.LogWarning($"{nameof(CannonInput)}: Chưa gán InputActionAsset cho cannon.");
                return;
            }

            _look = actions["Look"];
            _fire = actions["Fire"];
            _interact = actions["Interact"];

            if (_look == null || _fire == null || _interact == null)
            {
                Debug.LogWarning($"{nameof(CannonInput)}: Không tìm thấy action Look / Spin / Interact trong asset.");
            }
        }

        /// <summary>Bật toàn bộ action trong asset.</summary>
        public void EnableActions()
        {
            if (actions == null || _isEnabled)
                return;

            actions.Enable();
            _isEnabled = true;
        }

        /// <summary>Tắt toàn bộ action trong asset.</summary>
        public void DisableActions()
        {
            if (actions == null || !_isEnabled)
                return;

            actions.Disable();
            _isEnabled = false;
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === API ĐỌC INPUT ===

        /// <summary>Lấy raw vector Look (mouse / analog).</summary>
        public Vector2 GetLookAxisRaw()
        {
            return _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
        }

        /// <summary>True trong frame vừa nhấn nút bắn (Spin).</summary>
        public bool GetFireDown()
        {
            return _fire != null && _fire.WasPressedThisFrame();
        }

        /// <summary>True khi đang giữ nút bắn (Fire đang pressed).</summary>
        public bool GetFireHeld()
        {
            return _fire != null && _fire.IsPressed();
        }

        /// <summary>True trong frame vừa nhả nút bắn (Fire released).</summary>
        public bool GetFireUp()
        {
            return _fire != null && _fire.WasReleasedThisFrame();
        }


        /// <summary>True trong frame vừa nhấn Interact (E).</summary>
        public bool GetInteractDown()
        {
            return _interact != null && _interact.WasPressedThisFrame();
        }

        #endregion
        //────────────────────────────────────────────────────
    }
}
