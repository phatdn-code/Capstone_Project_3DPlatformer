using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Hiển thị giao diện Boss (thanh máu, tên phase, hiệu ứng chuyển phase).
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss UI")]
    public class BossUI : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("UI References")]
        [SerializeField] private Slider bossHealthBar;
        [SerializeField] private TextMeshProUGUI phaseNameText;
        [SerializeField] private CanvasGroup panelGroup;

        [Header("Tweens")]
        [SerializeField] private float barTweenDuration = 0.3f;
        [SerializeField] private Ease barEase = Ease.OutCubic;
        [SerializeField] private float fadeDuration = 0.35f;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME REFERENCES ===

        private BossCore boss;
        private BossHealth health;
        private Tween barTween;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void OnDestroy()
        {
            barTween?.Kill();
            Unbind();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === BINDING ===

        /// <summary>
        /// Gán Boss và đăng ký các sự kiện liên quan
        /// </summary>
        public void Bind(BossCore newBoss)
        {
            Unbind();

            boss = newBoss;
            if (boss == null) return;

            health = boss.BossHealth;
            if (health == null) return;

            boss.OnBossPhaseStartEvent.AddListener(OnBossPhaseStart);
            health.OnHealthChanged += OnHealthChanged;
            health.OnBossDefeated.AddListener(OnBossDefeated);

            OnHealthChanged(health.HealthPercentage);
            UpdatePhaseName(health.currentPhase);
        }

        /// <summary>
        /// Huỷ đăng ký sự kiện khi không còn dùng Boss
        /// </summary>
        public void Unbind()
        {
            if (boss != null && health != null)
            {
                boss.OnBossPhaseStartEvent.RemoveListener(OnBossPhaseStart);
                health.OnHealthChanged -= OnHealthChanged;
                health.OnBossDefeated.RemoveListener(OnBossDefeated);
            }

            boss = null;
            health = null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === EVENT HANDLERS ===

        /// <summary>
        /// Cập nhật thanh máu Boss
        /// </summary>
        private void OnHealthChanged(float normalized)
        {
            if (bossHealthBar == null) return;

            barTween?.Kill();
            barTween = bossHealthBar
                .DOValue(normalized, barTweenDuration)
                .SetEase(barEase)
                .SetUpdate(true);
        }

        /// <summary>
        /// Khi Boss chuyển phase
        /// </summary>
        private void OnBossPhaseStart(int phaseIndex)
        {
            UpdatePhaseName(phaseIndex);
        }

        /// <summary>
        /// Khi Boss bị đánh bại hoàn toàn
        /// </summary>
        private void OnBossDefeated()
        {
            if (health == null || !health.isDead)
                return;

            HideCompletely();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === UI CONTROL ===

        /// <summary>
        /// Hiện UI Boss
        /// </summary>
        private void Show()
        {
            if (panelGroup == null) return;

            panelGroup.DOKill();
            panelGroup.DOFade(1f, fadeDuration);
        }

        /// <summary>
        /// Ẩn hoàn toàn UI khi Boss chết
        /// </summary>
        private void HideCompletely()
        {
            if (panelGroup == null) return;

            panelGroup.DOKill();
            panelGroup.DOFade(0f, fadeDuration);
        }

        /// <summary>
        /// Hiển thị intro Boss (khi mới xuất hiện)
        /// </summary>
        public void ShowBossIntro()
        {
            if (panelGroup != null)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.alpha = 0f;
                Show();
            }

            if (health != null)
                UpdatePhaseName(health.currentPhase);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === PHASE NAME HELPERS ===

        /// <summary>
        /// Lấy tên phase theo index (có fallback)
        /// </summary>
        private string GetPhaseName(int phaseIndex)
        {
            if (boss?.Phases != null && phaseIndex < boss.Phases.Length)
                return boss.Phases[phaseIndex].phaseName;

            return $"Phase {phaseIndex + 1}";
        }

        /// <summary>
        /// Cập nhật text tên phase + hiệu ứng
        /// </summary>
        private void UpdatePhaseName(int phaseIndex)
        {
            if (phaseNameText == null || boss == null) return;

            phaseNameText.text = GetPhaseName(phaseIndex);
            phaseNameText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f);
        }

        #endregion
    }
}
