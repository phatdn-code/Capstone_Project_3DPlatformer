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

            animator.SetBool("IsFlameThrowing", isFlameThrowing);
        }

        /// <summary>
        /// Play animation Blast Attack (dùng trigger trên Animator).
        /// </summary>
        public void PlayBlastAttack()
        {
            if (animator == null) return;

            // Nếu bạn có trigger "TakeDamage" giống Soldier thì clear trước cho an toàn
            animator.ResetTrigger("TakeDamage");

            // Trigger clip blast attack, nhớ tạo parameter "BlastAttack" trong Animator
            animator.SetTrigger("BlastAttack");
        }
    }
}
