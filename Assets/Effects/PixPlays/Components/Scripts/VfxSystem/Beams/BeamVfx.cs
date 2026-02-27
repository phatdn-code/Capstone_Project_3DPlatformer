using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BeamVfx (3-part): LineRenderer + VFX đầu/cuối + hit detection + trừ shield theo thời gian.
    /// </summary>
    [DisallowMultipleComponent]
    public class BeamVfx : MonoBehaviour
    {
        //────────────────────────────────────────────
        #region ===== INSPECTOR: REFERENCES =====

        [Header("Beam Objects (3 parts)")]
        [SerializeField] private GameObject beamLineRenderer;
        [SerializeField] private GameObject beamStart;
        [SerializeField] private GameObject beamEnd;

        #endregion

        //────────────────────────────────────────────
        #region ===== INSPECTOR: BEAM SETTINGS =====

        [Header("Beam Settings")]
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private float beamEndOffset = 0.2f;

        [Header("Hit Detection")]
        [SerializeField] private float hitRadius = 0f;           // 0 = Raycast, >0 = SphereCast
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("Texture")]
        [SerializeField] private float textureScrollSpeed = 8f;
        [SerializeField] private float textureLengthScale = 3f;

        #endregion

        //────────────────────────────────────────────
        #region ===== INSPECTOR: SHIELD DAMAGE =====

        [Header("Shield Damage")]
        [SerializeField] private float shieldDamagePerSecond = 25f;

        #endregion

        //────────────────────────────────────────────
        #region ===== RUNTIME =====

        private const float kDirEpsSqr = 0.000001f;

        private LineRenderer _line;
        private bool _isPlaying;

        private float _shieldDamageAccumulator;

        #endregion

        //────────────────────────────────────────────
        #region ===== UNITY =====

        private void Awake()
        {
            CacheLineRenderer();
            SetActiveAll(false);
        }

        #endregion

        //────────────────────────────────────────────
        #region ===== PUBLIC API =====

        /// <summary>VN: Bắt đầu beam (bật 3 object, reset line + damage).</summary>
        public void StartBeam()
        {
            CacheLineRenderer();

            _isPlaying = true;
            _shieldDamageAccumulator = 0f;

            SetActiveAll(true);

            if (_line != null)
            {
                _line.positionCount = 2;
                _line.SetPosition(0, transform.position);
                _line.SetPosition(1, transform.position);
            }
        }

        /// <summary>VN: Update theo transform hiện tại.</summary>
        public void UpdateBeam()
        {
            if (!_isPlaying) return;
            UpdateBeam(transform.position, transform.forward);
        }

        /// <summary>VN: Update theo nguồn bắn + hướng bắn (dùng cho muzzle).</summary>
        public void UpdateBeam(Vector3 source, Vector3 dir)
        {
            if (!_isPlaying) return;

            dir = NormalizeDir(dir);

            Vector3 end = GetBeamEnd(source, dir, out RaycastHit hit, out bool hasHit);

            ApplyBeamTransforms(source, end, dir);
            UpdateTexture(source, end);

            ApplyShieldDamageIfNeeded(hasHit ? hit.collider : null, Time.deltaTime);
        }

        /// <summary>VN: Tắt beam (ẩn 3 object, reset damage).</summary>
        public void StopAll()
        {
            _isPlaying = false;
            _shieldDamageAccumulator = 0f;
            SetActiveAll(false);
        }

        #endregion

        //────────────────────────────────────────────
        #region ===== BEAM CORE =====

        /// <summary>VN: Cache LineRenderer để dùng nhanh.</summary>
        private void CacheLineRenderer()
        {
            if (_line != null) return;

            if (beamLineRenderer != null)
                _line = beamLineRenderer.GetComponent<LineRenderer>();

            if (_line != null)
                _line.positionCount = 2;
        }

        /// <summary>VN: Tính điểm cuối theo Raycast/SphereCast.</summary>
        private Vector3 GetBeamEnd(Vector3 source, Vector3 dir, out RaycastHit hit, out bool hasHit)
        {
            float radius = Mathf.Max(0f, hitRadius);
            float maxDist = Mathf.Max(0f, maxDistance);

            if (radius <= 0f)
            {
                hasHit = Physics.Raycast(
                    source, dir, out hit, maxDist, hitMask, QueryTriggerInteraction.Collide
                );
            }
            else
            {
                hasHit = Physics.SphereCast(
                    source, radius, dir, out hit, maxDist, hitMask, QueryTriggerInteraction.Collide
                );
            }

            if (hasHit)
            {
                float offset = Mathf.Max(0f, beamEndOffset);
                return hit.point - dir * offset;
            }

            hit = default;
            return source + dir * maxDist;
        }

        /// <summary>VN: Set line + start/end VFX (kèm xoay theo hướng).</summary>
        private void ApplyBeamTransforms(Vector3 start, Vector3 end, Vector3 dir)
        {
            if (_line != null)
            {
                _line.SetPosition(0, start);
                _line.SetPosition(1, end);
            }

            if (beamStart != null)
            {
                beamStart.transform.position = start;
                beamStart.transform.rotation = Quaternion.LookRotation(dir);
            }

            if (beamEnd != null)
            {
                beamEnd.transform.position = end;
                beamEnd.transform.rotation = Quaternion.LookRotation(-dir);
            }
        }

        /// <summary>VN: Scale/scroll UV theo độ dài beam để texture không bị kéo giãn.</summary>
        private void UpdateTexture(Vector3 start, Vector3 end)
        {
            if (_line == null) return;

            float lenScale = Mathf.Max(0.0001f, textureLengthScale);
            float distance = Vector3.Distance(start, end);

            var mat = _line.sharedMaterial;
            if (mat == null) return;

            mat.mainTextureScale = new Vector2(distance / lenScale, 1f);
            mat.mainTextureOffset -= new Vector2(Time.deltaTime * textureScrollSpeed, 0f);
        }

        /// <summary>VN: Chuẩn hoá hướng (tránh dir = 0).</summary>
        private Vector3 NormalizeDir(Vector3 dir)
        {
            if (dir.sqrMagnitude < kDirEpsSqr)
                return transform.forward;

            return dir.normalized;
        }

        /// <summary>VN: Bật/tắt cả 3 object.</summary>
        private void SetActiveAll(bool active)
        {
            if (beamLineRenderer != null) beamLineRenderer.SetActive(active);
            if (beamStart != null) beamStart.SetActive(active);
            if (beamEnd != null) beamEnd.SetActive(active);
        }

        #endregion

        //────────────────────────────────────────────
        #region ===== SHIELD DAMAGE =====

        /// <summary>VN: Trừ shield theo DPS khi collider hit thuộc shield.</summary>
        private void ApplyShieldDamageIfNeeded(Collider hitCollider, float dt)
        {
            if (hitCollider == null)
            {
                _shieldDamageAccumulator = 0f;
                return;
            }

            if (shieldDamagePerSecond <= 0f) return;

            BossShieldController shield = hitCollider.GetComponentInParent<BossShieldController>();
            if (shield == null)
            {
                _shieldDamageAccumulator = 0f;
                return;
            }

            DragonRobot dragon = shield.GetComponentInParent<DragonRobot>();
            if (dragon != null && dragon.IsDamageImmuneThisRound)
            {
                _shieldDamageAccumulator = 0f;
                return;
            }

            if (!shield.IsActive) return;

            _shieldDamageAccumulator += shieldDamagePerSecond * dt;

            int damageInt = Mathf.FloorToInt(_shieldDamageAccumulator);
            if (damageInt <= 0) return;

            _shieldDamageAccumulator -= damageInt;
            shield.ConsumeShield(damageInt);
        }

        #endregion
    }
}