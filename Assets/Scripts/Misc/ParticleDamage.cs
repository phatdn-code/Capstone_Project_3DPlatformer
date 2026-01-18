using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(ParticleSystem))]
    public class ParticleDamage : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Mode")]
        [SerializeField] private ParticleDamageMode mode = ParticleDamageMode.BulletTrigger;

        [Header("Damage")]
        [SerializeField] private int damage = 5;

        [Tooltip("Giới hạn tần suất gây damage (giây).")]
        [SerializeField] private float hitCooldown = 0.15f;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private ParticleSystem _ps;
        private float _nextHitTime;

        // VN: Hạt trigger theo Enter/Inside
        private readonly List<ParticleSystem.Particle> _enterParticles = new();
        private readonly List<ParticleSystem.Particle> _insideParticles = new();

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>VN: Cache ParticleSystem.</summary>
        private void Start()
        {
            _ps = GetComponent<ParticleSystem>();
        }

        /// <summary>VN: Unity gọi khi particle trigger chạm collider đã add trong Trigger module.</summary>
        private void OnParticleTrigger()
        {
            if (!CanHitNow()) return;

            bool hit = mode == ParticleDamageMode.BulletTrigger
                ? IsBulletHit()
                : IsSprayHit();

            if (!hit) return;

            ApplyDamageToPlayer();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === HIT CHECK ===

        /// <summary>VN: Kiểm tra cooldown trước khi gây damage.</summary>
        private bool CanHitNow()
        {
            return Time.time >= _nextHitTime;
        }

        /// <summary>VN: Đạn hạt - ưu tiên Enter, fallback Inside để tránh miss.</summary>
        private bool IsBulletHit()
        {
            _enterParticles.Clear();
            _insideParticles.Clear();

            int enterCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, _enterParticles);
            if (enterCount > 0) return true;

            int insideCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, _insideParticles);
            return insideCount > 0;
        }

        /// <summary>VN: Phun - chỉ cần có hạt đang Inside.</summary>
        private bool IsSprayHit()
        {
            _insideParticles.Clear();
            int insideCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, _insideParticles);
            return insideCount > 0;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === DAMAGE ===

        /// <summary>VN: Gây damage lên Player (lấy Player qua PlayerHub).</summary>
        private void ApplyDamageToPlayer()
        {
            _nextHitTime = Time.time + hitCooldown;

            var hub = PlayerHub.Instance;
            var player = hub != null ? hub.Player : null;
            if (player == null) return;

            player.ApplyDamage(damage, transform.position);
        }

        #endregion
    }
}
