using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BossShieldController:
    /// - Bật/tắt shield VFX bằng tween scale
    /// - Hồi shieldValue về đầy bằng DOTween
    /// - Khi recharge: loop scale 0.7 <-> 0.5, đầy thì về scale gốc
    /// </summary>
    public class BossShieldController : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR: REFERENCES ===

        [Header("Shield VFX")]
        [SerializeField] private GameObject shieldObject;

        [Header("Shield UI")]
        [SerializeField] private Slider shieldSlider;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: STATS ===

        [Header("Shield Stats")]
        [SerializeField] private int maxShieldValue = 100;
        [SerializeField] private int shieldValue = 100;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: STARTUP ===

        [Header("Startup")]
        [SerializeField] private bool enableOnStart = true;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: TWEEN ===

        [Header("Tween Settings")]
        [SerializeField] private float scaleTweenDuration = 0.3f;
        [SerializeField] private Ease enableEase = Ease.OutBack;
        [SerializeField] private Ease disableEase = Ease.InBack;

        [Header("Recharge Loop (Scale Factor)")]
        [SerializeField, Range(0f, 2f)] private float loopMaxFactor = 0.9f;   // VN: scale max khi loop
        [SerializeField, Range(0f, 2f)] private float loopMinFactor = 0.7f;   // VN: scale min khi loop
        [SerializeField] private float loopStepDuration = 0.2f;               // VN: tốc độ loop
        [SerializeField] private float settleDuration = 0.2f;                 // VN: về scale gốc

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC STATE ===

        public bool IsActive { get; private set; }
        public bool IsFull => shieldValue >= maxShieldValue;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME CACHE ===

        private Transform _shieldTf;
        private Vector3 _originalScale = Vector3.one;
        private bool _cachedOriginalScale;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME TWEENS ===

        private Tween _scaleTween;     // VN: tween bật/tắt
        private Tween _loopTween;      // VN: tween loop khi recharge
        private Tween _rechargeTween;  // VN: tween hồi shieldValue

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>VN: Init cache, clamp stats, sync UI, bật/tắt theo enableOnStart.</summary>
        private void Start()
        {
            CacheShieldIfNeeded();
            ClampShieldStats();
            SyncShieldUI(true);

            if (enableOnStart) Enable(true);
            else Disable(true);
        }

        /// <summary>VN: Kill tween để tránh kẹt/leak khi object disable.</summary>
        private void OnDisable()
        {
            KillScaleTween();
            KillLoopTween();
            KillRechargeTween();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC API: ENABLE/DISABLE ===

        /// <summary>VN: Bật shield (scale lên).</summary>
        public void Enable(bool instant = false) => SetShield(true, instant);

        /// <summary>VN: Tắt shield (scale về 0 rồi hide).</summary>
        public void Disable(bool instant = false) => SetShield(false, instant);

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC API: VALUE ===

        /// <summary>VN: Set shieldValue theo int và update UI.</summary>
        public void SetShieldValue(int value)
        {
            shieldValue = Mathf.Clamp(value, 0, maxShieldValue);
            SyncShieldUI();
        }

        /// <summary>VN: Trừ shield theo int, về 0 thì tự tắt shield.</summary>
        public void ConsumeShield(int amount)
        {
            if (amount <= 0) return;

            shieldValue = Mathf.Clamp(shieldValue - amount, 0, maxShieldValue);
            SyncShieldUI();

            if (shieldValue <= 0)
                Disable(false);
        }

        /// <summary>VN: Hồi đầy shield và tuỳ chọn bật shield lại.</summary>
        public void RefillShield(bool enableShieldAfterRefill = true, bool instant = false)
        {
            shieldValue = maxShieldValue;
            SyncShieldUI();

            if (enableShieldAfterRefill)
                Enable(instant);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC API: RECHARGE LOOP ===

        /// <summary>VN: Bắt đầu loop scale khi đang recharge.</summary>
        public void StartRechargeLoop(bool instant = false)
        {
            CacheShieldIfNeeded();
            if (_shieldTf == null) return;

            if (shieldObject != null) shieldObject.SetActive(true);

            KillLoopTween();
            KillScaleTween();

            Vector3 maxScale = _originalScale * loopMaxFactor;
            Vector3 minScale = _originalScale * loopMinFactor;

            if (instant || loopStepDuration <= 0f)
            {
                _shieldTf.localScale = maxScale;
                return;
            }

            _shieldTf.localScale = maxScale;
            _loopTween = _shieldTf
                .DOScale(minScale, loopStepDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>VN: Dừng loop và đưa scale về lại scale gốc.</summary>
        public void StopRechargeLoopToOriginal(bool instant = false)
        {
            CacheShieldIfNeeded();
            if (_shieldTf == null) return;

            KillLoopTween();
            KillScaleTween();

            if (instant || settleDuration <= 0f)
            {
                _shieldTf.localScale = _originalScale;
                return;
            }

            _scaleTween = _shieldTf
                .DOScale(_originalScale, settleDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>VN: Hồi shieldValue về max trong duration, xong gọi callback.</summary>
        public void StartRechargeToFull(float duration, System.Action onFullyRefilled = null)
        {
            if (maxShieldValue < 1) maxShieldValue = 1;
            if (duration <= 0f) duration = 0.01f;

            KillRechargeTween();

            if (shieldValue >= maxShieldValue)
            {
                shieldValue = maxShieldValue;
                SyncShieldUI(true);

                if (!IsActive) Enable(false);

                onFullyRefilled?.Invoke();
                return;
            }

            int startValue = Mathf.Clamp(shieldValue, 0, maxShieldValue);

            _rechargeTween = DOTween.To(
                    () => startValue,
                    v =>
                    {
                        startValue = v;
                        shieldValue = v;
                        SyncShieldUI(true);
                    },
                    maxShieldValue,
                    duration
                )
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    shieldValue = maxShieldValue;
                    SyncShieldUI(true);

                    if (!IsActive) Enable(false);

                    onFullyRefilled?.Invoke();
                });
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === CORE ===

        /// <summary>VN: Core bật/tắt shield object + tween scale.</summary>
        private void SetShield(bool active, bool instant)
        {
            CacheShieldIfNeeded();
            if (_shieldTf == null) return;

            // VN: không bật khi shield rỗng
            if (active && shieldValue <= 0) return;

            IsActive = active;

            // VN: tắt/bật thì luôn dừng loop recharge
            KillLoopTween();
            KillScaleTween();

            if (active)
                PlayEnableTween(instant);
            else
                PlayDisableTween(instant);
        }

        /// <summary>VN: Tween bật shield (0 → scale gốc).</summary>
        private void PlayEnableTween(bool instant)
        {
            shieldObject.SetActive(true);

            if (instant || scaleTweenDuration <= 0f)
            {
                _shieldTf.localScale = _originalScale;
                return;
            }

            _shieldTf.localScale = Vector3.zero;
            _scaleTween = _shieldTf
                .DOScale(_originalScale, scaleTweenDuration)
                .SetEase(enableEase);
        }

        /// <summary>VN: Tween tắt shield (scale → 0 rồi hide).</summary>
        private void PlayDisableTween(bool instant)
        {
            if (instant || scaleTweenDuration <= 0f)
            {
                _shieldTf.localScale = Vector3.zero;
                shieldObject.SetActive(false);
                return;
            }

            _scaleTween = _shieldTf
                .DOScale(Vector3.zero, scaleTweenDuration)
                .SetEase(disableEase)
                .OnComplete(() => shieldObject.SetActive(false));
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === UTILS ===

        /// <summary>VN: Cache transform và scale gốc (chỉ 1 lần).</summary>
        private void CacheShieldIfNeeded()
        {
            if (_cachedOriginalScale) return;

            if (shieldObject == null)
            {
                Debug.LogWarning("BossShieldController: ShieldObject chưa được gán.");
                return;
            }

            _shieldTf = shieldObject.transform;
            _originalScale = _shieldTf.localScale;
            _cachedOriginalScale = true;
        }

        /// <summary>VN: Clamp max/value để tránh giá trị sai.</summary>
        private void ClampShieldStats()
        {
            if (maxShieldValue < 1) maxShieldValue = 1;
            shieldValue = Mathf.Clamp(shieldValue, 0, maxShieldValue);
        }

        /// <summary>VN: Update slider theo % shield.</summary>
        private void SyncShieldUI(bool force = false)
        {
            if (shieldSlider == null) return;

            float normalized = (maxShieldValue <= 0) ? 0f : (shieldValue / (float)maxShieldValue);
            shieldSlider.value = normalized;

            if (force)
                shieldSlider.gameObject.SetActive(true);
        }

        /// <summary>VN: Kill tween bật/tắt.</summary>
        private void KillScaleTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
                _scaleTween.Kill();
            _scaleTween = null;
        }

        /// <summary>VN: Kill tween loop recharge.</summary>
        private void KillLoopTween()
        {
            if (_loopTween != null && _loopTween.IsActive())
                _loopTween.Kill();
            _loopTween = null;
        }

        /// <summary>VN: Kill tween hồi shieldValue.</summary>
        private void KillRechargeTween()
        {
            if (_rechargeTween != null && _rechargeTween.IsActive())
                _rechargeTween.Kill();
            _rechargeTween = null;
        }

        #endregion
    }
}
