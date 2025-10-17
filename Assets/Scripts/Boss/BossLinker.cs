using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Per-boss linker: centralizes references between BossCore, BossHealth, BossUI, and BossAnimationBase.
    /// Attach this to the same GameObject as the concrete Boss (e.g., SoldierRobot).
    /// </summary>
    [DisallowMultipleComponent]
    public class BossLinker : MonoBehaviour
    {
        [Header("Boss Components")]
        public BossCore bossCore;
        public BossHealth bossHealth;
        public BossUI bossUI;
        public BossAnimationBase bossAnim;

        private void Reset() => AutoLink();
        private void Awake() => AutoLink();

        /// <summary>
        /// Auto-detect related components of THIS boss. Safe for multi-boss in the same scene.
        /// </summary>
        public void AutoLink()
        {
            if (bossCore == null) bossCore = GetComponent<BossCore>();
            if (bossHealth == null) bossHealth = GetComponent<BossHealth>();
            if (bossAnim == null) bossAnim = GetComponent<BossAnimationBase>();
            if (bossUI == null) bossUI = GetComponent<BossUI>();

            if (bossUI != null && bossCore != null)
                bossUI.Bind(bossCore);
        }

        // Helpers
        public bool IsBossDead => bossHealth != null && bossHealth.isDead;
        public Animator Animator => bossAnim != null ? bossAnim.GetAnimator() : null;

        public void DamageBoss(int amount) => bossHealth?.TakeDamage(amount);

        public void PlayDeathAnim() => bossAnim?.PlayDeath();
        public void PlayPhaseChangeAnim() => bossAnim?.PlayPhaseChange();
        public void SetMovingAnim(bool moving) => bossAnim?.SetMoving(moving);
    }
}
