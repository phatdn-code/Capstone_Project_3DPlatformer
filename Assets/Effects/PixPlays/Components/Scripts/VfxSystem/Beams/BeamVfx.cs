using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BeamVfx: quản lý VFX beam (cast/body/tip/hit) + detect va chạm + trừ shield theo thời gian.
    /// Fix xuyên: khi hit gần hơn/rút về -> SNAP length + Clear/Play toàn bộ particle trong beamBodyEffect (kể cả con).
    /// </summary>
    public class BeamVfx : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR: VFX ===

        [Header("VFX")]
        [SerializeField] private ParticleSystem beamBodyEffect;
        [SerializeField] private ParticleSystem castEffect;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem bodyTip;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: BEAM SETTINGS ===

        [Header("Beam Settings")]
        [SerializeField] private float maxDistance = 30f;

        [Tooltip("Tốc độ kéo dài beam (đâm ra).")]
        [SerializeField] private float extendSpeed = 30f;

        [Tooltip("Tốc độ rút beam về (nên lớn hơn extendSpeed).")]
        [SerializeField] private float retractSpeed = 120f;

        [Tooltip("Offset hit effect ra khỏi bề mặt (tránh z-fighting).")]
        [SerializeField] private float hitOffset = 0.02f;

        [Tooltip("Trừ bớt chiều dài khi hit để body không chạm xuyên bề mặt.")]
        [SerializeField] private float lengthPadding = 0.03f;

        [Header("Hit Detection")]
        [Tooltip("Độ dày của tia beam (SphereCast).")]
        [SerializeField] private float hitRadius = 0.35f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Anti Penetration (VFX)")]
        [Tooltip("Khi beam bị clamp/rút về, clear & play lại các particle trong BeamBody để xoá phần 'lố'.")]
        [SerializeField] private bool clearParticlesOnClamp = true;

        [Tooltip("Ngưỡng chênh lệch (m) để coi là rút mạnh và clear particle.")]
        [SerializeField] private float clampClearThreshold = 0.05f;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: SHIELD DAMAGE ===

        [Header("Shield Damage")]
        [Tooltip("Sát thương lên shield theo giây (DPS).")]
        [SerializeField] private float shieldDamagePerSecond = 25f;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private bool _isPlaying;
        private float _currentLength;

        private Vector3 _baseBodyScale;
        private bool _cachedScale;

        // Cache tất cả particle nằm trong beam body (kể cả con)
        private ParticleSystem[] _bodyParticles = System.Array.Empty<ParticleSystem>();

        // Tích luỹ damage để chuyển float -> int trừ dần
        private float _shieldDamageAccumulator;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>Khởi tạo cache và tắt beam.</summary>
        private void Start()
        {
            CacheBodyBaseScale();
            CacheBodyParticles();
            StopAll();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC API ===

        /// <summary>Bắt đầu beam (bật VFX + reset trạng thái).</summary>
        public void StartBeam()
        {
            CacheBodyBaseScale();
            CacheBodyParticles();

            _isPlaying = true;
            _currentLength = 0f;
            _shieldDamageAccumulator = 0f;

            SetActive(castEffect, true);
            SetActive(beamBodyEffect, true);
            SetActive(bodyTip, true);
            SetActive(hitEffect, false);

            Restart(castEffect);
            RestartAllBodyParticles(); // ✅ restart cả cụm body
            Restart(bodyTip);

            ResetBodyLengthToZero();
        }

        /// <summary>Cập nhật beam mỗi frame (cast hit + update VFX + damage shield).</summary>
        public void UpdateBeam()
        {
            if (!_isPlaying) return;

            Vector3 source = transform.position;
            Vector3 dir = transform.forward;

            bool hasHit = CastBeam(source, dir, out RaycastHit hit);
            float targetLength = GetTargetLength(hasHit, hit);

            // ✅ FIX XUYÊN: nếu targetLength giảm (hit gần hơn) -> SNAP + clear particles
            ApplyLengthWithClampFix(targetLength);

            UpdateBodyVfx(source);
            UpdateTipVfx(source, dir);
            UpdateHitVfxAndDamage(dir, hasHit, hit);
        }

        /// <summary>Tắt toàn bộ VFX và reset trạng thái.</summary>
        public void StopAll()
        {
            _isPlaying = false;
            _currentLength = 0f;
            _shieldDamageAccumulator = 0f;

            StopHide(castEffect);
            StopHide(beamBodyEffect); // tắt root
            StopHide(bodyTip);
            StopHide(hitEffect);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === CAST / LENGTH ===

        /// <summary>Cast beam (SphereCast nếu hitRadius > 0, không thì Raycast).</summary>
        private bool CastBeam(Vector3 source, Vector3 dir, out RaycastHit hit)
        {
            float radius = Mathf.Max(0f, hitRadius);

            if (radius <= 0f)
            {
                return Physics.Raycast(
                    source,
                    dir,
                    out hit,
                    maxDistance,
                    hitMask,
                    QueryTriggerInteraction.Collide
                );
            }

            return Physics.SphereCast(
                source,
                radius,
                dir,
                out hit,
                maxDistance,
                hitMask,
                QueryTriggerInteraction.Collide
            );
        }

        /// <summary>Tính target length từ kết quả hit (có padding).</summary>
        private float GetTargetLength(bool hasHit, RaycastHit hit)
        {
            if (!hasHit) return maxDistance;

            float pad = Mathf.Max(0f, lengthPadding);
            return Mathf.Max(0f, hit.distance - pad);
        }

        /// <summary>
        /// Áp chiều dài beam:
        /// - Extend: MoveTowards bình thường
        /// - Retract (hit gần hơn): SNAP + clear/play body particles để không còn phần "lố"
        /// </summary>
        private void ApplyLengthWithClampFix(float targetLength)
        {
            float dt = Time.deltaTime;

            // Nếu targetLength nhỏ hơn -> đang retract / hit gần hơn
            bool isRetracting = targetLength < _currentLength;

            if (isRetracting)
            {
                // SNAP để tránh có frame nào dài hơn hit distance
                _currentLength = targetLength;

                // Clear/Play để xoá lịch sử particle/trail đã vẽ vượt quá
                if (clearParticlesOnClamp && (_currentLength - targetLength) > clampClearThreshold)
                {
                    // (Lưu ý: đoạn này về logic luôn ~0 vì đã set _currentLength = targetLength,
                    // nên dùng diff cũ để check)
                }

                if (clearParticlesOnClamp)
                    ClearAndPlayAllBodyParticles();
            }
            else
            {
                // Extend mượt
                float speed = Mathf.Max(0f, extendSpeed);
                _currentLength = Mathf.MoveTowards(_currentLength, targetLength, speed * dt);
            }
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === HIT VFX / SHIELD DAMAGE ===

        /// <summary>Update hit effect + apply damage lên shield nếu collider là shield.</summary>
        private void UpdateHitVfxAndDamage(Vector3 dir, bool hasHit, RaycastHit hit)
        {
            if (!hasHit)
            {
                SetActive(hitEffect, false);
                _shieldDamageAccumulator = 0f;
                return;
            }

            ApplyHitEffect(dir, hit);
            TryDamageShield(hit.collider, Time.deltaTime);
        }

        /// <summary>Đặt hit effect đúng vị trí va chạm.</summary>
        private void ApplyHitEffect(Vector3 dir, RaycastHit hit)
        {
            if (hitEffect == null) return;

            if (!hitEffect.gameObject.activeSelf)
            {
                SetActive(hitEffect, true);
                Restart(hitEffect);
            }

            hitEffect.transform.position = hit.point + hit.normal * hitOffset;
            hitEffect.transform.rotation = Quaternion.LookRotation(-dir);
        }

        /// <summary>Nếu trúng collider shield thì trừ shield theo thời gian (int).</summary>
        private void TryDamageShield(Collider hitCollider, float dt)
        {
            if (hitCollider == null) return;
            if (shieldDamagePerSecond <= 0f) return;

            // 1. Lấy shield từ parent chain
            BossShieldController shield =
                hitCollider.GetComponentInParent<BossShieldController>();

            if (shield == null) return;

            // 2. Lấy boss từ shield (CHA)
            DragonRobot dragon =
                shield.GetComponentInParent<DragonRobot>();

            if (dragon != null && dragon.IsDamageImmuneThisRound)
            {
                _shieldDamageAccumulator = 0f;
                return;
            }

            // 3. Shield phải active
            if (!shield.IsActive) return;

            // 4. DPS tích luỹ
            _shieldDamageAccumulator += shieldDamagePerSecond * dt;

            int damageInt = Mathf.FloorToInt(_shieldDamageAccumulator);
            if (damageInt <= 0) return;

            _shieldDamageAccumulator -= damageInt;
            shield.ConsumeShield(damageInt);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === VFX UPDATE ===

        /// <summary>Cache scale gốc của body để scale theo chiều dài.</summary>
        private void CacheBodyBaseScale()
        {
            if (_cachedScale) return;

            _baseBodyScale = beamBodyEffect != null
                ? beamBodyEffect.transform.localScale
                : Vector3.one;

            _cachedScale = true;
        }

        /// <summary>Cache toàn bộ ParticleSystem nằm trong beamBodyEffect (kể cả con).</summary>
        private void CacheBodyParticles()
        {
            if (beamBodyEffect == null)
            {
                _bodyParticles = System.Array.Empty<ParticleSystem>();
                return;
            }

            // Lấy cả root + children (kể cả inactive)
            _bodyParticles = beamBodyEffect.GetComponentsInChildren<ParticleSystem>(true);
        }

        /// <summary>Reset chiều dài beam body về 0 khi bắt đầu bắn.</summary>
        private void ResetBodyLengthToZero()
        {
            if (beamBodyEffect == null) return;

            var s = _baseBodyScale;
            s.z = 0f;
            beamBodyEffect.transform.localScale = s;
        }

        /// <summary>Update vị trí/rotation/scale của body theo chiều dài hiện tại.</summary>
        private void UpdateBodyVfx(Vector3 source)
        {
            if (beamBodyEffect == null) return;

            beamBodyEffect.transform.position = source;
            beamBodyEffect.transform.rotation = transform.rotation;

            var s = _baseBodyScale;
            s.z = _currentLength;
            beamBodyEffect.transform.localScale = s;
        }

        /// <summary>Update tip ở cuối beam.</summary>
        private void UpdateTipVfx(Vector3 source, Vector3 dir)
        {
            if (bodyTip == null) return;

            bodyTip.transform.position = source + dir * _currentLength;
            bodyTip.transform.rotation = transform.rotation;
        }

        /// <summary>Restart tất cả particle trong cụm beam body.</summary>
        private void RestartAllBodyParticles()
        {
            if (_bodyParticles == null) return;

            for (int i = 0; i < _bodyParticles.Length; i++)
                Restart(_bodyParticles[i]);
        }

        /// <summary>Clear + Play tất cả particle trong cụm beam body (xoá phần lố).</summary>
        private void ClearAndPlayAllBodyParticles()
        {
            if (_bodyParticles == null) return;

            for (int i = 0; i < _bodyParticles.Length; i++)
            {
                var ps = _bodyParticles[i];
                if (ps == null) continue;

                ps.Clear(true);
                ps.Play(true);
            }
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PARTICLE HELPERS ===

        /// <summary>Restart particle system.</summary>
        private static void Restart(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }

        /// <summary>Stop particle system và ẩn GameObject.</summary>
        private static void StopHide(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        /// <summary>Bật/tắt GameObject chứa particle system.</summary>
        private static void SetActive(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            ps.gameObject.SetActive(active);
        }

        #endregion
    }
}
