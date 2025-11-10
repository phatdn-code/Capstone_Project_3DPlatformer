using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// 🔗 Liên kết các component quan trọng của boss:
    /// BossCore, BossHealth, BossUI, BossAnimationBase, BossTransition, BossFinalSequence.
    /// Dùng để quản lý tập trung, mở rộng cho nhiều loại boss khác nhau.
    /// </summary>
    [DisallowMultipleComponent]
    public class BossLinker : MonoBehaviour
    {
        [Header("Boss Components")]
        public BossCore bossCore;
        public BossHealth bossHealth;
        public BossUI bossUI;
        public BossAnimationBase bossAnim;
        public BossPhaseTransitionBase bossTransition;
        public BossFinalSequenceBase finalSequence;

        private bool hasTriggeredFinalSequence;

        //─────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Reset() => AutoLink();
        private void Awake() => AutoLink();

        private void Start()
        {
            if (bossHealth != null)
                bossHealth.OnBossDefeated.AddListener(HandleFinalSequence);

        }

        private void OnDestroy()
        {
            if (bossHealth != null)
                bossHealth.OnBossDefeated.RemoveListener(HandleFinalSequence);

        }

        #endregion
        //─────────────────────────────────────────────────────
        #region === FINAL SEQUENCE HANDLER ===

        private void HandleFinalSequence()
        {
            if (hasTriggeredFinalSequence || finalSequence == null) return;
            hasTriggeredFinalSequence = true;

            finalSequence.RunSequence(this);
        }

        public void ResetFinalSequenceState()
        {
            hasTriggeredFinalSequence = false;
        }

        #endregion
        //─────────────────────────────────────────────────────
        #region === AUTO LINK & HELPERS ===

        public void AutoLink()
        {
            if (bossCore == null) bossCore = GetComponent<BossCore>();
            if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
            if (bossAnim == null) bossAnim = GetComponent<BossAnimationBase>();
            if (bossTransition == null) bossTransition = GetComponent<BossPhaseTransitionBase>();
            if (bossUI == null) bossUI = GetComponent<BossUI>();
            if (finalSequence == null) finalSequence = GetComponent<BossFinalSequenceBase>();

            if (bossUI != null && bossCore != null)
                bossUI.Bind(bossCore);
        }

        public bool IsBossDead => bossHealth != null && bossHealth.isDead;
        public Animator Animator => bossAnim != null ? bossAnim.GetAnimator() : null;

        public void DamageBoss(int amount) => bossHealth?.TakeDamage(amount);
        public void PlayDeathAnim() => bossAnim?.PlayDeath();
        public void PlayPhaseChangeAnim() => bossAnim?.PlayPhaseChange();
        public void SetMovingAnim(bool moving) => bossAnim?.SetMoving(moving);

        #endregion
    }
}
