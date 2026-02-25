using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Script gắn trên GameObject có Animator để nhận Animation Event,
    /// sau đó gọi lên Enemy ở parent (GetComponentInParent).
    /// </summary>
    public class EnemyAnimationEventsBridge : MonoBehaviour
    {
        private Enemy m_enemy;

        private void Start()
        {
            m_enemy = GetComponentInParent<Enemy>();
        }

        // Gọi ở frame trúng đòn
        public void AnimEvent_AttackHit()
        {
            if (m_enemy == null) return;
            m_enemy.ExtraAttackHit_AnimationEvent();
        }

        // Gọi ở cuối clip
        public void AnimEvent_AttackEnd()
        {
            if (m_enemy == null) return;
            m_enemy.ExtraAttackEnd_AnimationEvent();
        }
    }
}