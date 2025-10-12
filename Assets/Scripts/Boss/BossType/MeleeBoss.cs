using UnityEngine;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Melee Boss – cận chiến, lao vào và gây sát thương lớn ở khoảng gần.
    /// Có kỹ năng đặc biệt: Rush Attack, Shockwave, Berserker Rage.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Melee Boss")]
    public class MeleeBoss : BaseBoss
    {
        [Header("Melee Boss Settings")]
        [Tooltip("Tốc độ lao vào")]
        public float rushSpeed = 10f;

        [Tooltip("Prefab Shockwave")]
        public GameObject shockwavePrefab;

        [Tooltip("Điểm spawn shockwave")]
        public Transform shockwaveSpawnPoint;

        [Tooltip("Hiệu ứng khi kích hoạt Berserker Rage")]
        public GameObject berserkerEffect;

        private float m_lastRushTime;
        private bool m_isBerserkerActive = false;
        private Tween m_attackTween;
        private Tween m_specialTween;

        #region === Unity Lifecycle ===

        protected override void Start()
        {
            base.Start();
            InitializeMeleeBoss();
        }

        private void InitializeMeleeBoss()
        {
            m_lastRushTime = 0f;
            m_isBerserkerActive = false;
        }

        #endregion

        #region === Boss Behavior ===

        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();

            if (currentPhase == null) return;

            // Thực hiện tấn công cận chiến
            if (CanPerformRush())
                PerformRushAttack();
        }

        /// <summary>
        /// Kiểm tra có thể thực hiện Rush Attack
        /// </summary>
        private bool CanPerformRush()
        {
            if (player == null) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= attackRange && Time.time >= m_lastRushTime + attackInterval;
        }

        #endregion

        #region === Combat Logic ===

        /// <summary>
        /// Tấn công lao vào mục tiêu
        /// </summary>
        private void PerformRushAttack()
        {
            m_lastRushTime = Time.time;
            m_isAttacking = true;

            Vector3 direction = (player.position - transform.position).normalized;
            transform.DOMove(transform.position + direction * rushSpeed, 0.3f)
                .SetEase(Ease.OutQuad);

            Debug.Log($"{GetType().Name} thực hiện Rush Attack!");

            // Reset trạng thái tấn công bằng DOTween thay cho Invoke
            m_attackTween?.Kill();
            m_attackTween = DOVirtual.DelayedCall(currentPhase.attackSpeed, () =>
            {
                ResetAttackState();
            });
        }

        /// <summary>
        /// Tạo shockwave tấn công diện rộng
        /// </summary>
        private void PerformShockwave()
        {
            if (shockwavePrefab == null) return;

            Vector3 spawnPos = shockwaveSpawnPoint != null ? shockwaveSpawnPoint.position : transform.position;
            Instantiate(shockwavePrefab, spawnPos, Quaternion.identity);

            Debug.Log($"{GetType().Name} tạo Shockwave!");
        }

        /// <summary>
        /// Berserker Rage – tăng sức mạnh trong thời gian ngắn
        /// </summary>
        private void PerformBerserkerRage()
        {
            if (m_isBerserkerActive) return; // tránh spam
            m_isBerserkerActive = true;

            if (berserkerEffect != null)
                Instantiate(berserkerEffect, transform.position, Quaternion.identity);

            // Tăng damage và tốc độ
            currentPhase.damage = Mathf.RoundToInt(currentPhase.damage * 1.5f);
            attackInterval *= 0.7f;

            Debug.Log($"{GetType().Name} kích hoạt Berserker Rage!");

            // Sau 5 giây trở lại bình thường
            m_specialTween?.Kill();
            m_specialTween = DOVirtual.DelayedCall(5f, () =>
            {
                EndBerserkerRage();
            });
        }

        private void EndBerserkerRage()
        {
            m_isBerserkerActive = false;
            Debug.Log($"{GetType().Name} kết thúc Berserker Rage!");
        }

        #endregion

        #region === Special Ability ===

        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            if (currentPhase.phaseName.Contains("2"))
                PerformShockwave();

            else if (currentPhase.phaseName.Contains("3"))
                PerformBerserkerRage();
        }

        #endregion

        #region === Cleanup ===

        private void OnDestroy()
        {
            m_attackTween?.Kill();
            m_specialTween?.Kill();
        }

        #endregion
    }
}
