using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // ✅ Dùng DOTween để quản lý animation

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss UI")]
    public class BossUI : SingletonMonobehaviour<BossUI>
    {
        [Header("UI References")]
        [SerializeField] private Slider bossHealthBar;
        [SerializeField] private TextMeshProUGUI phaseNameText;
        [SerializeField] private TextMeshProUGUI healthText;
        [SerializeField] private GameObject bossUIPanel;
        [SerializeField] private TextMeshProUGUI phaseTransitionText;
        [SerializeField] private TextMeshProUGUI specialAbilityText;

        [Header("Animation Settings")]
        [SerializeField] private float notificationDuration = 3f;   // thời gian giữ thông báo
        [SerializeField] private float healthBarAnimationSpeed = 2f; // tốc độ mượt máu
        [SerializeField] private float fadeDuration = 0.5f;          // thời gian fade in/out

        private BaseBoss m_boss;
        private bool m_isVisible = false;

        // Tween quản lý notification
        private Tween hideTween;

        // ─────────────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────────────
        protected override void Awake()
        {
            base.Awake();
            InitializeBossUI();
        }

        private void Update()
        {
            if (m_boss != null && m_boss.bossHealth != null)
                UpdateHealthBar();
        }

        private void OnDestroy()
        {
            if (m_boss != null)
            {
                m_boss.OnBossPhaseStartEvent.RemoveListener(OnBossPhaseStart);
                m_boss.OnSpecialAbilityUsedEvent.RemoveListener(OnSpecialAbilityUsed);
                m_boss.bossHealth.OnPhaseChanged.RemoveListener(OnPhaseChanged);
                m_boss.bossHealth.OnBossHealed.RemoveListener(OnBossHealed);
                m_boss.bossHealth.OnBossDefeated.RemoveListener(OnBossDefeated);
            }

            hideTween?.Kill();
        }

        // ─────────────────────────────────────────────────────
        // Initialization
        // ─────────────────────────────────────────────────────
        private void InitializeBossUI()
        {
            m_boss = BossUtils.FindBoss();

            if (m_boss == null)
            {
                Debug.LogWarning("Không tìm thấy Boss trong scene!");
                return;
            }

            SetupBossEvents();
            UpdateUI();
        }

        private void SetupBossEvents()
        {
            BossUtils.SetupBossEvents(m_boss,
                onPhaseStart: OnBossPhaseStart,
                onSpecialAbility: OnSpecialAbilityUsed,
                onPhaseChanged: OnPhaseChanged,
                onBossHealed: OnBossHealed,
                onBossDefeated: OnBossDefeated);
        }

        // ─────────────────────────────────────────────────────
        // UI Updates
        // ─────────────────────────────────────────────────────
        private void UpdateUI()
        {
            if (m_boss == null) return;
            UpdateHealthBar();
            UpdatePhaseName();
            UpdateHealthText();
        }

        private void UpdateHealthBar()
        {
            if (bossHealthBar == null || m_boss.bossHealth == null) return;

            float targetHealth = m_boss.bossHealth.healthPercentage;
            float currentHealth = bossHealthBar.value;
            bossHealthBar.value = Mathf.Lerp(currentHealth, targetHealth,
                healthBarAnimationSpeed * Time.deltaTime);
        }

        private void UpdatePhaseName()
        {
            if (phaseNameText == null || m_boss.currentPhase == null) return;
            phaseNameText.text = m_boss.currentPhase.phaseName;
        }

        private void UpdateHealthText()
        {
            if (healthText == null || m_boss.bossHealth == null) return;
            healthText.text = $"{m_boss.bossHealth.currentHealth} / {m_boss.bossHealth.initialHealth}";
        }

        public void SetBossUIVisible(bool visible)
        {
            m_isVisible = visible;
            if (bossUIPanel != null)
                bossUIPanel.SetActive(visible);
        }

        // ─────────────────────────────────────────────────────
        // Notifications (Fade in/out bằng DOTween)
        // ─────────────────────────────────────────────────────
        private void ShowPhaseTransitionNotification(int newPhase)
        {
            if (phaseTransitionText == null) return;
            string message = $"BOSS CHUYỂN SANG GIAI ĐOẠN {newPhase + 1}!";
            ShowNotification(phaseTransitionText, message);
        }

        private void ShowSpecialAbilityNotification(string abilityName)
        {
            if (specialAbilityText == null) return;
            string message = $"BOSS SỬ DỤNG: {abilityName}!";
            ShowNotification(specialAbilityText, message);
        }

        /// <summary>
        /// Hiển thị text notification với fade in/out
        /// </summary>
        private void ShowNotification(TextMeshProUGUI textComponent, string message)
        {
            if (textComponent == null) return;

            textComponent.text = message;

            // Reset alpha = 0 trước khi bật
            var color = textComponent.color;
            color.a = 0;
            textComponent.color = color;
            textComponent.gameObject.SetActive(true);

            // Hủy tween cũ nếu có
            hideTween?.Kill();

            // Fade in
            textComponent.DOFade(1f, fadeDuration);

            // Sau khi giữ notificationDuration, fade out rồi ẩn
            hideTween = DOVirtual.DelayedCall(notificationDuration, () =>
            {
                textComponent.DOFade(0f, fadeDuration)
                    .OnComplete(() => textComponent.gameObject.SetActive(false));
            });
        }

        // ─────────────────────────────────────────────────────
        // Event Handlers
        // ─────────────────────────────────────────────────────
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
    }
}
