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
        // ─────────────────────────────────────────────
        // References
        [Header("Animation Events")]
        [Tooltip("Soldier Robot component")]
        [SerializeField] private SoldierRobot soldierRobot;

        [Tooltip("Animator của boss")]
        private Animator bossAnimator;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        /// <summary>
        /// Khởi tạo SoldierRobot & Animator khi bắt đầu.
        /// </summary>
        private void Start()
        {
            Debug.Log($"🎬 SoldierRobotAnimationEvents Start() | GO: {gameObject.name}, Parent: {(transform.parent ? transform.parent.name : "null")}");

            // Animator
            if (bossAnimator == null)
            {
                bossAnimator = soldierRobot?.SkinAnimator ?? GetComponent<Animator>();
                Debug.Log($"🔍 Animator: {(bossAnimator != null ? "✅ Tìm thấy" : "❌ Null")}");
            }
        }

        // ─────────────────────────────────────────────
        // Animation Event Callbacks
        /// <summary>Bắn bom từ tay phải (gọi từ clip animation).</summary>
        public void OnRightHandShoot()
        {
            Debug.Log("🎯 OnRightHandShoot()");
            soldierRobot?.ShootBombFromAnimation(true);
        }

        /// <summary>Bắn bom từ tay trái (gọi từ clip animation).</summary>
        public void OnLeftHandShoot()
        {
            Debug.Log("🎯 OnLeftHandShoot()");
            soldierRobot?.ShootBombFromAnimation(false);
        }

        /// <summary>Bắt đầu animation bắn (có thể thêm hiệu ứng).</summary>
        public void OnShootStart()
        {
            Debug.Log("Animation Event: Bắt đầu animation bắn");
            PlayShootSound();
        }

        /// <summary>Bắn cầu lửa (gọi từ clip animation).</summary>
        public void OnFireballShoot()
        {
            Debug.Log("🎯 OnFireballShoot()");
            soldierRobot?.CreateFireballFromAnimation();
        }

        /// <summary>
        /// Gọi tại frame ra đòn trong animation cận chiến.
        /// </summary>
        public void OnMeleeHit()
        {
            soldierRobot?.ApplyMeleeDamageToPlayer();
        }

        /// <summary>Boss nhận damage.</summary>
        public void OnDamageTaken()
        {
            Debug.Log("Animation Event: Boss nhận sát thương");
            PlayDamageSound();
        }

        /// <summary>Boss chuyển phase (transition).</summary>
        public void OnPhaseTransition()
        {
            Debug.Log("Animation Event: Boss chuyển giai đoạn");
            PlayPhaseTransitionEffect();
        }

        /// <summary>Boss chết.</summary>
        public void OnBossDeath()
        {
            Debug.Log("Animation Event: Boss chết");
            PlayDeathEffect();
        }

        // ─────────────────────────────────────────────
        // Helpers

        private void PlayShootSound() { /* TODO: Thêm âm thanh bắn bom */ }
        private void PlayDamageSound() { /* TODO: Âm thanh damage */ }
        private void PlayPhaseTransitionEffect() { /* TODO: Hiệu ứng chuyển phase */ }
        private void PlayDeathEffect() { /* TODO: Hiệu ứng chết */ }

        /// <summary>Kích hoạt trigger animation từ code.</summary>
        public void TriggerAnimation(string animationName)
        {
            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger(animationName);
                Debug.Log($"🎬 Trigger animation: {animationName}");
            }

            else Debug.LogError($"❌ Animator null, không trigger được {animationName}");
        }

        /// <summary>In ra thông tin debug về animator.</summary>
        public void DebugAnimationState()
        {
            if (bossAnimator != null)
            {
                var state = bossAnimator.GetCurrentAnimatorStateInfo(0);
                Debug.Log($"🎬 State Hash: {state.shortNameHash}, Speed: {bossAnimator.speed}, Enabled: {bossAnimator.enabled}");
            }

            else Debug.LogError("❌ Animator null - không thể debug");
        }

        /// <summary>Set parameter float cho animator.</summary>
        public void SetAnimationParameter(string parameterName, float value)
        {
            bossAnimator?.SetFloat(parameterName, value);
        }

        /// <summary>Set parameter bool cho animator.</summary>
        public void SetAnimationParameter(string parameterName, bool value)
        {
            bossAnimator?.SetBool(parameterName, value);
        }

        // ─────────────────────────────────────────────
        // Public Setters
        public void SetSoldierRobot(SoldierRobot newSoldierRobot) => soldierRobot = newSoldierRobot;
        public void SetBossAnimator(Animator newAnimator) => bossAnimator = newAnimator;
    }
}
