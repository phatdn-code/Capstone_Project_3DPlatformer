using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Boss cận chiến - Tấn công gần, di chuyển nhanh
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Melee Boss")]
    public class MeleeBoss : BaseBoss
    {
        [Header("Melee Boss Settings")]
        [Tooltip("Tốc độ rush tới player")]
        public float rushSpeed = 15f;
        
        [Tooltip("Khoảng cách rush")]
        public float rushDistance = 8f;
        
        [Tooltip("Thời gian rush")]
        public float rushDuration = 1f;
        
        [Tooltip("Hiệu ứng rush")]
        public GameObject rushEffect;
        
        [Tooltip("Hiệu ứng tấn công cận chiến")]
        public GameObject meleeAttackEffect;

        private bool m_isRushing = false;
        private Vector3 m_rushTarget;
        private float m_rushStartTime;

        protected override void Start()
        {
            base.Start();
            InitializeMeleeBoss();
        }

        private void InitializeMeleeBoss()
        {
            // Khởi tạo các giá trị ban đầu cho Melee Boss
            m_isRushing = false;
            m_rushTarget = Vector3.zero;
            m_rushStartTime = 0f;
        }

        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();
            
            if (currentPhase == null) return;

            // Rush behavior cho Melee Boss
            if (CanRush())
            {
                StartRush();
            }

            // Cập nhật rush nếu đang trong quá trình rush
            if (m_isRushing)
            {
                UpdateRush();
            }
        }

        /// <summary>
        /// Kiểm tra có thể rush không
        /// </summary>
        protected virtual bool CanRush()
        {
            if (player == null) return false;
            if (m_isRushing) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= rushDistance && distance > attackRange;
        }

        /// <summary>
        /// Bắt đầu rush tới player
        /// </summary>
        protected virtual void StartRush()
        {
            m_isRushing = true;
            m_rushTarget = player.position;
            m_rushStartTime = Time.time;

            Debug.Log($"{GetType().Name} bắt đầu rush tới player!");
            
            // Hiệu ứng rush
            if (rushEffect != null)
            {
                Instantiate(rushEffect, transform.position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Cập nhật quá trình rush
        /// </summary>
        protected virtual void UpdateRush()
        {
            if (Time.time - m_rushStartTime >= rushDuration)
            {
                EndRush();
                return;
            }

            // Di chuyển tới target với tốc độ rush
            Vector3 direction = (m_rushTarget - transform.position).normalized;
            transform.position += direction * rushSpeed * Time.deltaTime;

            // Face direction
            FaceDirectionSmooth(direction);
        }

        /// <summary>
        /// Kết thúc rush
        /// </summary>
        protected virtual void EndRush()
        {
            m_isRushing = false;
            Debug.Log($"{GetType().Name} kết thúc rush!");
        }

        /// <summary>
        /// Override tấn công cho Melee Boss
        /// </summary>
        protected override void PerformAttack()
        {
            base.PerformAttack();

            // Hiệu ứng tấn công cận chiến
            if (meleeAttackEffect != null)
            {
                Vector3 attackPos = transform.position + transform.forward * 2f;
                Instantiate(meleeAttackEffect, attackPos, transform.rotation);
            }

            // Tạo shockwave effect
            CreateShockwave();
        }

        /// <summary>
        /// Tạo shockwave khi tấn công
        /// </summary>
        protected virtual void CreateShockwave()
        {
            // Tìm tất cả enemies trong phạm vi
            Collider[] enemies = Physics.OverlapSphere(transform.position, attackRange * 1.5f);
            
            foreach (var enemy in enemies)
            {
                if (enemy.CompareTag(GameTags.Player))
                {
                    // Push back player
                    Vector3 pushDirection = (enemy.transform.position - transform.position).normalized;
                    if (enemy.TryGetComponent<Player>(out var player))
                    {
                        // Apply knockback force
                        player.controller.Move(pushDirection * 5f);
                    }
                }
            }
        }

        /// <summary>
        /// Override kỹ năng đặc biệt cho Melee Boss
        /// </summary>
        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            // Kỹ năng đặc biệt: Berserker Rage
            if (currentPhase.phaseName.Contains("2") || currentPhase.phaseName.Contains("3"))
            {
                StartBerserkerRage();
            }
        }

        /// <summary>
        /// Kích hoạt Berserker Rage
        /// </summary>
        protected virtual void StartBerserkerRage()
        {
            Debug.Log($"{GetType().Name} kích hoạt Berserker Rage!");
            
            // Tăng tốc độ tấn công tạm thời
            attackInterval *= 0.5f;
            Invoke(nameof(EndBerserkerRage), 5f);
        }

        /// <summary>
        /// Kết thúc Berserker Rage
        /// </summary>
        protected virtual void EndBerserkerRage()
        {
            attackInterval /= 0.5f;
            Debug.Log($"{GetType().Name} kết thúc Berserker Rage!");
        }

        /// <summary>
        /// Override để thêm logic đặc biệt khi chuyển giai đoạn
        /// </summary>
        protected override void OnPhaseChanged(int newPhase)
        {
            base.OnPhaseChanged(newPhase);
            
            // Melee Boss trở nên hung dữ hơn ở giai đoạn sau
            if (newPhase >= 1)
            {
                rushSpeed *= 1.5f;
                attackRange *= 1.2f;
            }
        }
    }
}
