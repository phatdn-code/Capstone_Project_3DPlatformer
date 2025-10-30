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
        #region === BINDING ===

        public void Bind(BossCore newBoss)
        {
            Unbind();

            boss = newBoss;
            if (boss == null) return;

            var linker = boss.GetComponent<BossLinker>();
            if (linker != null && linker.bossHealth != null)
                health = linker.bossHealth;
            else
                health = boss.GetComponent<BossHealth>();

            if (health == null) return;

            boss.OnBossPhaseStartEvent.AddListener(OnBossPhaseStart);
            health.OnHealthChanged += OnHealthChanged;
            health.OnBossDefeated.AddListener(OnBossDefeated);

            Show();
            OnHealthChanged(health.HealthPercentage);
            OnBossPhaseStart(health.currentPhase);
        }

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

        private void OnHealthChanged(float normalized)
        {
            if (bossHealthBar == null) return;

            barTween?.Kill();
            barTween = bossHealthBar.DOValue(normalized, barTweenDuration)
                .SetEase(barEase);
        }

        private void OnBossPhaseStart(int phaseIndex)
        {
            if (phaseNameText == null || boss == null) return;

            string phaseName = (boss.phases != null && phaseIndex < boss.phases.Length)
                ? boss.phases[phaseIndex].phaseName
                : $"Phase {phaseIndex + 1}";

            phaseNameText.text = phaseName;
            phaseNameText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f);

            // Đảm bảo UI vẫn hiển thị khi sang phase mới
            Show();
        }

        private void OnBossDefeated()
        {
            if (boss == null || health == null)
                return;

            bool isLastPhase = health.currentPhase >= boss.phases.Length - 1;

            if (isLastPhase)
                HideCompletely();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === UI CONTROL ===

        private void Show()
        {
            if (panelGroup == null) return;

            panelGroup.DOKill();
            panelGroup.DOFade(1f, fadeDuration);
        }

        /// <summary>Ẩn hoàn toàn khi boss thật sự chết.</summary>
        private void HideCompletely()
        {
            if (panelGroup == null) return;

            panelGroup.DOKill();
            panelGroup.DOFade(0f, fadeDuration)
                .OnComplete(() => gameObject.SetActive(false));
        }

        public void ShowBossIntro(string bossName)
        {
            if (panelGroup != null)
            {
                panelGroup.gameObject.SetActive(true);
                panelGroup.alpha = 0f;
                panelGroup.DOFade(1f, 0.35f);
            }

            if (phaseNameText != null)
            {
                phaseNameText.text = bossName;
                phaseNameText.transform.DOPunchScale(Vector3.one * 0.25f, 0.25f);
            }
        }

        #endregion
    }
}
