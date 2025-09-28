using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Boss tầm xa - Tấn công từ xa, di chuyển chậm nhưng sát thương cao
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Ranged Boss")]
    public class RangedBoss : BaseBoss
    {
        [Header("Ranged Boss Settings")]
        [Tooltip("Prefab projectile")]
        public GameObject projectilePrefab;
        
        [Tooltip("Tốc độ projectile")]
        public float projectileSpeed = 20f;
        
        [Tooltip("Số lượng projectile mỗi lần bắn")]
        public int projectileCount = 1;
        
        [Tooltip("Góc spread của projectile")]
        public float spreadAngle = 15f;
        
        [Tooltip("Điểm spawn projectile")]
        public Transform projectileSpawnPoint;
        
        [Tooltip("Hiệu ứng bắn")]
        public GameObject shootEffect;
        
        [Tooltip("Hiệu ứng nổ")]
        public GameObject explosionEffect;

        private float m_lastShootTime;
        private int m_burstCount = 0;
        private float m_burstTimer = 0f;

        protected override void Start()
        {
            base.Start();
            InitializeRangedBoss();
        }

        private void InitializeRangedBoss()
        {
            // Khởi tạo các giá trị ban đầu cho Ranged Boss
            m_lastShootTime = 0f;
            m_burstCount = 0;
            m_burstTimer = 0f;
        }

        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();
            
            if (currentPhase == null) return;

            // Ranged attack behavior
            if (CanShoot())
            {
                PerformRangedAttack();
            }

            // Burst fire cho giai đoạn sau
            if (currentPhase.phaseName.Contains("2") || currentPhase.phaseName.Contains("3"))
            {
                UpdateBurstFire();
            }
        }

        /// <summary>
        /// Kiểm tra có thể bắn không
        /// </summary>
        protected virtual bool CanShoot()
        {
            if (player == null) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= attackRange && Time.time >= m_lastShootTime + attackInterval;
        }

        /// <summary>
        /// Thực hiện tấn công tầm xa
        /// </summary>
        protected virtual void PerformRangedAttack()
        {
            m_lastShootTime = Time.time;
            m_isAttacking = true;

            // Bắn projectile
            ShootProjectiles();

            // Hiệu ứng bắn
            if (shootEffect != null)
            {
                Vector3 shootPos = projectileSpawnPoint != null ? 
                    projectileSpawnPoint.position : transform.position;
                Instantiate(shootEffect, shootPos, transform.rotation);
            }

            // Reset trạng thái tấn công
            Invoke(nameof(ResetAttackState), currentPhase.attackSpeed);
        }

        /// <summary>
        /// Bắn projectile
        /// </summary>
        protected virtual void ShootProjectiles()
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = projectileSpawnPoint != null ? 
                projectileSpawnPoint.position : transform.position + Vector3.up;

            for (int i = 0; i < projectileCount; i++)
            {
                // Tính toán hướng bắn với spread
                Vector3 direction = GetShootDirection(i);
                
                // Tạo projectile
                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));
                
                // Set up projectile
                SetupProjectile(projectile, direction);
            }

            Debug.Log($"{GetType().Name} bắn {projectileCount} projectile!");
        }

        /// <summary>
        /// Lấy hướng bắn với spread
        /// </summary>
        protected virtual Vector3 GetShootDirection(int projectileIndex)
        {
            Vector3 baseDirection = (player.position - transform.position).normalized;
            
            if (projectileCount == 1)
            {
                return baseDirection;
            }

            // Tính toán spread angle
            float angle = (projectileIndex - (projectileCount - 1) / 2f) * spreadAngle;
            return Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
        }

        /// <summary>
        /// Thiết lập projectile
        /// </summary>
        protected virtual void SetupProjectile(GameObject projectile, Vector3 direction)
        {
            // Thêm component Projectile nếu cần
            var projectileComponent = projectile.GetComponent<BossProjectile>();
            if (projectileComponent == null)
            {
                projectileComponent = projectile.AddComponent<BossProjectile>();
            }

            // Set up projectile
            projectileComponent.damage = currentPhase.damage;
            projectileComponent.speed = projectileSpeed;
            projectileComponent.direction = direction;
            projectileComponent.explosionEffect = explosionEffect;
        }

        /// <summary>
        /// Cập nhật burst fire
        /// </summary>
        protected virtual void UpdateBurstFire()
        {
            if (m_burstCount > 0)
            {
                m_burstTimer += Time.deltaTime;
                
                if (m_burstTimer >= 0.2f) // Bắn mỗi 0.2s
                {
                    ShootProjectiles();
                    m_burstCount--;
                    m_burstTimer = 0f;
                }
            }
        }

        /// <summary>
        /// Override kỹ năng đặc biệt cho Ranged Boss
        /// </summary>
        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            if (currentPhase.phaseName.Contains("2"))
            {
                // Giai đoạn 2: Triple Shot
                PerformTripleShot();
            }
            else if (currentPhase.phaseName.Contains("3"))
            {
                // Giai đoạn 3: Burst Fire
                PerformBurstFire();
            }
        }

        /// <summary>
        /// Triple Shot - Bắn 3 projectile cùng lúc
        /// </summary>
        protected virtual void PerformTripleShot()
        {
            Debug.Log($"{GetType().Name} sử dụng Triple Shot!");
            
            int originalCount = projectileCount;
            projectileCount = 3;
            ShootProjectiles();
            projectileCount = originalCount;
        }

        /// <summary>
        /// Burst Fire - Bắn liên tục
        /// </summary>
        protected virtual void PerformBurstFire()
        {
            Debug.Log($"{GetType().Name} sử dụng Burst Fire!");
            
            m_burstCount = 5; // Bắn 5 lần
            m_burstTimer = 0f;
        }

        /// <summary>
        /// Override để thêm logic đặc biệt khi chuyển giai đoạn
        /// </summary>
        protected override void OnPhaseChanged(int newPhase)
        {
            base.OnPhaseChanged(newPhase);
            
            // Ranged Boss tăng sát thương và tốc độ bắn ở giai đoạn sau
            if (newPhase >= 1)
            {
                projectileSpeed *= 1.3f;
                attackInterval *= 0.8f;
            }
            
            if (newPhase >= 2)
            {
                projectileCount = 2; // Bắn 2 projectile từ giai đoạn 2
            }
        }
    }

    /// <summary>
    /// Component cho projectile của boss
    /// </summary>
    public class BossProjectile : MonoBehaviour
    {
        public int damage = 10;
        public float speed = 20f;
        public Vector3 direction = Vector3.forward;
        public GameObject explosionEffect;
        
        private void Start()
        {
            // Tự hủy sau 5 giây
            Destroy(gameObject, 5f);
        }
        
        private void Update()
        {
            transform.position += direction * speed * Time.deltaTime;
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player))
            {
                if (other.TryGetComponent<Player>(out var player))
                {
                    player.ApplyDamage(damage, transform.position);
                }
                
                // Hiệu ứng nổ
                if (explosionEffect != null)
                {
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);
                }
                
                Destroy(gameObject);
            }
        }
    }
}
