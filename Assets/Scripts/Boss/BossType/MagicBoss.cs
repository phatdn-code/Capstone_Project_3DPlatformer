using UnityEngine;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Magic Boss – chuyên dùng phép thuật: Teleport, Spell Cast, Ultimate Spell.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Magic Boss")]
    public class MagicBoss : BaseBoss
    {
        [Header("Magic Boss Settings")]
        [Tooltip("Prefab phép thuật bắn ra")]
        public GameObject spellPrefab;

        [Tooltip("Prefab Ultimate Spell (đòn tất sát)")]
        public GameObject ultimateSpellPrefab;

        [Tooltip("Điểm spawn phép thuật")]
        public Transform spellSpawnPoint;

        [Tooltip("Hiệu ứng khi teleport")]
        public GameObject teleportEffect;

        [Tooltip("Khoảng cách tối thiểu/tối đa khi teleport")]
        public float teleportMinDistance = 5f;
        public float teleportMaxDistance = 10f;

        // Runtime
        private float m_lastSpellTime;
        private Tween m_attackTween;   // quản lý delay cho attack
        private Tween m_spellTween;    // quản lý delay cho spell
        private Tween m_ultimateTween; // quản lý delay cho ultimate

        #region === Unity Lifecycle ===

        protected override void Start()
        {
            base.Start();
            InitializeMagicBoss();
        }

        private void InitializeMagicBoss()
        {
            m_lastSpellTime = 0f;
        }

        #endregion

        #region === Boss Behavior ===

        /// <summary>
        /// Override behavior cập nhật mỗi frame
        /// </summary>
        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();

            if (currentPhase == null) return;

            // Tấn công cơ bản (spell) nếu có thể
            if (CanCastSpell())
                CastSpell();
        }

        /// <summary>
        /// Kiểm tra có thể cast spell hay không
        /// </summary>
        protected virtual bool CanCastSpell()
        {
            if (player == null) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= attackRange && Time.time >= m_lastSpellTime + attackInterval;
        }

        #endregion

        #region === Combat Logic ===

        /// <summary>
        /// Thực hiện phép thuật cơ bản
        /// </summary>
        protected virtual void CastSpell()
        {
            m_lastSpellTime = Time.time;
            m_isAttacking = true;

            // Tạo spell projectile
            Vector3 spawnPos = spellSpawnPoint != null ? spellSpawnPoint.position : transform.position + Vector3.up;
            GameObject spell = Instantiate(spellPrefab, spawnPos, Quaternion.identity);

            if (spell.TryGetComponent<BossProjectile>(out var proj))
            {
                proj.damage = currentPhase.damage;
                proj.speed = 15f;
                proj.direction = (player.position - transform.position).normalized;
            }

            // Reset attack state bằng DOTween thay vì Invoke
            m_attackTween?.Kill();
            m_attackTween = DOVirtual.DelayedCall(currentPhase.attackSpeed, () =>
            {
                ResetAttackState();
            });
        }

        /// <summary>
        /// Teleport Boss đến vị trí ngẫu nhiên quanh player
        /// </summary>
        protected virtual void PerformTeleport()
        {
            if (teleportEffect != null)
                Instantiate(teleportEffect, transform.position, Quaternion.identity);

            Vector3 randomDir = Random.insideUnitSphere;
            randomDir.y = 0;
            float distance = Random.Range(teleportMinDistance, teleportMaxDistance);
            Vector3 newPos = player.position + randomDir.normalized * distance;

            transform.position = newPos;

            if (teleportEffect != null)
                Instantiate(teleportEffect, newPos, Quaternion.identity);
        }

        /// <summary>
        /// Ultimate Spell – chiêu mạnh nhất
        /// </summary>
        protected virtual void PerformUltimateSpell()
        {
            if (ultimateSpellPrefab == null) return;

            Vector3 spawnPos = transform.position + Vector3.up * 2f;
            GameObject ultimate = Instantiate(ultimateSpellPrefab, spawnPos, Quaternion.identity);

            if (ultimate.TryGetComponent<BossProjectile>(out var proj))
            {
                proj.damage = currentPhase.damage * 3;
                proj.speed = 10f;
                proj.direction = (player.position - transform.position).normalized;
            }
        }

        #endregion

        #region === Special Ability ===

        /// <summary>
        /// Override kỹ năng đặc biệt
        /// </summary>
        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            // Giai đoạn 2: Teleport spam
            if (currentPhase.phaseName.Contains("2"))
                PerformTeleport();

            // Giai đoạn 3: Ultimate Spell
            else if (currentPhase.phaseName.Contains("3"))
                PerformUltimateSpell();
        }

        #endregion

        #region === Cleanup ===

        private void OnDestroy()
        {
            // Hủy tween khi Boss bị xóa
            m_attackTween?.Kill();
            m_spellTween?.Kill();
            m_ultimateTween?.Kill();
        }

        #endregion
    }
}
