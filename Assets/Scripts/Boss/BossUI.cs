using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss UI")]
    public class BossUI : SingletonMonobehaviour<BossUI>
    {
        [Header("UI References")]
        [Tooltip("Thanh máu boss")]
        [SerializeField] private Slider bossHealthBar;
        
        [Tooltip("Text hiển thị tên giai đoạn")]
        [SerializeField] private TextMeshProUGUI phaseNameText;
        
        [Tooltip("Text hiển thị máu boss")]
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Tooltip("Panel chứa UI boss")]
        [SerializeField] private GameObject bossUIPanel;
        
        [Tooltip("Text thông báo chuyển giai đoạn")]
        [SerializeField] private TextMeshProUGUI phaseTransitionText;
        
        [Tooltip("Text thông báo kỹ năng đặc biệt")]
        [SerializeField] private TextMeshProUGUI specialAbilityText;

        [Header("Animation Settings")]
        [Tooltip("Thời gian hiển thị thông báo")]
        [SerializeField] private float notificationDuration = 3f;
        
        [Tooltip("Tốc độ animation thanh máu")]
        [SerializeField] private float healthBarAnimationSpeed = 2f;

        private BaseBoss m_boss;
        private bool m_isVisible = false;

        protected override void Awake()
        {
            base.Awake();
            InitializeBossUI();
        }

        private void InitializeBossUI()
        {
            // Tìm boss trong scene
            m_boss = BossUtils.FindBoss();
            
            if (m_boss == null)
            {
                Debug.LogWarning("Không tìm thấy Boss trong scene!");
                return;
            }

            SetupBossEvents();
            UpdateUI();
        }

        private void Update()
        {
            if (m_boss != null && m_boss.bossHealth != null)
            {
                UpdateHealthBar();
            }
        }

        /// <summary>
        /// Thiết lập các sự kiện từ boss
        /// </summary>
        private void SetupBossEvents()
        {
            BossUtils.SetupBossEvents(m_boss,
                onPhaseStart: OnBossPhaseStart,
                onSpecialAbility: OnSpecialAbilityUsed,
                onPhaseChanged: OnPhaseChanged,
                onBossHealed: OnBossHealed,
                onBossDefeated: OnBossDefeated);
        }

        /// <summary>
        /// Cập nhật UI
        /// </summary>
        private void UpdateUI()
        {
            if (m_boss == null) return;

            UpdateHealthBar();
            UpdatePhaseName();
            UpdateHealthText();
        }

        /// <summary>
        /// Cập nhật thanh máu
        /// </summary>
        private void UpdateHealthBar()
        {
            if (bossHealthBar == null || m_boss.bossHealth == null) return;

            float targetHealth = m_boss.bossHealth.healthPercentage;
            float currentHealth = bossHealthBar.value;
            
            // Smooth animation cho thanh máu
            float newHealth = Mathf.Lerp(currentHealth, targetHealth, 
                healthBarAnimationSpeed * Time.deltaTime);
            
            bossHealthBar.value = newHealth;
        }

        /// <summary>
        /// Cập nhật tên giai đoạn
        /// </summary>
        private void UpdatePhaseName()
        {
            if (phaseNameText == null || m_boss.currentPhase == null) return;

            phaseNameText.text = m_boss.currentPhase.phaseName;
        }

        /// <summary>
        /// Cập nhật text máu
        /// </summary>
        private void UpdateHealthText()
        {
            if (healthText == null || m_boss.bossHealth == null) return;

            healthText.text = $"{m_boss.bossHealth.currentHealth} / {m_boss.bossHealth.initialHealth}";
        }

        /// <summary>
        /// Hiển thị/ẩn UI boss
        /// </summary>
        public void SetBossUIVisible(bool visible)
        {
            m_isVisible = visible;
            
            if (bossUIPanel != null)
            {
                bossUIPanel.SetActive(visible);
            }
        }

        /// <summary>
        /// Hiển thị thông báo chuyển giai đoạn
        /// </summary>
        private void ShowPhaseTransitionNotification(int newPhase)
        {
            if (phaseTransitionText == null) return;

            string message = $"BOSS CHUYỂN SANG GIAI ĐOẠN {newPhase + 1}!";
            ShowNotification(phaseTransitionText, message);
        }

        /// <summary>
        /// Hiển thị thông báo kỹ năng đặc biệt
        /// </summary>
        private void ShowSpecialAbilityNotification(string abilityName)
        {
            if (specialAbilityText == null) return;

            string message = $"BOSS SỬ DỤNG: {abilityName}!";
            ShowNotification(specialAbilityText, message);
        }

        /// <summary>
        /// Hiển thị thông báo chung
        /// </summary>
        private void ShowNotification(TextMeshProUGUI textComponent, string message)
        {
            if (textComponent == null) return;

            textComponent.text = message;
            textComponent.gameObject.SetActive(true);

            // Tự động ẩn sau một khoảng thời gian
            Invoke(nameof(HideNotification), notificationDuration);
        }

        /// <summary>
        /// Ẩn thông báo
        /// </summary>
        private void HideNotification()
        {
            if (phaseTransitionText != null)
                phaseTransitionText.gameObject.SetActive(false);
            
            if (specialAbilityText != null)
                specialAbilityText.gameObject.SetActive(false);
        }

        // Event Handlers
        private void OnBossPhaseStart(int phase)
        {
            Debug.Log($"Boss UI: Bắt đầu giai đoạn {phase + 1}");
            UpdateUI();
        }

        private void OnSpecialAbilityUsed(string abilityName)
        {
            Debug.Log($"Boss UI: Sử dụng kỹ năng {abilityName}");
            ShowSpecialAbilityNotification(abilityName);
        }

        private void OnPhaseChanged(int newPhase)
        {
            Debug.Log($"Boss UI: Chuyển giai đoạn {newPhase + 1}");
            ShowPhaseTransitionNotification(newPhase);
            UpdateUI();
        }

        private void OnBossHealed()
        {
            Debug.Log("Boss UI: Boss đã hồi phục");
            UpdateUI();
        }

        private void OnBossDefeated()
        {
            Debug.Log("Boss UI: Boss đã bị đánh bại");
            SetBossUIVisible(false);
        }

        private void OnDestroy()
        {
            // Cleanup events
            if (m_boss != null)
            {
                m_boss.OnBossPhaseStartEvent.RemoveListener(OnBossPhaseStart);
                m_boss.OnSpecialAbilityUsedEvent.RemoveListener(OnSpecialAbilityUsed);
                m_boss.bossHealth.OnPhaseChanged.RemoveListener(OnPhaseChanged);
                m_boss.bossHealth.OnBossHealed.RemoveListener(OnBossHealed);
                m_boss.bossHealth.OnBossDefeated.RemoveListener(OnBossDefeated);
            }
        }
    }
}
