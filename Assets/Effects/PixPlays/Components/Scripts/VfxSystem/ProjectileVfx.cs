using UnityEngine;

namespace PixPlays.ElementalVFX
{
    /// <summary>
    /// VFX cho đạn nước:
    /// - Hiệu ứng đạn đang bay (projectile)
    /// - Hiệu ứng trúng mục tiêu (hit)
    /// Không xử lý muzzle / cast, chỉ bám theo WaterProjectile.
    /// </summary>
    public class ProjectileVfx : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Projectile / Hit VFX")]
        [SerializeField] private ParticleSystem _projectileEffect; // Hiệu ứng đạn đang bay
        [SerializeField] private ParticleSystem _hitEffect;        // Hiệu ứng trúng mục tiêu

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === PUBLIC API ===

        /// <summary>
        /// Bắt đầu VFX đạn bay tại vị trí source, hướng theo direction.
        /// (Gọi khi WaterProjectile vừa được bắn ra.)
        /// </summary>
        public void StartProjectile(Vector3 source, Vector3 direction)
        {
            if (_projectileEffect == null)
                return;

            _projectileEffect.gameObject.SetActive(true);
            _projectileEffect.transform.position = source;

            if (direction.sqrMagnitude > 0.0001f)
                _projectileEffect.transform.forward = direction.normalized;

            _projectileEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            _projectileEffect.Play();
        }

        /// <summary>
        /// Cập nhật vị trí và hướng của VFX đạn bay mỗi frame.
        /// (Gọi từ WaterProjectile.Update.)
        /// </summary>
        public void UpdateProjectile(Vector3 position, Vector3 velocity)
        {
            if (_projectileEffect == null)
                return;

            _projectileEffect.transform.position = position;

            if (velocity.sqrMagnitude > 0.0001f)
                _projectileEffect.transform.forward = velocity.normalized;
        }

        /// <summary>
        /// Chạy VFX trúng mục tiêu tại hitPoint, xoay theo hitNormal.
        /// (Đồng thời tắt VFX đạn đang bay.)
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
        /// Tắt sạch tất cả VFX (dùng trước khi despawn projectile).
        /// </summary>
        public void StopAll()
        {
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

        #endregion
        //────────────────────────────────────────────────────
    }
}
