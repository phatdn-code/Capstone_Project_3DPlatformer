using UnityEngine;

namespace PixPlays.ElementalVFX
{
    /// <summary>
    /// Điều khiển VFX cho đạn nước:
    /// - Cast (bắn ra)
    /// - Projectile (đạn đang bay, bám theo WaterProjectile)
    /// - Hit (trúng mục tiêu)
    /// </summary>
    public class ProjectileVfx : MonoBehaviour
    {
        [Header("Muzzle (tùy chọn)")]
        [SerializeField] private Transform muzzle;              // Miệng súng / điểm xuất phát VFX

        [Header("VFX References")]
        [SerializeField] private ParticleSystem _castEffect;    // Hiệu ứng lúc bắn
        [SerializeField] private ParticleSystem _projectileEffect; // Hiệu ứng đạn đang bay
        [SerializeField] private ParticleSystem _hitEffect;     // Hiệu ứng trúng

        /// <summary>
        /// Cast tại muzzle nếu có, ngược lại dùng transform hiện tại.
        /// </summary>
        public void PlayCastFromMuzzle()
        {
            Vector3 source = muzzle != null ? muzzle.position : transform.position;
            Vector3 dir = muzzle != null ? muzzle.forward : transform.forward;
            PlayCast(source, dir);
        }

        /// <summary>
        /// Gọi khi bắt đầu bắn: spawn cast VFX + projectile VFX tại source.
        /// direction dùng để chỉnh forward cho đẹp.
        /// </summary>
        public void PlayCast(Vector3 source, Vector3 direction)
        {
            // Cast VFX
            if (_castEffect != null)
            {
                _castEffect.gameObject.SetActive(true);
                _castEffect.transform.position = source;

                if (direction.sqrMagnitude > 0.0001f)
                    _castEffect.transform.forward = direction.normalized;

                _castEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _castEffect.Play();
            }

            // Projectile VFX
            if (_projectileEffect != null)
            {
                _projectileEffect.gameObject.SetActive(true);
                _projectileEffect.transform.position = source;

                if (direction.sqrMagnitude > 0.0001f)
                    _projectileEffect.transform.forward = direction.normalized;

                _projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _projectileEffect.Play();
            }
        }

        /// <summary>
        /// Gọi mỗi frame từ WaterProjectile để VFX bám theo vị trí + hướng bay hiện tại.
        /// </summary>
        public void UpdateProjectile(Vector3 position, Vector3 velocity)
        {
            if (_projectileEffect == null)
                return;

            _projectileEffect.transform.position = position;

            if (velocity.sqrMagnitude > 0.0001f)
            {
                _projectileEffect.transform.forward = velocity.normalized;
            }
        }

        /// <summary>
        /// Gọi khi projectile va chạm: tắt VFX bay, bật VFX hit tại điểm va chạm.
        /// Chỉ dùng khi thực sự trúng (OnCollisionEnter).
        /// </summary>
        public void PlayHit(Vector3 hitPoint, Vector3 hitNormal)
        {
            // Tắt VFX đạn bay
            if (_projectileEffect != null)
            {
                _projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                _projectileEffect.gameObject.SetActive(false);
            }

            // Bật VFX trúng
            if (_hitEffect != null)
            {
                _hitEffect.gameObject.SetActive(true);
                _hitEffect.transform.position = hitPoint;

                if (hitNormal.sqrMagnitude > 0.0001f)
                    _hitEffect.transform.forward = hitNormal.normalized;

                _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _hitEffect.Play();
            }
        }

        /// <summary>
        /// Dừng toàn bộ VFX (khi despawn projectile).
        /// </summary>
        public void StopAll()
        {
            if (_castEffect != null)
            {
                _castEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _castEffect.gameObject.SetActive(false);
            }

            if (_projectileEffect != null)
            {
                _projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _projectileEffect.gameObject.SetActive(false);
            }

            if (_hitEffect != null)
            {
                _hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                _hitEffect.gameObject.SetActive(false);
            }
        }
    }
}
