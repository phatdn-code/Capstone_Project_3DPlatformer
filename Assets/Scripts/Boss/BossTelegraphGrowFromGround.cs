using UnityEngine;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Boss telegraph indicator:
    /// - Snap indicator Y to ground by raycast.
    /// - Scale indicator from 0 -> targetLength using DOTween.
    /// - Optional spell-style FX by pulsing material alpha (URP-friendly).
    /// </summary>
    public class BossTelegraphGrowFromGround : MonoBehaviour
    {
        //─────────────────────────────────────────────────────────────
        #region === Inspector: References ===

        [Header("Indicator Object (enable/disable + scale)")]
        [SerializeField] private Transform indicator;

        [Header("Spell FX (Optional)")]
        [SerializeField] private Renderer indicatorRenderer;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Inspector: Ground Snap (Raycast) ===

        [Header("Ground Snap (Raycast)")]
        [SerializeField] private LayerMask groundMask;

        [SerializeField, Min(0f)] private float raycastHeight = 2f;
        [SerializeField, Min(0f)] private float forwardProbeOffset = 0.5f;
        [SerializeField, Min(0.1f)] private float raycastDistance = 20f;
        [SerializeField, Min(0f)] private float surfaceOffset = 0.02f;

        [SerializeField] private bool followWhileActive = true;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Inspector: Scale ===

        [Header("Scale")]
        [SerializeField] private float targetLength = 40f;
        [SerializeField] private float width = 12f;

        [SerializeField] private Axis lengthAxis = Axis.Z;
        [SerializeField] private Axis widthAxis = Axis.X;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Inspector: Tweens ===

        [Header("Tween (Scale)")]
        [SerializeField] private float growTime = 0.25f;
        [SerializeField] private Ease growEase = Ease.OutCubic;

        [Header("Tween (Spell FX - Alpha Pulse)")]
        [SerializeField] private string colorProperty = "_BaseColor"; // URP Lit/Unlit
        [SerializeField, Range(0f, 1f)] private float pulseMinAlpha = 0.15f;
        [SerializeField, Range(0f, 1f)] private float pulseMaxAlpha = 0.75f;
        [SerializeField, Min(0.01f)] private float pulsePeriod = 0.6f;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Runtime State ===

        private Tween growTween;
        private Tween spellFxTween;

        private bool isActive;

        private Vector3 originalLocalScale;

        private MaterialPropertyBlock mpb;
        private Color cachedBaseColor;
        private bool fxCached;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Unity Lifecycle ===

        private void Start()
        {
            // (VN) Khởi tạo: cache scale gốc và tắt indicator lúc đầu.
            if (indicator == null)
            {
                Debug.LogError($"{nameof(BossTelegraphGrowFromGround)}: indicator is NULL.");
                enabled = false;
                return;
            }

            originalLocalScale = indicator.localScale;
            indicator.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            // (VN) Khi đang active thì bám ground liên tục (nếu bật follow).
            if (!isActive || !followWhileActive || indicator == null) return;
            SnapPositionToGroundOnly();
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Public API ===

        public void PlayTelegraph()
        {
            // (VN) Bật telegraph: hiện indicator, snap ground, scale ra từ từ + bật FX alpha pulse.
            if (indicator == null) return;

            KillGrowTween();

            isActive = true;
            indicator.gameObject.SetActive(true);

            SnapPositionToGroundOnly();

            Vector3 start = BuildStartScale();
            indicator.localScale = start;

            Vector3 end = BuildEndScale(start);

            growTween = indicator
                .DOScale(end, growTime)
                .SetEase(growEase)
                .SetUpdate(UpdateType.Normal, false);

            PlaySpellIndicatorFX();
        }

        public void StopTelegraph()
        {
            // (VN) Tắt telegraph: dừng tween, tắt FX, reset scale, rồi hide indicator.
            KillGrowTween();
            StopSpellIndicatorFX();

            isActive = false;

            if (indicator != null)
            {
                indicator.localScale = originalLocalScale;
                indicator.gameObject.SetActive(false);
            }
        }

        public void PlaySpellIndicatorFX()
        {
            // (VN) FX kiểu spell: nhấp nháy alpha (không đụng tiling/offset).
            if (!TryResolveRenderer()) return;

            CacheFxStateIfNeeded();

            spellFxTween?.Kill();
            spellFxTween = null;

            float half = Mathf.Max(0.01f, pulsePeriod * 0.5f);
            float t = 0f;

            // Dummy tween để có OnUpdate chạy liên tục
            spellFxTween = DOTween
                .To(() => 0f, _ => { }, 1f, 999999f)
                .SetUpdate(UpdateType.Normal, false)
                .OnUpdate(() =>
                {
                    t += Time.deltaTime;

                    float s = 0.5f + 0.5f * Mathf.Sin((t / half) * Mathf.PI); // 0..1
                    float a = Mathf.Lerp(pulseMinAlpha, pulseMaxAlpha, s);

                    ApplyAlphaOnly(a);
                });
        }

        public void StopSpellIndicatorFX()
        {
            // (VN) Dừng FX và trả lại alpha gốc.
            spellFxTween?.Kill();
            spellFxTween = null;

            if (!fxCached || indicatorRenderer == null) return;
            ApplyAlphaOnly(cachedBaseColor.a);
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Ground Snap ===

        private void SnapPositionToGroundOnly()
        {
            // (VN) Raycast xuống ground để lấy Y; giữ nguyên X/Z để không bị trôi.
            Vector3 basePos = indicator.position;

            Vector3 rayOrigin = basePos
                                + indicator.forward * forwardProbeOffset
                                + Vector3.up * raycastHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit,
                                raycastDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                float y = hit.point.y + surfaceOffset;
                indicator.position = new Vector3(basePos.x, y, basePos.z);
            }
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Scale Helpers ===

        private Vector3 BuildStartScale()
        {
            // (VN) Tạo scale bắt đầu: width đúng, length = 0.
            Vector3 start = originalLocalScale;
            SetAxis(ref start, widthAxis, width);
            SetAxis(ref start, lengthAxis, 0f);
            return start;
        }

        private Vector3 BuildEndScale(Vector3 start)
        {
            // (VN) Tạo scale kết thúc: width giữ nguyên, length = targetLength.
            Vector3 end = start;
            SetAxis(ref end, lengthAxis, Mathf.Max(0.001f, targetLength));
            return end;
        }

        private static void SetAxis(ref Vector3 v, Axis axis, float value)
        {
            // (VN) Gán giá trị cho 1 trục của Vector3.
            switch (axis)
            {
                case Axis.X: v.x = value; break;
                case Axis.Y: v.y = value; break;
                case Axis.Z: v.z = value; break;
            }
        }

        private enum Axis { X, Y, Z }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Spell FX Helpers (Alpha Only) ===

        private bool TryResolveRenderer()
        {
            // (VN) Tự tìm Renderer nếu chưa gán trong Inspector.
            if (indicatorRenderer != null) return true;

            if (indicator != null)
                indicatorRenderer = indicator.GetComponentInChildren<Renderer>();

            return indicatorRenderer != null;
        }

        private void CacheFxStateIfNeeded()
        {
            // (VN) Cache màu gốc để StopFX trả lại đúng.
            if (fxCached) return;

            mpb ??= new MaterialPropertyBlock();

            cachedBaseColor = Color.white;

            var mat = indicatorRenderer.sharedMaterial;
            if (mat != null)
            {
                // URP ưu tiên _BaseColor, fallback _Color
                if (mat.HasProperty("_BaseColor")) cachedBaseColor = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color")) cachedBaseColor = mat.GetColor("_Color");
            }

            fxCached = true;
        }

        private void ApplyAlphaOnly(float alpha)
        {
            // (VN) Chỉ chỉnh alpha của màu (URP Lit/Unlit), không đụng UV.
            mpb ??= new MaterialPropertyBlock();
            indicatorRenderer.GetPropertyBlock(mpb);

            Color c = cachedBaseColor;
            c.a = alpha;

            // Set cả 2 để shader nào có thì nhận
            mpb.SetColor("_BaseColor", c);
            mpb.SetColor("_Color", c);

            // Nếu bạn muốn tôn trọng colorProperty, vẫn set thêm 1 lần:
            if (!string.IsNullOrEmpty(colorProperty))
                mpb.SetColor(colorProperty, c);

            indicatorRenderer.SetPropertyBlock(mpb);
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Tween Utilities ===

        private void KillGrowTween()
        {
            // (VN) Dừng tween scale nếu đang chạy.
            growTween?.Kill();
            growTween = null;
        }

        #endregion
        //─────────────────────────────────────────────────────────────
    }
}
