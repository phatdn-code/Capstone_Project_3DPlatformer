using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Quản lý máu, phase và trạng thái sống/chết của boss.
    /// Đây là component bắt buộc cho mọi boss.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Health")]
    public class BossHealth : MonoBehaviour
    {
        // ───────────────────────────────────────────────
        // Serialized Fields
        // ───────────────────────────────────────────────
        [Header("Boss Health Settings")]
        [Tooltip("Máu ban đầu của boss")]
        public int initialHealth = 300;

        [Tooltip("Thời gian chuyển giai đoạn (delay hồi phục)")]
        public float phaseTransitionTime = 3f;

        [Tooltip("Hiệu ứng hồi phục khi boss heal đầy máu")]
        public GameObject healEffect;

        [Header("Events")]
        [Tooltip("Được gọi khi boss chuyển sang giai đoạn mới")]
        public UnityEvent<int> OnPhaseChanged;

        [Tooltip("Được gọi khi boss hồi phục máu")]
        public UnityEvent OnBossHealed;

        [Tooltip("Được gọi khi boss chết hoàn toàn")]
        public UnityEvent OnBossDefeated;

        // ───────────────────────────────────────────────
        // Private Fields
        // ───────────────────────────────────────────────
        private int m_currentHealth;
        private int m_currentPhase = 0;
        private bool m_isTransitioning = false;
        private bool m_isDead = false;

        // ───────────────────────────────────────────────
        // Properties
        // ───────────────────────────────────────────────

        /// <summary> Máu hiện tại của boss (clamped trong [0, initialHealth]) </summary>
        public int currentHealth
        {
            get => m_currentHealth;
            private set
            {
                m_currentHealth = Mathf.Clamp(value, 0, initialHealth);

                // Nếu máu về 0 và chưa chết -> xử lý chuyển phase
                if (m_currentHealth <= 0 && !m_isDead)
                    HandlePhaseTransition();
            }
        }

        /// <summary> Giai đoạn hiện tại của boss (0, 1, 2) </summary>
        public int currentPhase => m_currentPhase;

        /// <summary> Boss đã chết hoàn toàn chưa </summary>
        public bool isDead => m_isDead;

        /// <summary> Boss có đang chuyển phase không </summary>
        public bool isTransitioning => m_isTransitioning;

        /// <summary> Tỷ lệ máu hiện tại (0–1) </summary>
        public float healthPercentage => (float)currentHealth / initialHealth;

        // ───────────────────────────────────────────────
        // Unity Lifecycle
        // ───────────────────────────────────────────────

        /// <summary> Khi bắt đầu, thiết lập máu ban đầu </summary>
        private void Start()
        {
            m_currentHealth = initialHealth;
        }

        // ───────────────────────────────────────────────
        // Public Methods
        // ───────────────────────────────────────────────

        /// <summary>
        /// Gây sát thương cho boss.
        /// Nếu boss đang chết hoặc đang chuyển phase thì bỏ qua.
        /// </summary>
        public void TakeDamage(int damage)
        {
            if (m_isDead || m_isTransitioning) return;

            currentHealth -= damage;
            Debug.Log($"Boss nhận {damage} sát thương. Máu còn lại: {currentHealth}");
        }

        /// <summary>
        /// Hồi một lượng máu cho boss (gọi event OnBossHealed).
        /// </summary>
        public void Heal(int amount)
        {
            if (m_isDead) return;

            currentHealth += amount;
            OnBossHealed?.Invoke();

            if (healEffect != null)
                Instantiate(healEffect, transform.position, Quaternion.identity);
        }

        /// <summary>
        /// Hồi phục toàn bộ máu (full heal).
        /// </summary>
        public void FullHeal()
        {
            currentHealth = initialHealth;
            OnBossHealed?.Invoke();
        }

        /// <summary>
        /// Reset boss về trạng thái ban đầu (máu, phase, trạng thái sống).
        /// </summary>
        public void ResetBoss()
        {
            m_currentHealth = initialHealth;
            m_currentPhase = 0;
            m_isDead = false;
            m_isTransitioning = false;
        }

        // ───────────────────────────────────────────────
        // Private Logic
        // ───────────────────────────────────────────────

        /// <summary>
        /// Xử lý khi boss hết máu ở một phase.
        /// Nếu đã qua tất cả phase → DefeatBoss().
        /// Ngược lại → bắt đầu coroutine chuyển phase.
        /// </summary>
        private void HandlePhaseTransition()
        {
            if (m_currentPhase >= 2) // 0,1,2 = 3 phase
            {
                DefeatBoss();
                return;
            }

            StartCoroutine(TransitionToNextPhase());
        }

        /// <summary>
        /// Coroutine chuyển sang giai đoạn tiếp theo:
        /// - Tạm dừng (isTransitioning = true).
        /// - Đợi một khoảng (phaseTransitionTime).
        /// - Hồi full máu.
        /// - Kích hoạt event OnPhaseChanged.
        /// </summary>
        private IEnumerator TransitionToNextPhase()
        {
            m_isTransitioning = true;
            m_currentPhase++;

            Debug.Log($"Boss chuyển sang giai đoạn {m_currentPhase + 1}!");

            yield return new WaitForSeconds(phaseTransitionTime);

            FullHeal();
            m_isTransitioning = false;

            OnPhaseChanged?.Invoke(m_currentPhase);
        }

        /// <summary>
        /// Đánh bại boss hoàn toàn.
        /// Gọi event OnBossDefeated và đánh dấu isDead = true.
        /// </summary>
        private void DefeatBoss()
        {
            m_isDead = true;
            OnBossDefeated?.Invoke();
            Debug.Log("Boss đã bị đánh bại hoàn toàn!");
        }
    }
}
