using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Animation controller riêng cho DragonRobot.
    /// Kế thừa BossAnimationBase để có default logic an toàn.
    /// </summary>
    [DisallowMultipleComponent]
    public class DragonRobotAnimation : BossAnimationBase
    {
        /// <summary>
        /// Bật / tắt trạng thái Flame Thrower trên Animator (bool).
        /// </summary>
        public void SetFlameThrower(bool isFlameThrowing)
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.SetBool("IsFlameThrowing", isFlameThrowing);
        }

        /// <summary>
        /// Play animation Blast Attack (dùng trigger trên Animator).
        /// </summary>
        public void PlayBlastAttack()
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.SetTrigger("BlastAttack");
        }

        /// <summary>
        /// Play animation Meteor Attack (dùng trigger MeteorAttack).
        /// </summary>
        public void PlayMeteorAttack()
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.SetTrigger("MeteorAttack");
        }

        /// <summary>
        /// Bật / tắt trạng thái Meteor Rain (skill mưa meteor, dùng bool).
        /// </summary>
        public void SetMeteorRain(bool isRaining)
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.SetBool("IsMeteorRaining", isRaining);
        }

        /// <summary>Bật / tắt shield của boss (bool trên Animator).</summary>
        public void SetShield(bool isOn)
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.SetBool("IsShieldOn", isOn);
        }

        /// <summary>
        /// Ép thoát toàn bộ trạng thái skill và chuyển ngay về Idle.
        /// </summary>
        public void ForceStopSkillAnimations()
        {
            if (animator == null) return;

            animator.ResetTrigger("TakeDamage");
            animator.ResetTrigger("BlastAttack");
            animator.ResetTrigger("MeteorAttack");

            animator.SetBool("IsFlameThrowing", false);
            animator.SetBool("IsMeteorRaining", false);
            animator.SetBool("IsShieldOn", false);

            animator.CrossFade("Idle", 0.05f, 0);
        }
    }
}
