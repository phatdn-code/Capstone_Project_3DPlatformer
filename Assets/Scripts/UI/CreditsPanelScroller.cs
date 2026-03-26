using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class CreditsPanelScroller : MonoBehaviour
    {
        #region Inspector

        [TitleGroup("UI"), Required]
        [SerializeField] private RectTransform viewport;

        [TitleGroup("UI"), Required]
        [SerializeField] private RectTransform creditsContent;

        [TitleGroup("Fast Hint"), Required]
        [SerializeField] private Image fastHintIcon;

        [TitleGroup("Fast Hint"), Required]
        [SerializeField] private CanvasGroup fastHintTextCanvasGroup;

        [TitleGroup("Layout")]
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;

        [TitleGroup("Layout")]
        [SerializeField] private ContentSizeFitter contentSizeFitter;

        [TitleGroup("Scroll Settings"), MinValue(0.1f)]
        [SerializeField] private float scrollDuration = 20f;

        [TitleGroup("Scroll Settings"), MinValue(1f)]
        [SerializeField] private float fastSpeedMultiplier = 4f;

        [TitleGroup("Scroll Settings")]
        [SerializeField] private float startOffset = 50f;

        [TitleGroup("Scroll Settings")]
        [SerializeField] private float endOffset = 50f;

        [TitleGroup("Scroll Settings")]
        [SerializeField] private bool ignoreTimeScale = true;

        [TitleGroup("Fast Hint Visual")]
        [SerializeField] private float hintNormalScale = 1f;

        [TitleGroup("Fast Hint Visual")]
        [SerializeField] private float hintFastScale = 1.08f;

        [TitleGroup("Fast Hint Visual"), MinValue(0.01f)]
        [SerializeField] private float hintTweenDuration = 0.15f;

        [TitleGroup("Fast Hint Visual"), Range(0f, 1f)]
        [SerializeField] private float hintNormalAlpha = 0.82f;

        [TitleGroup("Fast Hint Visual"), Range(0f, 1f)]
        [SerializeField] private float hintFastAlpha = 1f;

        #endregion

        #region Runtime

        private Tween scrollTween;
        private Action onScrollFinished;
        private bool isFastMode;

        public bool IsPlaying { get; private set; }

        #endregion

        #region Public API

        /// <summary>
        /// Bắt đầu chạy credits từ đầu.
        /// </summary>
        public void PlayFromStart(Action onFinished = null)
        {
            KillScrollTween();
            RebuildCreditsLayout();

            onScrollFinished = onFinished;
            IsPlaying = true;
            isFastMode = false;

            ResetFastHintVisual();

            float contentHeight = creditsContent.rect.height;
            float viewportHeight = viewport.rect.height;

            float startY = -viewportHeight - startOffset;
            float endY = contentHeight + endOffset;

            creditsContent.anchoredPosition = new Vector2(0f, startY);

            scrollTween = creditsContent
                .DOAnchorPosY(endY, scrollDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(ignoreTimeScale)
                .OnComplete(OnScrollCompleted);

            ApplyScrollSpeed();
        }

        /// <summary>
        /// Bật hoặc tắt chế độ tua nhanh credits.
        /// </summary>
        public void SetFastMode(bool fastMode)
        {
            if (isFastMode == fastMode)
                return;

            isFastMode = fastMode;

            ApplyScrollSpeed();
            UpdateFastHintVisual(isFastMode);
        }

        /// <summary>
        /// Hiện panel credits.
        /// </summary>
        public void ShowPanel()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Ẩn panel credits và dừng mọi tween liên quan.
        /// </summary>
        public void HidePanel()
        {
            KillScrollTween();

            IsPlaying = false;
            isFastMode = false;
            onScrollFinished = null;

            ResetFastHintVisual();
            gameObject.SetActive(false);
        }

        #endregion

        #region Credits Flow

        /// <summary>
        /// Xử lý khi credits chạy xong.
        /// </summary>
        private void OnScrollCompleted()
        {
            KillScrollTween();

            IsPlaying = false;
            isFastMode = false;

            ResetFastHintVisual();

            onScrollFinished?.Invoke();
            onScrollFinished = null;
        }

        /// <summary>
        /// Áp dụng tốc độ scroll theo trạng thái hiện tại.
        /// </summary>
        private void ApplyScrollSpeed()
        {
            if (scrollTween == null || !scrollTween.IsActive())
                return;

            scrollTween.timeScale = isFastMode ? fastSpeedMultiplier : 1f;
        }

        /// <summary>
        /// Rebuild layout để lấy đúng chiều cao content.
        /// </summary>
        private void RebuildCreditsLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (verticalLayoutGroup != null || contentSizeFitter != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>
        /// Hủy tween scroll hiện tại nếu còn tồn tại.
        /// </summary>
        private void KillScrollTween()
        {
            if (scrollTween != null && scrollTween.IsActive())
                scrollTween.Kill();

            scrollTween = null;
        }

        #endregion

        #region Fast Hint Visual

        /// <summary>
        /// Đưa hint về trạng thái mặc định.
        /// </summary>
        private void ResetFastHintVisual()
        {
            UpdateFastHintVisual(false, true);
        }

        /// <summary>
        /// Cập nhật hiệu ứng hint khi giữ hoặc thả nút tua nhanh.
        /// </summary>
        private void UpdateFastHintVisual(bool fastMode, bool instant = false)
        {
            float duration = instant ? 0f : hintTweenDuration;
            float targetScale = fastMode ? hintFastScale : hintNormalScale;
            float targetAlpha = fastMode ? hintFastAlpha : hintNormalAlpha;

            if (fastHintIcon != null)
            {
                fastHintIcon.rectTransform.DOKill();
                fastHintIcon.rectTransform
                    .DOScale(targetScale, duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(ignoreTimeScale);

                fastHintIcon.DOKill();
                fastHintIcon
                    .DOFade(targetAlpha, duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(ignoreTimeScale);
            }

            if (fastHintTextCanvasGroup != null)
            {
                fastHintTextCanvasGroup.DOKill();
                fastHintTextCanvasGroup
                    .DOFade(targetAlpha, duration)
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(ignoreTimeScale);
            }
        }

        #endregion
    }
}