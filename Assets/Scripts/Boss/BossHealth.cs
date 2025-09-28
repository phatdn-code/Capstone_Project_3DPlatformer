using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Health")]
    public class BossHealth : MonoBehaviour
    {
        [Header("Boss Health Settings")]
        [Tooltip("Máu ban đầu của boss")]
        public int initialHealth = 300;
        
        [Tooltip("Thời gian hồi phục giữa các giai đoạn")]
        public float phaseTransitionTime = 3f;
        
        [Tooltip("Hiệu ứng hồi phục")]
        public GameObject healEffect;
        
        [Header("Events")]
        [Tooltip("Được gọi khi boss chuyển giai đoạn")]
        public UnityEvent<int> OnPhaseChanged;
        
        [Tooltip("Được gọi khi boss hồi phục")]
        public UnityEvent OnBossHealed;
        
        [Tooltip("Được gọi khi boss chết hoàn toàn")]
        public UnityEvent OnBossDefeated;

        private int m_currentHealth;
        private int m_currentPhase = 0;
        private bool m_isTransitioning = false;
        private bool m_isDead = false;

        /// <summary>
        /// Máu hiện tại của boss
        /// </summary>
        public int currentHealth
        {
            get { return m_currentHealth; }
            private set
            {
                m_currentHealth = Mathf.Clamp(value, 0, initialHealth);
                
                if (m_currentHealth <= 0 && !m_isDead)
                {
                    HandlePhaseTransition();
                }
            }
        }

        /// <summary>
        /// Giai đoạn hiện tại của boss (0, 1, 2)
        /// </summary>
        public int currentPhase => m_currentPhase;

        /// <summary>
        /// Boss có đang chết không
        /// </summary>
        public bool isDead => m_isDead;

        /// <summary>
        /// Boss có đang chuyển giai đoạn không
        /// </summary>
        public bool isTransitioning => m_isTransitioning;

        /// <summary>
        /// Tỷ lệ máu hiện tại (0-1)
        /// </summary>
        public float healthPercentage => (float)currentHealth / initialHealth;

        private void Start()
        {
            m_currentHealth = initialHealth;
        }

        /// <summary>
        /// Gây sát thương cho boss
        /// </summary>
        /// <param name="damage">Lượng sát thương</param>
        public void TakeDamage(int damage)
        {
            if (m_isDead || m_isTransitioning) return;

            currentHealth -= damage;
            Debug.Log($"Boss nhận {damage} sát thương. Máu còn lại: {currentHealth}");
        }

        /// <summary>
        /// Hồi phục máu cho boss
        /// </summary>
        /// <param name="amount">Lượng máu hồi phục</param>
        public void Heal(int amount)
        {
            if (m_isDead) return;

            currentHealth += amount;
            OnBossHealed?.Invoke();
            
            if (healEffect != null)
            {
                Instantiate(healEffect, transform.position, Quaternion.identity);
            }
        }

        /// <summary>
        /// Hồi phục toàn bộ máu
        /// </summary>
        public void FullHeal()
        {
            currentHealth = initialHealth;
            OnBossHealed?.Invoke();
        }

        /// <summary>
        /// Xử lý chuyển giai đoạn khi boss hết máu
        /// </summary>
        private void HandlePhaseTransition()
        {
            if (m_currentPhase >= 2) // Đã hết 3 giai đoạn
            {
                DefeatBoss();
                return;
            }

            StartCoroutine(TransitionToNextPhase());
        }

        /// <summary>
        /// Chuyển sang giai đoạn tiếp theo
        /// </summary>
        private System.Collections.IEnumerator TransitionToNextPhase()
        {
            m_isTransitioning = true;
            m_currentPhase++;

            Debug.Log($"Boss chuyển sang giai đoạn {m_currentPhase + 1}!");

            // Hiệu ứng chuyển giai đoạn
            yield return new WaitForSeconds(phaseTransitionTime);

            // Hồi phục toàn bộ máu
            FullHeal();
            m_isTransitioning = false;

            OnPhaseChanged?.Invoke(m_currentPhase);
        }

        /// <summary>
        /// Đánh bại boss hoàn toàn
        /// </summary>
        private void DefeatBoss()
        {
            m_isDead = true;
            OnBossDefeated?.Invoke();
            Debug.Log("Boss đã bị đánh bại hoàn toàn!");
        }

        /// <summary>
        /// Reset boss về trạng thái ban đầu
        /// </summary>
        public void ResetBoss()
        {
            m_currentHealth = initialHealth;
            m_currentPhase = 0;
            m_isDead = false;
            m_isTransitioning = false;
        }
    }
}


