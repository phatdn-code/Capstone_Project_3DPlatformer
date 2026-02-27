using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Enemy))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy Animator")]
    public class EnemyAnimator : MonoBehaviour
    {
        #region ===== INSPECTOR =====

        [Header("Animator Reference")]
        public Animator animator;

        [Header("Parameters Names")]
        public string stateName = "State";
        public string lastStateName = "Last State";

        // Speed thật (magnitude)
        public string lateralSpeedName = "Lateral Speed";
        public string verticalSpeedName = "Vertical Speed";

        // Speed chuẩn hoá 0..1 (dùng chung animator cho nhiều enemy)
        public string speed01Name = "Speed01";

        public string healthName = "Health";
        public string isGroundedName = "Is Grounded";
        public string onStateChangedName = "On State Changed";

        // Trigger đánh thường
        public string attackName = "Attack";

        // Bool roll (theo setup hiện tại của bạn)
        public string rollAttackName = "Roll";

        public string initializedName = "Initialized";

        #endregion

        #region ===== HASH CACHE =====

        protected int m_stateHash;
        protected int m_lastStateHash;

        protected int m_lateralSpeedHash;
        protected int m_speed01Hash;
        protected int m_verticalSpeedHash;

        protected int m_healthHash;
        protected int m_isGroundedHash;

        protected int m_onStateChangedHash;

        // Trigger đánh thường
        protected int m_attackHash;

        // Bool roll
        protected int m_rollBoolHash;

        protected int m_initializedHash;

        #endregion

        #region ===== RUNTIME CACHE =====

        protected Enemy m_enemy;

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>
        /// Khởi tạo: lấy Enemy, cache hash, đăng ký callback đổi state và init an toàn animator.
        /// </summary>
        protected virtual void Start()
        {
            InitializeEnemy();
            InitializeParametersHash();
            RegisterStateChangeEvent();
            InitializeAnimatorSafely();
        }

        /// <summary>
        /// Hủy đăng ký event để tránh đăng ký lặp khi object bị destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            UnregisterStateChangeEvent();
        }

        /// <summary>
        /// LateUpdate: cập nhật parameter cho Animator sau khi movement đã được tính xong.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!CanUpdateAnimator()) return;
            UpdateAnimatorParameters();
        }

        #endregion

        #region ===== INITIALIZE HELPERS =====

        /// <summary>
        /// Lấy reference Enemy cùng GameObject.
        /// </summary>
        protected virtual void InitializeEnemy()
        {
            m_enemy = GetComponent<Enemy>();
        }

        /// <summary>
        /// Cache hash cho các parameter name để tối ưu hiệu năng.
        /// </summary>
        protected virtual void InitializeParametersHash()
        {
            m_stateHash = Animator.StringToHash(stateName);
            m_lastStateHash = Animator.StringToHash(lastStateName);

            m_lateralSpeedHash = Animator.StringToHash(lateralSpeedName);
            m_speed01Hash = Animator.StringToHash(speed01Name);
            m_verticalSpeedHash = Animator.StringToHash(verticalSpeedName);

            m_healthHash = Animator.StringToHash(healthName);
            m_isGroundedHash = Animator.StringToHash(isGroundedName);

            m_onStateChangedHash = Animator.StringToHash(onStateChangedName);

            m_attackHash = Animator.StringToHash(attackName);
            m_rollBoolHash = Animator.StringToHash(rollAttackName);

            m_initializedHash = Animator.StringToHash(initializedName);
        }

        /// <summary>
        /// Đăng ký lắng nghe event đổi state để bắn trigger "On State Changed".
        /// </summary>
        protected virtual void RegisterStateChangeEvent()
        {
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;
            if (animator == null) return;

            m_enemy.states.events.onChange.AddListener(OnStateChanged);
        }

        /// <summary>
        /// Hủy đăng ký event đổi state.
        /// </summary>
        protected virtual void UnregisterStateChangeEvent()
        {
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;
            m_enemy.states.events.onChange.RemoveListener(OnStateChanged);
        }

        /// <summary>
        /// Kiểm tra đủ điều kiện để update animator mỗi frame.
        /// </summary>
        protected virtual bool CanUpdateAnimator()
        {
            if (animator == null) return false;
            if (m_enemy == null) return false;
            if (m_enemy.health == null) return false;
            if (m_enemy.states == null) return false;

            return true;
        }

        #endregion

        #region ===== PUBLIC API (Enemy gọi cho gọn) =====

        /// <summary>
        /// Bắn trigger đánh thường (NormalHit).
        /// </summary>
        public void TriggerAttack()
        {
            SetTrigger(m_attackHash);
        }

        /// <summary>
        /// Bật/tắt Bool roll (dùng cho RollAttack).
        /// </summary>
        public void SetAttackBool(bool value)
        {
            if (animator == null) return;
            animator.SetBool(m_rollBoolHash, value);
        }

        /// <summary>
        /// Set một trigger bất kỳ theo hash.
        /// </summary>
        public void SetTrigger(int hash)
        {
            if (animator == null) return;
            animator.SetTrigger(hash);
        }

        /// <summary>
        /// Set một bool bất kỳ theo hash.
        /// </summary>
        public void SetBool(int hash, bool value)
        {
            if (animator == null) return;
            animator.SetBool(hash, value);
        }

        #endregion

        #region ===== INTERNAL =====

        /// <summary>
        /// Callback khi state đổi: bắn trigger "On State Changed".
        /// </summary>
        protected virtual void OnStateChanged()
        {
            if (animator == null) return;
            animator.SetTrigger(m_onStateChangedHash);
        }

        /// <summary>
        /// Cập nhật parameter mà Animator Controller cần:
        /// - speed thật (Lateral Speed)
        /// - speed chuẩn hoá 0..1 (Speed01)
        /// - vertical speed, health, grounded, state index
        /// </summary>
        protected virtual void UpdateAnimatorParameters()
        {
            float lateralSpeed = m_enemy.lateralVelocity.magnitude;
            float verticalSpeed = m_enemy.verticalVelocity.y;

            float denom = 1f;
            if (m_enemy.stats != null && m_enemy.stats.current != null)
                denom = Mathf.Max(0.01f, m_enemy.stats.current.followTopSpeed);

            float lateralSpeed01 = Mathf.Clamp01(lateralSpeed / denom);

            animator.SetInteger(m_stateHash, m_enemy.states.index);
            animator.SetInteger(m_lastStateHash, m_enemy.states.lastIndex);

            animator.SetFloat(m_lateralSpeedHash, lateralSpeed);
            animator.SetFloat(m_speed01Hash, lateralSpeed01);
            animator.SetFloat(m_verticalSpeedHash, verticalSpeed);
            animator.SetInteger(m_healthHash, m_enemy.health.current);
            animator.SetBool(m_isGroundedHash, m_enemy.isGrounded);
        }

        /// <summary>
        /// Khởi tạo an toàn: chặn AnyState->Die ở frame đầu bằng bool Initialized,
        /// set param 1 lần ngay lúc start để Health/State không bị mặc định sai.
        /// </summary>
        protected virtual void InitializeAnimatorSafely()
        {
            if (animator == null) return;

            animator.SetBool(m_initializedHash, false);

            if (m_enemy != null && m_enemy.health != null && m_enemy.states != null)
            {
                UpdateAnimatorParameters();
                animator.Update(0f);
            }

            animator.SetBool(m_initializedHash, true);
        }

        #endregion
    }
}