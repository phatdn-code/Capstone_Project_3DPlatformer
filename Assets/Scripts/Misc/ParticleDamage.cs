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

        [Header("Cooldown")]
        [SerializeField] private float hitCooldown = 0.15f;

        [Header("Trigger Bind")]
        [SerializeField] private bool autoBindPlayerOnEnable = true;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private ParticleSystem _ps;
        private ParticleSystem.TriggerModule _trigger;
        private float _nextHitTime;

        // VN: Cache list hạt bị trigger (tránh GC)
        private readonly List<ParticleSystem.Particle> _enterParticles = new();
        private readonly List<ParticleSystem.Particle> _insideParticles = new();

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>VN: Cache ParticleSystem + TriggerModule.</summary>
        private void Awake()
        {
            _ps = GetComponent<ParticleSystem>();
            _trigger = _ps.trigger;
        }

        /// <summary>VN: Khi spawn/pool enable lại thì tự bind collider Player vào Trigger.</summary>
        private void OnEnable()
        {
            if (autoBindPlayerOnEnable)
                BindPlayerColliders();
        }

        /// <summary>VN: Unity gọi khi particle chạm collider đã add trong Trigger module.</summary>
        private void OnParticleTrigger()
        {
            if (!CanHitNow()) return;

            bool hit = mode == ParticleDamageMode.BulletTrigger
                ? CheckBulletHit()
                : CheckSprayHit();

            if (!hit) return;

            ApplyDamageToPlayer();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === TRIGGER BIND ===

        /// <summary>VN: Add toàn bộ collider của Player vào Trigger module (phù hợp game 3D).</summary>
        private void BindPlayerColliders()
        {
            var player = GetPlayer();
            if (player == null) return;

            var colliders = player.GetComponentsInChildren<Collider>(true);
            if (colliders == null || colliders.Length == 0) return;

            ClearTriggerColliders();

            foreach (var col in colliders)
            {
                if (col == null) continue;
                _trigger.AddCollider(col);
            }
        }

        /// <summary>VN: Cho phép spawner gọi thủ công để bind 1 collider cụ thể.</summary>
        public void BindTargetCollider(Collider collider)
        {
            if (collider == null) return;

            ClearTriggerColliders();
            _trigger.AddCollider(collider);
        }

        /// <summary>VN: Xóa toàn bộ collider đang có trong Trigger module (tránh cộng dồn khi pool).</summary>
        private void ClearTriggerColliders()
        {
            for (int i = _trigger.colliderCount - 1; i >= 0; i--)
                _trigger.RemoveCollider(i);
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
        private bool CheckBulletHit()
        {
            _enterParticles.Clear();
            _insideParticles.Clear();

            int enterCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, _enterParticles);
            if (enterCount > 0) return true;

            int insideCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, _insideParticles);
            return insideCount > 0;
        }

        /// <summary>VN: Phun - chỉ cần có hạt đang Inside.</summary>
        private bool CheckSprayHit()
        {
            _insideParticles.Clear();
            int insideCount = _ps.GetTriggerParticles(ParticleSystemTriggerEventType.Inside, _insideParticles);
            return insideCount > 0;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === DAMAGE ===

        /// <summary>VN: Gây damage lên Player và set cooldown.</summary>
        private void ApplyDamageToPlayer()
        {
            _nextHitTime = Time.time + hitCooldown;

            var player = GetPlayer();
            if (player == null) return;

            player.ApplyDamage(damage, transform.position);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === HELPERS ===

        /// <summary>VN: Lấy Player từ PlayerHub (nguồn chuẩn trong project).</summary>
        private Player GetPlayer()
        {
            var hub = PlayerHub.Instance;
            return hub != null ? hub.Player : null;
        }

        #endregion
    }
}