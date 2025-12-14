using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class BeamVfx : MonoBehaviour
    {
        [Header("VFX")]
        [SerializeField] private ParticleSystem beamBodyEffect;
        [SerializeField] private ParticleSystem castEffect;
        [SerializeField] private ParticleSystem hitEffect;
        [SerializeField] private ParticleSystem bodyTip;

        [Header("Beam Settings")]
        [SerializeField] private float extendSpeed = 30f;
        [SerializeField] private float maxDistance = 30f;
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField] private float hitOffset = 0.02f;

        private bool _isPlaying;
        private float _currentLength;
        private Vector3 _baseBodyScale;
        private bool _cachedScale;

        private void Start()
        {
            CacheScale();
            StopAll();
        }

        private void CacheScale()
        {
            if (_cachedScale) return;
            if (beamBodyEffect != null)
                _baseBodyScale = beamBodyEffect.transform.localScale;
            else
                _baseBodyScale = Vector3.one;

            _cachedScale = true;
        }

        /// <summary>Bắt đầu beam.</summary>
        public void StartBeam()
        {
            CacheScale();

            _isPlaying = true;
            _currentLength = 0f;

            // ✅ BẬT TRƯỚC
            SetActive(castEffect, true);
            SetActive(beamBodyEffect, true);
            SetActive(bodyTip, true);
            SetActive(hitEffect, false);

            // ✅ RỒI MỚI PLAY
            Restart(castEffect);
            Restart(beamBodyEffect);
            Restart(bodyTip);

            // đảm bảo scale Z bắt đầu = 0
            if (beamBodyEffect != null)
            {
                var s = _baseBodyScale;
                s.z = 0f;
                beamBodyEffect.transform.localScale = s;
            }
        }

        /// <summary>Cập nhật beam mỗi frame.</summary>
        public void UpdateBeam()
        {
            if (!_isPlaying)
                return;

            Vector3 source = transform.position;
            Vector3 dir = transform.forward;

            bool hasHit = Physics.Raycast(
                source,
                dir,
                out RaycastHit hit,
                maxDistance,
                hitMask,
                QueryTriggerInteraction.Ignore
            );

            float targetLength = hasHit ? hit.distance : maxDistance;
            _currentLength = Mathf.MoveTowards(_currentLength, targetLength, extendSpeed * Time.deltaTime);

            // Body
            if (beamBodyEffect != null)
            {
                beamBodyEffect.transform.position = source;
                beamBodyEffect.transform.rotation = transform.rotation;

                var s = _baseBodyScale;
                s.z = _currentLength;
                beamBodyEffect.transform.localScale = s;
            }

            // Tip
            if (bodyTip != null)
            {
                bodyTip.transform.position = source + dir * _currentLength;
                bodyTip.transform.rotation = transform.rotation;
            }

            // Hit
            if (hasHit && hitEffect != null)
            {
                if (!hitEffect.gameObject.activeSelf)
                {
                    SetActive(hitEffect, true);
                    Restart(hitEffect);
                }

                hitEffect.transform.position = hit.point + hit.normal * hitOffset;
                hitEffect.transform.rotation = Quaternion.LookRotation(-dir);
            }

            else SetActive(hitEffect, false);
        }

        /// <summary>Tắt toàn bộ VFX.</summary>
        public void StopAll()
        {
            _isPlaying = false;
            _currentLength = 0f;

            StopHide(castEffect);
            StopHide(beamBodyEffect);
            StopHide(bodyTip);
            StopHide(hitEffect);
        }

        // helpers
        private static void Restart(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play();
        }

        private static void StopHide(ParticleSystem ps)
        {
            if (ps == null) return;
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.gameObject.SetActive(false);
        }

        private static void SetActive(ParticleSystem ps, bool active)
        {
            if (ps == null) return;
            ps.gameObject.SetActive(active);
        }
    }
}
