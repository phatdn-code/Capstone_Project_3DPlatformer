using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Component xử lý các Animation Events cho Dragon Robot.
    /// Dùng để liên kết clip animation với logic (flame thrower, roar, impact...).
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Dragon Robot Animation Events")]
    public class DragonRobotAnimationEvents : MonoBehaviour
    {
        //─────────────────────────────────────────────────────────────
        [Header("References")]
        [Tooltip("Tham chiếu đến DragonRobot (logic gameplay).")]
        [SerializeField] private DragonRobot dragonRobot;

        //─────────────────────────────────────────────────────────────
        // Animation Event Callbacks – gọi từ Animation Clip

        /// <summary>
        /// Được gọi ở frame bắt đầu Flame Thrower (mở miệng, chuẩn bị phun).
        /// </summary>
        public void OnFlameStart()
        {
            dragonRobot?.StartFlameThrowerFromAnimation();
            PlayFlameStartSound();
        }


        /// <summary>
        /// Được gọi khi Dragon nhận damage (hit reaction).
        /// </summary>
        public void OnDamageTaken()
        {
            PlayDamageSound();
        }

        /// <summary>
        /// Được gọi khi chuyển phase (phase change animation).
        /// </summary>
        public void OnPhaseTransition()
        {
            PlayPhaseTransitionEffect();
        }

        /// <summary>
        /// Được gọi ở frame death cuối cùng (cho VFX, disable collider...).
        /// </summary>
        public void OnBossDeath()
        {
            PlayDeathEffect();
        }

        //─────────────────────────────────────────────────────────────
        // Helpers (stub) – bạn tự fill sau

        private void PlayFlameStartSound() { /* TODO: âm thanh bắt đầu phun lửa */ }
        private void PlayFlameEndEffect() { /* TODO: hiệu ứng tắt lửa / khói */ }
        private void PlayRoarSound() { /* TODO: âm thanh gầm */ }
        private void PlayLandingEffect() { /* TODO: dust VFX, camera shake */ }
        private void PlayDamageSound() { /* TODO: âm thanh bị trúng */ }
        private void PlayPhaseTransitionEffect() { /* TODO: hiệu ứng chuyển phase */ }
        private void PlayDeathEffect() { /* TODO: hiệu ứng chết */ }
    }
}
