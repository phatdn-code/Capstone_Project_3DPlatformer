using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Nhận Animation Event từ Animator rồi gọi lên Enemy ở parent.
    /// </summary>
    public class EnemyAnimationEventsBridge : MonoBehaviour
    {
        private Enemy m_enemy;

        /// <summary>Cache Enemy ở parent để gọi event.</summary>
        private void Awake()
        {
            m_enemy = GetComponentInParent<Enemy>();
        }

        // ===== MELEE / EXTRA ATTACK =====

        /// <summary>Frame trúng đòn (melee): trừ máu player.</summary>
        public void AnimEvent_AttackHit()
        {
            if (m_enemy == null) return;
            m_enemy.ExtraAttackHit_AnimationEvent();
        }

        /// <summary>Cuối clip Attack (melee): kết thúc trạng thái đánh.</summary>
        public void AnimEvent_AttackEnd()
        {
            if (m_enemy == null) return;
            m_enemy.ExtraAttackEnd_AnimationEvent();
        }

        // ===== RANGED (PROJECTILE) =====

        /// <summary>Frame bắn: spawn projectile.</summary>
        public void AnimEvent_RangedFire()
        {
            if (m_enemy == null) return;
            m_enemy.RangedFire_AnimationEvent();
        }

        /// <summary>Cuối clip bắn: kết thúc trạng thái bắn.</summary>
        public void AnimEvent_RangedEnd()
        {
            if (m_enemy == null) return;
            m_enemy.RangedAttackEnd_AnimationEvent();
        }
    }
}