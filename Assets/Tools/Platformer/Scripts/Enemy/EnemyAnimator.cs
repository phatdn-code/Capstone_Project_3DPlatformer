using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Enemy))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy Animator")]
    public class EnemyAnimator : MonoBehaviour
    {
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

        public string attackName = "Attack";
        public string rollAttackName = "Roll";
        public string sprayAttackName = "SprayAttack";

        public string initializedName = "Initialized";

        protected Enemy m_enemy;

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

        private Coroutine m_initializeCoroutine;

        /// <summary>
        /// VN: Cache reference và hash 1 lần.
        /// </summary>
        protected virtual void Awake()
        {
            CacheEnemy();
            CacheHashes();
        }

        /// <summary>
        /// VN: Khi object bật lại, chưa init Animator ngay.
        /// Chờ 1 frame để Health/State kịp khởi tạo xong rồi mới sync.
        /// </summary>
        protected virtual void OnEnable()
        {
            RegisterStateChangeEvent();

            if (animator != null)
                animator.SetBool(m_initializedHash, false);

            if (m_initializeCoroutine != null)
                StopCoroutine(m_initializeCoroutine);

            m_initializeCoroutine = StartCoroutine(InitializeAnimatorNextFrame());
        }

        /// <summary>
        /// VN: Khi object tắt thì hủy event và reset cờ init.
        /// </summary>
        protected virtual void OnDisable()
        {
            UnregisterStateChangeEvent();

            if (m_initializeCoroutine != null)
            {
                StopCoroutine(m_initializeCoroutine);
                m_initializeCoroutine = null;
            }

            if (animator != null)
                animator.SetBool(m_initializedHash, false);
        }

        /// <summary>
        /// VN: Chờ 1 frame rồi mới init Animator để tránh build bị sync Health = 0 quá sớm.
        /// </summary>
        private IEnumerator InitializeAnimatorNextFrame()
        {
            yield return null;

            InitializeAnimatorSafely();
            m_initializeCoroutine = null;
        }

        /// <summary>
        /// VN: Update parameter Animator sau khi movement chạy xong.
        /// </summary>
        protected virtual void LateUpdate()
        {
            if (!CanUpdateAnimator()) return;
            UpdateAnimatorParameters();
        }

        protected virtual void CacheEnemy()
        {
            m_enemy = GetComponent<Enemy>();
        }

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

        protected virtual void RegisterStateChangeEvent()
        {
            if (animator == null) return;
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;

            m_enemy.states.events.onChange.AddListener(OnStateChanged);
        }

        protected virtual void UnregisterStateChangeEvent()
        {
            if (m_enemy == null || m_enemy.states == null || m_enemy.states.events == null) return;
            m_enemy.states.events.onChange.RemoveListener(OnStateChanged);
        }

        protected virtual bool CanUpdateAnimator()
        {
            if (animator == null) return false;
            if (m_enemy == null) return false;
            if (m_enemy.health == null) return false;
            if (m_enemy.states == null) return false;

            return true;
        }

        public void TriggerAttack()
        {
            SetTrigger(m_attackHash);
        }

        public void SetAttackBool(bool value)
        {
            SetRollAttackBool(value);
        }

        public void SetRollAttackBool(bool value)
        {
            SetBool(m_rollBoolHash, value);
        }

        public void SetSprayAttackBool(bool value)
        {
            SetBool(m_sprayBoolHash, value);
        }

        public void SetTrigger(int hash)
        {
            if (animator == null) return;
            animator.SetTrigger(hash);
        }

        public void SetBool(int hash, bool value)
        {
            if (animator == null) return;
            animator.SetBool(hash, value);
        }

        protected virtual void OnStateChanged()
        {
            if (animator == null) return;
            animator.SetTrigger(m_onStateChangedHash);
        }

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
        /// VN: Khởi tạo Animator an toàn sau khi dữ liệu gameplay đã sẵn sàng.
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
    }
}