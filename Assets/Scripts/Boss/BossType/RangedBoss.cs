using UnityEngine;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Ranged Boss – chuyên tấn công từ xa với projectile.
    /// Có kỹ năng đặc biệt: Triple Shot, Burst Fire.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Ranged Boss")]
    public class RangedBoss : BaseBoss
    {
        [Header("Ranged Boss Settings")]
        [Tooltip("Prefab projectile bắn ra")]
        public GameObject projectilePrefab;

        [Tooltip("Tốc độ bay của projectile")]
        public float projectileSpeed = 20f;

        [Tooltip("Số lượng projectile mỗi lần bắn")]
        public int projectileCount = 1;

        [Tooltip("Góc spread của projectile")]
        public float spreadAngle = 15f;

        [Tooltip("Điểm spawn projectile")]
        public Transform projectileSpawnPoint;

        [Tooltip("Hiệu ứng khi bắn")]
        public GameObject shootEffect;

        [Tooltip("Hiệu ứng nổ của projectile")]
        public GameObject explosionEffect;

        // Runtime
        private float m_lastShootTime;
        private int m_burstCount = 0;
        private float m_burstTimer = 0f;

        private Tween m_attackTween;

        #region === Unity Lifecycle ===

        protected override void Start()
        {
            base.Start();
            InitializeRangedBoss();
        }

        private void InitializeRangedBoss()
        {
            m_lastShootTime = 0f;
            m_burstCount = 0;
            m_burstTimer = 0f;
        }

        #endregion

        #region === Boss Behavior ===

        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();

            if (currentPhase == null) return;

            // Kiểm tra bắn tấn công thường
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

        #endregion

        #region === Combat Logic ===

        /// <summary>
        /// Thực hiện bắn tầm xa
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

            // Reset trạng thái tấn công bằng DOTween
            m_attackTween?.Kill();
            m_attackTween = DOVirtual.DelayedCall(currentPhase.attackSpeed, () =>
            {
                ResetAttackState();
            });
        }

        /// <summary>
        /// Tạo và bắn các projectile
        /// </summary>
        protected virtual void ShootProjectiles()
        {
            if (projectilePrefab == null) return;

            Vector3 spawnPos = projectileSpawnPoint != null ?
                projectileSpawnPoint.position : transform.position + Vector3.up;

            for (int i = 0; i < projectileCount; i++)
            {
                Vector3 direction = GetShootDirection(i);
                GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(direction));

                SetupProjectile(projectile, direction);
            }

            Debug.Log($"{GetType().Name} bắn {projectileCount} projectile!");
        }

        /// <summary>
        /// Lấy hướng bắn với spread angle
        /// </summary>
        protected virtual Vector3 GetShootDirection(int projectileIndex)
        {
            Vector3 baseDirection = (player.position - transform.position).normalized;

            if (projectileCount == 1)
                return baseDirection;

            float angle = (projectileIndex - (projectileCount - 1) / 2f) * spreadAngle;
            return Quaternion.AngleAxis(angle, Vector3.up) * baseDirection;
        }

        /// <summary>
        /// Thiết lập thông số cho projectile
        /// </summary>
        protected virtual void SetupProjectile(GameObject projectile, Vector3 direction)
        {
            var projectileComponent = projectile.GetComponent<BossProjectile>();

            if (projectileComponent == null)
                projectileComponent = projectile.AddComponent<BossProjectile>();

            projectileComponent.damage = currentPhase.damage;
            projectileComponent.speed = projectileSpeed;
            projectileComponent.direction = direction;
            projectileComponent.explosionEffect = explosionEffect;
        }

        /// <summary>
        /// Burst Fire – bắn liên tục trong thời gian ngắn
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

        #endregion

        #region === Special Ability ===

        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            if (currentPhase.phaseName.Contains("2"))
                PerformTripleShot();

            else if (currentPhase.phaseName.Contains("3"))
                PerformBurstFire();
        }

        /// <summary>
        /// Triple Shot – bắn 3 projectile cùng lúc
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
        /// Burst Fire – chuẩn bị bắn nhiều loạt liên tiếp
        /// </summary>
        protected virtual void PerformBurstFire()
        {
            Debug.Log($"{GetType().Name} sử dụng Burst Fire!");
            m_burstCount = 5; // bắn 5 lần
            m_burstTimer = 0f;
        }

        #endregion

        #region === Event Handlers ===

        protected override void OnPhaseChanged(int newPhase)
        {
            base.OnPhaseChanged(newPhase);

            if (newPhase >= 1)
            {
                projectileSpeed *= 1.3f;
                attackInterval *= 0.8f;
            }

            if (newPhase >= 2)
            {
                projectileCount = 2;
            }
        }

        private void OnDestroy()
        {
            m_attackTween?.Kill();
        }

        #endregion
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
            Destroy(gameObject, 5f); // tự hủy sau 5 giây
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
                    player.ApplyDamage(damage, transform.position);

                if (explosionEffect != null)
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);

                Destroy(gameObject);
            }
        }
    }
}
