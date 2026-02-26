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
        public string lateralSpeedName = "Lateral Speed";
        public string verticalSpeedName = "Vertical Speed";
        public string healthName = "Health";
        public string isGroundedName = "Is Grounded";
        public string onStateChangedName = "On State Changed";

        // ✅ Trigger đánh thường: đổi sang "Hit" (vì "Attack" sẽ là Bool cho roll)
        public string attackName = "Attack";

        // ✅ Bool cho roll theo yêu cầu: animator.SetBool("Attack", true/false)
        public string rollAttackName = "Roll";

        public string initializedName = "Initialized";

        #endregion

        #region ===== HASH CACHE =====

        protected int m_stateHash;
        protected int m_lastStateHash;
        protected int m_lateralSpeedHash;
        protected int m_verticalSpeedHash;
        protected int m_healthHash;
        protected int m_isGroundedHash;
        protected int m_onStateChangedHash;

        // Trigger đánh thường
        protected int m_attackHash;

        // Bool roll ("Attack")
        protected int m_attackBoolHash;

        protected int m_initializedHash;

        #endregion

        #region ===== RUNTIME CACHE =====

        protected Enemy m_enemy;

        #endregion

        #region ===== INITIALIZE =====

        /// <summary>
        /// Lấy reference Enemy cùng GameObject.
        /// </summary>
        protected virtual void InitializeEnemy()
        {
            m_enemy = GetComponent<Enemy>();
        }

        /// <summary>
        /// Cache hash cho các parameter name (tối ưu hiệu năng so với dùng string mỗi frame).
        /// </summary>
        protected virtual void InitializeParametersHash()
        {
            m_stateHash = Animator.StringToHash(stateName);
            m_lastStateHash = Animator.StringToHash(lastStateName);
            m_lateralSpeedHash = Animator.StringToHash(lateralSpeedName);
            m_verticalSpeedHash = Animator.StringToHash(verticalSpeedName);
            m_healthHash = Animator.StringToHash(healthName);
            m_isGroundedHash = Animator.StringToHash(isGroundedName);
            m_onStateChangedHash = Animator.StringToHash(onStateChangedName);

            // Attack
            m_attackHash = Animator.StringToHash(attackName);
            m_attackBoolHash = Animator.StringToHash(rollAttackName);
            // -----------------

            m_initializedHash = Animator.StringToHash(initializedName);
        }

        /// <summary>
        /// Đăng ký lắng nghe event đổi state để bắn trigger "On State Changed".
        /// </summary>
        protected virtual void InitializeAnimatorTriggers()
        {
            if (animator == null || m_enemy == null || m_enemy.states == null || m_enemy.states.events == null)
                return;

            m_enemy.states.events.onChange.AddListener(OnStateChanged);
        }

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>
        /// Start: init enemy, cache hash, và đăng ký trigger khi state đổi.
        /// </summary>
        protected virtual void Start()
        {
            InitializeEnemy();
            InitializeParametersHash();
            InitializeAnimatorTriggers();
            InitializeAnimatorSafely();
        }

        /// <summary>
        /// Hủy đăng ký event để tránh đăng ký lặp khi object bị destroy.
        /// </summary>
        protected virtual void OnDestroy()
        {
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;
            m_enemy.states.events.onChange.RemoveListener(OnStateChanged);
        }

        /// <summary>
        /// LateUpdate: cập nhật parameter cho Animator sau khi movement đã được tính xong.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (animator == null || m_enemy == null) return;
            if (m_enemy.health == null) return;
            if (m_enemy.states == null) return;

            UpdateAnimatorParameters();
        }

        #endregion

        #region ===== PUBLIC API (Enemy gọi cho gọn) =====

        /// <summary>
        /// Bắn trigger đánh thường (NormalHit). Trigger này là "Hit" (không phải "Attack").
        /// </summary>
        public void TriggerAttack()
        {
            SetTrigger(m_attackHash);
        }

        /// <summary>
        /// Bật/tắt Bool "Attack" (dùng cho RollAttack theo yêu cầu).
        /// </summary>
        public void SetAttackBool(bool value)
        {
            if (animator == null) return;
            animator.SetBool(m_attackBoolHash, value);
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

        #region ===== INTERNAL HELPERS =====

        /// <summary>
        /// Callback khi state đổi: bắn trigger "On State Changed".
        /// </summary>
        protected virtual void OnStateChanged()
        {
            if (animator == null) return;
            animator.SetTrigger(m_onStateChangedHash);
        }

        /// <summary>
        /// Cập nhật tất cả parameter mà Animator Controller cần (state, speed, health, grounded).
        /// </summary>
        protected virtual void UpdateAnimatorParameters()
        {
            var lateralSpeed = m_enemy.lateralVelocity.magnitude;
            var verticalSpeed = m_enemy.verticalVelocity.y;

            animator.SetInteger(m_stateHash, m_enemy.states.index);
            animator.SetInteger(m_lastStateHash, m_enemy.states.lastIndex);
            animator.SetFloat(m_lateralSpeedHash, lateralSpeed);
            animator.SetFloat(m_verticalSpeedHash, verticalSpeed);
            animator.SetInteger(m_healthHash, m_enemy.health.current);
            animator.SetBool(m_isGroundedHash, m_enemy.isGrounded);
        }

        /// <summary>
        /// Khởi tạo an toàn: chặn AnyState->Die ở frame đầu bằng bool Initialized,
        /// set param 1 lần ngay lúc start để Health không còn mặc định 0.
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