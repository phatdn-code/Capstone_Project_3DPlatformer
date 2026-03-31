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
        public string speed01Name = "Speed01";

        public string healthName = "Health";
        public string isGroundedName = "Is Grounded";
        public string onStateChangedName = "On State Changed";

        public string attackName = "Attack";          // Trigger đánh thường
        public string rollAttackName = "Roll";        // Bool roll
        public string sprayAttackName = "SprayAttack"; // Bool spray

        public string initializedName = "Initialized";

        #endregion

        #region ===== RUNTIME CACHE =====

        protected Enemy m_enemy;

        #endregion

        #region ===== HASH CACHE =====

        protected int m_stateHash;
        protected int m_lastStateHash;

        protected int m_lateralSpeedHash;
        protected int m_verticalSpeedHash;
        protected int m_speed01Hash;

        protected int m_healthHash;
        protected int m_isGroundedHash;

        protected int m_onStateChangedHash;

        protected int m_attackHash;
        protected int m_rollBoolHash;
        protected int m_sprayBoolHash;

        protected int m_initializedHash;

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>
        /// VN: Cache reference và hash ngay từ Awake.
        /// Chỉ chạy 1 lần khi object được tạo.
        /// </summary>
        protected virtual void Awake()
        {
            CacheEnemy();
            CacheHashes();
        }

        /// <summary>
        /// VN: Mỗi lần object được bật lại thì đăng ký event
        /// và khởi tạo lại Animator cho an toàn.
        /// </summary>
        protected virtual void OnEnable()
        {
            RegisterStateChangeEvent();
            InitializeAnimatorSafely();
        }

        /// <summary>
        /// VN: Khi object bị tắt thì hủy đăng ký event,
        /// tránh đăng ký trùng khi bật lại.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnregisterStateChangeEvent();
        }

        /// <summary>
        /// VN: Cập nhật parameter Animator sau khi movement chạy xong.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!CanUpdateAnimator()) return;
            UpdateAnimatorParameters();
        }

        #endregion

        #region ===== INITIALIZE =====

        /// <summary>Cache Enemy cùng GameObject.</summary>
        protected virtual void CacheEnemy()
        {
            m_enemy = GetComponent<Enemy>();
        }

        /// <summary>Cache hash các parameter để tối ưu.</summary>
        protected virtual void CacheHashes()
        {
            m_stateHash = Animator.StringToHash(stateName);
            m_lastStateHash = Animator.StringToHash(lastStateName);

            m_lateralSpeedHash = Animator.StringToHash(lateralSpeedName);
            m_verticalSpeedHash = Animator.StringToHash(verticalSpeedName);
            m_speed01Hash = Animator.StringToHash(speed01Name);

            m_healthHash = Animator.StringToHash(healthName);
            m_isGroundedHash = Animator.StringToHash(isGroundedName);

            m_onStateChangedHash = Animator.StringToHash(onStateChangedName);

            m_attackHash = Animator.StringToHash(attackName);
            m_rollBoolHash = Animator.StringToHash(rollAttackName);
            m_sprayBoolHash = Animator.StringToHash(sprayAttackName);

            m_initializedHash = Animator.StringToHash(initializedName);
        }

        /// <summary>Đăng ký event đổi state để bắn trigger OnStateChanged.</summary>
        protected virtual void RegisterStateChangeEvent()
        {
            if (animator == null) return;
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;

            m_enemy.states.events.onChange.AddListener(OnStateChanged);
        }

        /// <summary>Hủy đăng ký event đổi state.</summary>
        protected virtual void UnregisterStateChangeEvent()
        {
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;
            m_enemy.states.events.onChange.RemoveListener(OnStateChanged);
        }

        /// <summary>Check đủ điều kiện để update animator.</summary>
        protected virtual bool CanUpdateAnimator()
        {
            if (animator == null) return false;
            if (m_enemy == null) return false;
            if (m_enemy.health == null) return false;
            if (m_enemy.states == null) return false;

            return true;
        }

        #endregion

        #region ===== PUBLIC API =====

        /// <summary>Bắn trigger đánh thường.</summary>
        public void TriggerAttack()
        {
            SetTrigger(m_attackHash);
        }

        /// <summary>Giữ tên cũ để không break code đang gọi: thực chất là bool Roll.</summary>
        public void SetAttackBool(bool value)
        {
            SetRollAttackBool(value);
        }

        /// <summary>Bật/tắt bool RollAttack.</summary>
        public void SetRollAttackBool(bool value)
        {
            SetBool(m_rollBoolHash, value);
        }

        /// <summary>Bật/tắt bool SprayAttack.</summary>
        public void SetSprayAttackBool(bool value)
        {
            SetBool(m_sprayBoolHash, value);
        }

        /// <summary>Set trigger theo hash.</summary>
        public void SetTrigger(int hash)
        {
            if (animator == null) return;
            animator.SetTrigger(hash);
        }

        /// <summary>Set bool theo hash.</summary>
        public void SetBool(int hash, bool value)
        {
            if (animator == null) return;
            animator.SetBool(hash, value);
        }

        #endregion

        #region ===== INTERNAL =====

        /// <summary>Callback đổi state: bắn trigger OnStateChanged.</summary>
        protected virtual void OnStateChanged()
        {
            if (animator == null) return;
            animator.SetTrigger(m_onStateChangedHash);
        }

        /// <summary>Update param animation: speed, health, grounded, state.</summary>
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

        /// <summary>Init an toàn: set param 1 lần để tránh transition sai ở frame đầu.</summary>
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