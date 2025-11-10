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
        [HideInInspector] public BossCore bossCore;
        [HideInInspector] public BossHealth bossHealth;
        [HideInInspector] public BossUI bossUI;
        [HideInInspector] public BossAnimationBase bossAnim;
        [HideInInspector] public BossPhaseTransitionBase bossTransition;
        [HideInInspector] public BossFinalSequenceBase finalSequence;

        //─────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Reset() => AutoLink();
        private void Awake() => AutoLink();

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

        public void PlayDeathAnim() => bossAnim?.PlayDeath();

        #endregion
    }
}
