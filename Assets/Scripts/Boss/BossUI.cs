using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Hiển thị giao diện Boss (thanh máu, tên phase, hiệu ứng chuyển phase...).
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
        #region === INITIALIZATION ===

        /// <summary>Gắn UI này với một BossCore cụ thể.</summary>
        public void Bind(BossCore newBoss)
        {
            Unbind();

            boss = newBoss;
            if (boss == null) return;

            // 🔹 Ưu tiên lấy từ BossLinker nếu có
            var linker = boss.GetComponent<BossLinker>();

            if (linker != null && linker.bossHealth != null)
                health = linker.bossHealth;

            else health = boss.GetComponent<BossHealth>();

            if (health == null) return;

            // Đăng ký event
            boss.OnBossPhaseStartEvent.AddListener(OnBossPhaseStart);
            health.OnHealthChanged += OnHealthChanged;
            health.OnBossDefeated.AddListener(Hide);

            // Hiển thị ngay
            Show();
            OnHealthChanged(health.HealthPercentage);
            OnBossPhaseStart(health.currentPhase);
        }


        /// <summary>Gỡ liên kết UI khỏi boss hiện tại (ngắt sự kiện).</summary>
        public void Unbind()
        {
            if (boss != null && health != null)
            {
                boss.OnBossPhaseStartEvent.RemoveListener(OnBossPhaseStart);
                health.OnHealthChanged -= OnHealthChanged;
                health.OnBossDefeated.RemoveListener(Hide);
            }

            boss = null;
            health = null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === EVENT HANDLERS ===

        /// <summary>Cập nhật giá trị thanh máu theo phần trăm hiện tại.</summary>
        private void OnHealthChanged(float normalized)
        {
            if (bossHealthBar == null) return;

            barTween?.Kill();
            barTween = bossHealthBar.DOValue(normalized, barTweenDuration)
                .SetEase(barEase);
        }

        /// <summary>Hiển thị tên phase và hiệu ứng khi chuyển phase.</summary>
        private void OnBossPhaseStart(int phaseIndex)
        {
            if (phaseNameText == null || boss == null) return;

            string phaseName = (boss.phases != null && phaseIndex < boss.phases.Length)
                ? boss.phases[phaseIndex].phaseName
                : $"Phase {phaseIndex + 1}";

            phaseNameText.text = phaseName;
            phaseNameText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === UI VISIBILITY ===

        /// <summary>Hiển thị UI boss bằng hiệu ứng fade.</summary>
        private void Show()
        {
            if (panelGroup == null) return;
            panelGroup.alpha = 0f;
            panelGroup.DOFade(1f, fadeDuration);
        }

        /// <summary>Ẩn UI boss bằng fade-out.</summary>
        private void Hide()
        {
            if (panelGroup == null) return;
            panelGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }

        #endregion
    }
}
