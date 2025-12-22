using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Quản lý Shield cho boss:
    /// - Shield là 1 GameObject VFX
    /// - Mỗi lần bật: scale từ 0 → scale gốc đã setup sẵn
    /// - Có Shield Value (int) + Slider UI hiển thị
    /// </summary>
    public class BossShieldController : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Shield VFX")]
        [SerializeField] private GameObject shieldObject;

        [Header("Shield Stats")]
        [SerializeField] private int maxShieldValue = 100;
        [SerializeField] private int shieldValue = 100;

        [Header("Shield UI")]
        [SerializeField] private Slider shieldSlider; // slider hiển thị shield (0..1)

        [Header("Startup")]
        [SerializeField] private bool enableOnStart = true;

        [Header("Tween Settings")]
        [SerializeField] private float scaleTweenDuration = 0.3f;
        [SerializeField] private Ease enableEase = Ease.OutBack;
        [SerializeField] private Ease disableEase = Ease.InBack;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME ===

        public bool IsActive { get; private set; }
        public bool IsFull => shieldValue >= maxShieldValue;

        private Transform _shieldTf;
        private Vector3 _originalScale = Vector3.one;
        private bool _cachedOriginalScale;

        private Tween _scaleTween;
        private Tween _rechargeTween;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>Init shield scale + bật UI slider + sync giá trị ban đầu.</summary>
        private void Start()
        {
            CacheShieldIfNeeded();
            ClampShieldStats();
            SyncShieldUI(true);

            if (enableOnStart) Enable(true);
            else Disable(true);
        }

        /// <summary>Dọn tween khi bị disable.</summary>
        private void OnDisable()
        {
            KillTween();

            if (_rechargeTween != null && _rechargeTween.IsActive())
                _rechargeTween.Kill();

            _rechargeTween = null;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PUBLIC API ===

        /// <summary>Bật shield (scale 0 → scale gốc).</summary>
        public void Enable(bool instant = false)
        {
            SetShield(true, instant);
        }

        /// <summary>Tắt shield (scale về 0 và ẩn object).</summary>
        public void Disable(bool instant = false)
        {
            SetShield(false, instant);
        }

        /// <summary>Set giá trị shield (int) và cập nhật UI.</summary>
        public void SetShieldValue(int value)
        {
            shieldValue = Mathf.Clamp(value, 0, maxShieldValue);
            SyncShieldUI();
        }

        /// <summary>Trừ shield (ví dụ khi bị đánh) và tự tắt shield nếu về 0.</summary>
        public void ConsumeShield(int amount)
        {
            if (amount <= 0) return;

            shieldValue = Mathf.Clamp(shieldValue - amount, 0, maxShieldValue);
            SyncShieldUI();

            if (shieldValue <= 0)
                Disable(false);
        }

        /// <summary>Hồi đầy shield và (tuỳ bạn) bật lại shield.</summary>
        public void RefillShield(bool enableShieldAfterRefill = true, bool instant = false)
        {
            shieldValue = maxShieldValue;
            SyncShieldUI();

            if (enableShieldAfterRefill)
                Enable(instant);
        }

        /// <summary>Hồi shield về đầy (chạy tween), xong thì gọi callback + bắn event.</summary>
        public void StartRechargeToFull(float duration, System.Action onFullyRefilled = null)
        {
            if (maxShieldValue < 1) maxShieldValue = 1;
            if (duration <= 0f) duration = 0.01f;

            // Kill tween cũ nếu đang hồi
            if (_rechargeTween != null && _rechargeTween.IsActive())
                _rechargeTween.Kill();

            // Nếu đã đầy thì gọi luôn
            if (shieldValue >= maxShieldValue)
            {
                shieldValue = maxShieldValue;
                SyncShieldUI(true);

                if (!IsActive) Enable(false);

                onFullyRefilled?.Invoke();
                return;
            }

            // Tween hồi shieldValue về max
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
        #region === INTERNAL ===

        /// <summary>Cache transform và scale gốc (chỉ làm 1 lần).</summary>
        private void CacheShieldIfNeeded()
        {
            if (_cachedOriginalScale) return;

            if (shieldObject == null)
            {
                Debug.LogWarning("BossShieldController: ShieldObject chưa được gán.");
                return;
            }

            _shieldTf = shieldObject.transform;
            _originalScale = _shieldTf.localScale; // scale gốc bạn set sẵn
            _cachedOriginalScale = true;
        }

        /// <summary>Đảm bảo max/value hợp lệ.</summary>
        private void ClampShieldStats()
        {
            if (maxShieldValue < 1) maxShieldValue = 1;
            shieldValue = Mathf.Clamp(shieldValue, 0, maxShieldValue);
        }

        /// <summary>Core bật/tắt shield object + tween scale.</summary>
        private void SetShield(bool active, bool instant)
        {
            CacheShieldIfNeeded();
            if (_shieldTf == null) return;

            // Nếu shield đã cạn thì không bật (tránh bật “rỗng”)
            if (active && shieldValue <= 0)
                return;

            IsActive = active;
            KillTween();

            if (active)
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
            else
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
        }

        /// <summary>Update slider theo % shield (0..1).</summary>
        private void SyncShieldUI(bool force = false)
        {
            if (shieldSlider == null) return;

            float normalized = (maxShieldValue <= 0) ? 0f : (shieldValue / (float)maxShieldValue);
            shieldSlider.value = normalized;

            // Theo yêu cầu: slider luôn active true khi vào game.
            // Về sau nếu bạn muốn auto hide thì chỉnh ở đây.
            if (force)
                shieldSlider.gameObject.SetActive(true);
        }

        /// <summary>Kill tween để tránh chồng animation.</summary>
        private void KillTween()
        {
            if (_scaleTween != null && _scaleTween.IsActive())
                _scaleTween.Kill();

            _scaleTween = null;
        }

        #endregion
    }
}
