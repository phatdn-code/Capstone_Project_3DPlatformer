using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Component xử lý các Animation Events cho Soldier Robot.
    /// Dùng để liên kết clip animation với logic (bắn bom, cầu lửa, damage, death...).
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Soldier Robot Animation Events")]
    public class SoldierRobotAnimationEvents : MonoBehaviour
    {
        //─────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Tham chiếu đến SoldierRobot (logic gameplay).")]
        [SerializeField] private SoldierRobot soldierRobot;

        [Tooltip("Tham chiếu đến SoldierRobotAnimation (điều khiển Animator).")]
        [SerializeField] private SoldierRobotAnimation soldierAnim;

        private Animator bossAnimator;

        //─────────────────────────────────────────────
        private void Start()
        {
            // Tự tìm component nếu chưa được gán trong Inspector
            if (soldierRobot == null)
                soldierRobot = GetComponentInParent<SoldierRobot>();

            if (soldierAnim == null)
                soldierAnim = GetComponentInParent<SoldierRobotAnimation>();

            // Lấy Animator trực tiếp từ SoldierRobotAnimation
            if (bossAnimator == null && soldierAnim != null)
                bossAnimator = soldierAnim.GetAnimator();

            if (bossAnimator == null)
                bossAnimator = GetComponent<Animator>();

#if UNITY_EDITOR
            Debug.Log($"🎬 SoldierRobotAnimationEvents khởi tạo | Animator: {(bossAnimator ? "✅ Có" : "❌ Null")}");
#endif
        }

        //─────────────────────────────────────────────
        // Animation Event Callbacks

        public void OnRightHandShoot() => soldierRobot?.ShootBombFromAnimation(true);
        public void OnLeftHandShoot() => soldierRobot?.ShootBombFromAnimation(false);
        public void OnFireballShoot() => soldierRobot?.CreateFireballFromAnimation();
        public void OnMeleeHit() => soldierRobot?.ApplyMeleeDamageToPlayer();

        public void OnDamageTaken() => PlayDamageSound();
        public void OnPhaseTransition() => PlayPhaseTransitionEffect();
        public void OnBossDeath() => PlayDeathEffect();

        //─────────────────────────────────────────────
        // Helpers (stub)
        private void PlayShootSound() { /* TODO: âm thanh bắn */ }
        private void PlayDamageSound() { /* TODO: âm thanh trúng */ }
        private void PlayPhaseTransitionEffect() { /* TODO: hiệu ứng chuyển phase */ }
        private void PlayDeathEffect() { /* TODO: hiệu ứng chết */ }

        //─────────────────────────────────────────────
        // Utility Methods
        public void TriggerAnimation(string animationName)
        {
            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger(animationName);
#if UNITY_EDITOR
                Debug.Log($"🎬 Trigger animation: {animationName}");
#endif
            }
            else Debug.LogWarning($"❌ Không tìm thấy Animator khi trigger {animationName}");
        }

        public void DebugAnimationState()
        {
            if (bossAnimator == null)
            {
                Debug.LogWarning("❌ Animator null - không thể debug");
                return;
            }

            var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"🎬 State Hash: {state.shortNameHash}, Speed: {bossAnimator.speed}, Enabled: {bossAnimator.enabled}");
        }

        public void SetAnimationParameter(string name, float value) => bossAnimator?.SetFloat(name, value);
        public void SetAnimationParameter(string name, bool value) => bossAnimator?.SetBool(name, value);

        //─────────────────────────────────────────────
        // Public Setters
        public void SetSoldierRobot(SoldierRobot newRobot) => soldierRobot = newRobot;
        public void SetSoldierAnim(SoldierRobotAnimation newAnim) => soldierAnim = newAnim;
        public void SetBossAnimator(Animator newAnimator) => bossAnimator = newAnimator;
    }
}
