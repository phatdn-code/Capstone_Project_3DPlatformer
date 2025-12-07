using UnityEngine;

namespace PixPlays.ElementalVFX
{
    /// <summary>
    /// Khẩu cannon bắn đạn nước:
    /// - Giữ prefab WaterProjectile (bên trong có Rigidbody + ProjectileVfx).
    /// - Bắn tới 1 targetPosition, dùng quỹ đạo bomb (WaterProjectile.Launch).
    /// </summary>
    public class WaterCannon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform muzzle;                // Đầu nòng súng
        [SerializeField] private WaterProjectile projectilePrefab;

        [Header("Fire Settings")]
        [SerializeField] private float fireCooldown = 1.0f;       // Thời gian giữa 2 lần bắn
        [SerializeField] private float flightTime = 1.2f;         // Thời gian bay mong muốn

        private float _lastFireTime;

        /// <summary>
        /// Gọi từ AI / Input: bắn về 1 điểm targetPosition trong world.
        /// </summary>
        public void FireAt(Vector3 targetPosition)
        {
            if (projectilePrefab == null || muzzle == null)
            {
                Debug.LogWarning("WaterCannon: Chưa gán projectilePrefab hoặc muzzle.");
                return;
            }

            if (Time.time < _lastFireTime + fireCooldown)
                return; // chưa tới cooldown

            _lastFireTime = Time.time;

            // Spawn projectile tại vị trí muzzle
            WaterProjectile projectile = Instantiate(
                projectilePrefab,
                muzzle.position,
                Quaternion.identity
            );

            // Cho projectile bay theo quỹ đạo bomb tới target
            projectile.Launch(muzzle.position, targetPosition, flightTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (muzzle == null) return;

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(muzzle.position, 0.1f);
            Gizmos.DrawLine(transform.position, muzzle.position);
        }
    }
}
