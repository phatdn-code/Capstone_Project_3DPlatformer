using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Animation controller riêng cho SoldierRobot.
    /// Kế thừa BossAnimationBase để có default logic an toàn.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoldierRobotAnimation : BossAnimationBase
    {
        // Melee
        public override void PlayMeleeAttack()
        {
            animator?.ResetTrigger("TakeDamage");
            animator?.SetTrigger("MeleeAttack");
        }

        // Ranged (generic fallback)
        public override void PlayShoot()
        {
            animator?.ResetTrigger("TakeDamage");
            animator?.SetTrigger("Shoot");
        }

        public override void PlaySpecialSkill()
        {
            animator?.ResetTrigger("TakeDamage");
            animator?.SetTrigger("SmashAttack");
        }

        public override void PlayFinalSkill()
        {
            animator?.SetTrigger("FinalAttack");
        }

        public override void SetHealing(bool isRecharging)
        {
            animator?.SetBool("Recharging", isRecharging);
        }

        // Ranged cụ thể: ném bom từ tay trái/phải
        public void PlayShootBomb(bool useRightHand)
        {
            animator?.SetTrigger(useRightHand ? "RightHandShoot" : "LeftHandShoot");
        }

        // Fireball riêng
        public void PlayFireballShoot()
        {
            animator?.SetTrigger("FireballShoot");
        }
    }
}
