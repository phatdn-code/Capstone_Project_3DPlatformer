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
        /// Bật / tắt trạng thái Flame Thrower trên Animator.
        /// Dùng bool parameter, ví dụ "IsFlameThrowing".
        /// </summary>
        public void SetFlameThrower(bool isFlameThrowing)
        {
            if (animator == null) return;

            animator.SetBool("IsFlameThrowing", isFlameThrowing);
        }
    }
}
