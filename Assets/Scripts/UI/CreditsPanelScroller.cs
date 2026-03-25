using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    public class CreditsPanelScroller : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform creditsContent;

        [Header("Optional Layout")]
        [SerializeField] private VerticalLayoutGroup verticalLayoutGroup;
        [SerializeField] private ContentSizeFitter contentSizeFitter;

        [Header("Scroll Settings")]
        [SerializeField] private float scrollDuration = 20f;
        [SerializeField] private float fastSpeedMultiplier = 4f;
        [SerializeField] private float startOffset = 50f;
        [SerializeField] private float endOffset = 50f;
        [SerializeField] private bool ignoreTimeScale = true;

        private Tween scrollTween;
        private bool isFastMode;
        private Action onScrollFinished;

        public bool IsPlaying { get; private set; }

        /// <summary>
        /// Chạy credit từ dưới lên trên.
        /// Yêu cầu CreditsContent: Anchor Top Center, Pivot (0.5, 1).
        /// </summary>
        public void PlayFromStart(Action onFinished = null)
        {
            KillScrollTween();
            RebuildLayout();

            onScrollFinished = onFinished;
            IsPlaying = true;
            isFastMode = false;

            float contentHeight = creditsContent.rect.height;
            float viewportHeight = viewport.rect.height;

            float startY = -viewportHeight - startOffset;
            float endY = contentHeight + endOffset;

            creditsContent.anchoredPosition = new Vector2(0f, startY);

            scrollTween = creditsContent
                .DOAnchorPosY(endY, scrollDuration)
                .SetEase(Ease.Linear)
                .SetUpdate(ignoreTimeScale)
                .OnComplete(HandleScrollCompleted);

            ApplySpeed();
        }

        /// <summary>
        /// Bật/tắt tăng tốc credit.
        /// </summary>
        public void SetFastMode(bool fastMode)
        {
            isFastMode = fastMode;
            ApplySpeed();
        }

        /// <summary>
        /// Ẩn panel credits.
        /// </summary>
        public void HidePanel()
        {
            KillScrollTween();
            IsPlaying = false;
            isFastMode = false;
            gameObject.SetActive(false);
        }

        /// <summary>
        /// Hiện panel credits.
        /// </summary>
        public void ShowPanel()
        {
            gameObject.SetActive(true);
        }

        private void HandleScrollCompleted()
        {
            KillScrollTween();

            IsPlaying = false;
            isFastMode = false;

            onScrollFinished?.Invoke();
            onScrollFinished = null;
        }

        private void ApplySpeed()
        {
            if (scrollTween == null || !scrollTween.IsActive())
                return;

            scrollTween.timeScale = isFastMode ? fastSpeedMultiplier : 1f;
        }

        private void RebuildLayout()
        {
            Canvas.ForceUpdateCanvases();

            if (verticalLayoutGroup != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

            if (contentSizeFitter != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(creditsContent);

            Canvas.ForceUpdateCanvases();
        }

        private void KillScrollTween()
        {
            if (scrollTween != null && scrollTween.IsActive())
                scrollTween.Kill();

            scrollTween = null;
        }
    }
}