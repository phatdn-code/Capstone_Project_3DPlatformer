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
    }
}
