using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Base class for all boss animation controllers.
    /// Provides safe, optional defaults; each boss overrides only what it needs.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class BossAnimationBase : MonoBehaviour
    {
        [Header("Animator Reference")]
        [SerializeField] protected Animator animator;

        // ─────────────────────────────────────────────────────
        // COMMON / OPTIONAL
        // ─────────────────────────────────────────────────────

        public virtual Animator GetAnimator() => animator;

        /// <summary>Called when the boss starts/stops moving (if that boss can move).</summary>
        public virtual void SetMoving(bool isMoving)
        {
            if (animator != null)
                animator.SetBool("isMoving", isMoving);
        }

        /// <summary>Play phase change animation (if the boss supports it).</summary>
        public virtual void PlayPhaseChange()
        {
            animator?.SetTrigger("PhaseChange");
        }

        /// <summary>Play death animation (if the boss supports it).</summary>
        public virtual void PlayDeath()
        {
            animator?.SetTrigger("Death");
        }

        // ─────────────────────────────────────────────────────
        // OVERRIDABLE HOOKS (chỉ boss có hành vi này mới override)
        // ─────────────────────────────────────────────────────

        /// <summary>For melee-capable bosses.</summary>
        public virtual void PlayMeleeAttack() { /* optional */ }

        /// <summary>For generic ranged-capable bosses.</summary>
        public virtual void PlayShoot() { /* optional */ }

        /// <summary>For bosses with a unique special skill.</summary>
        public virtual void PlaySpecialSkill() { /* optional */ }
    }
}
