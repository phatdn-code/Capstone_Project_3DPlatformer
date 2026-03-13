using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Quản lý flow cốt truyện:
    /// ảnh -> overlay -> text -> next/skip.
    /// </summary>
    public class StoryManager : SingletonMonobehaviour<StoryManager>
    {
        //────────────────────────────────────────────────────
        #region === STORY DATA ===

        [Header("Story Data")]
        [SerializeField] private List<StoryDataSO> storyList = new();

        #endregion

        //────────────────────────────────────────────────────
        #region === UI REFERENCES ===

        [Header("UI References")]
        [SerializeField] private Transform imageStackParent;
        [SerializeField] private Image storyImagePrefab;
        [SerializeField] private TextMeshProUGUI storyTextUI;

        [Header("Overlay")]
        [SerializeField] private CanvasGroup storyOverlay;

        [Header("Buttons")]
        [SerializeField] private Button nextButton;
        [SerializeField] private Button skipButton;

        #endregion

        //────────────────────────────────────────────────────
        #region === SETTINGS ===

        [Header("Typing")]
        [SerializeField] private float typeSpeed = 0.03f;

        [Header("Page Intro")]
        [SerializeField] private Vector2 pageIntroDelayRange = new Vector2(2f, 3f);

        [Header("Overlay Fade")]
        [SerializeField] private float overlayFadeInDuration = 0.45f;
        [SerializeField] private float overlayFadeOutDuration = 0.55f;
        [SerializeField, Range(0f, 1f)] private float overlayAlphaWhenTextShows = 0.9f;
        [SerializeField] private float skipIntroOverlayFadeDuration = 0.25f;

        [Header("Page Transition")]
        [SerializeField] private float slideDuration = 1.2f;
        [SerializeField] private float slideOutX = 800f;
        [SerializeField] private float nextPageDelay = 0.08f;

        [Header("Ending")]
        [SerializeField] private string nextSceneName;
        [SerializeField] private bool useFadeWhenEnding = true;
        [SerializeField] private float endSceneLoadDelay = 0f;

        [Header("Input")]
        [SerializeField] private float nextInputCooldown = 0.2f;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME ===

        private readonly List<Image> stackedImages = new();

        private int currentIndex;
        private float nextAllowedTime;
        private string currentStoryText = string.Empty;
        private bool isEnding;

        private StoryPageState pageState = StoryPageState.Transitioning;

        private Coroutine introCoroutine;
        private Coroutine typingCoroutine;
        private Coroutine nextPageCoroutine;

        private Tween overlayTween;
        private Sequence transitionSequence;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>
        /// Khởi tạo story khi scene bắt đầu.
        /// </summary>
        private void Start()
        {
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayMusic(1);

            InitButtons();
            BuildStory();
        }

        /// <summary>
        /// Dọn listener và tween khi object bị huỷ.
        /// </summary>
        protected override void OnDestroy()
        {
            RemoveButtonListeners();
            KillRunningFlow();
            DOTween.Kill(this);

            base.OnDestroy();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === SETUP ===

        /// <summary>
        /// Gắn sự kiện cho các nút.
        /// </summary>
        private void InitButtons()
        {
            if (nextButton != null)
                nextButton.onClick.AddListener(OnClickNext);

            if (skipButton != null)
                skipButton.onClick.AddListener(SkipStory);
        }

        /// <summary>
        /// Gỡ sự kiện khỏi các nút.
        /// </summary>
        private void RemoveButtonListeners()
        {
            if (nextButton != null)
                nextButton.onClick.RemoveListener(OnClickNext);

            if (skipButton != null)
                skipButton.onClick.RemoveListener(SkipStory);
        }

        /// <summary>
        /// Dựng lại toàn bộ story từ đầu.
        /// </summary>
        private void BuildStory()
        {
            KillRunningFlow();
            DOTween.Kill(this);

            ClearSpawnedImages();
            SpawnStoryImages();
            ArrangeImagesInStack();
            ResetRuntimeState();
            ResetUIState();

            ShowCurrentStory();
        }

        /// <summary>
        /// Reset các biến runtime.
        /// </summary>
        private void ResetRuntimeState()
        {
            isEnding = false;
            currentIndex = 0;
            nextAllowedTime = 0f;
            currentStoryText = string.Empty;
            pageState = StoryPageState.Transitioning;
        }

        /// <summary>
        /// Reset UI về trạng thái ban đầu.
        /// </summary>
        private void ResetUIState()
        {
            SetButtonsInteractable(true);
            SetOverlayAlphaInstant(0f);
            ResetStoryTextUI();
        }

        /// <summary>
        /// Xoá các ảnh story đã spawn trước đó.
        /// </summary>
        private void ClearSpawnedImages()
        {
            foreach (var image in stackedImages)
            {
                if (image != null)
                    Destroy(image.gameObject);
            }

            stackedImages.Clear();
        }

        /// <summary>
        /// Spawn tất cả ảnh story ngay từ đầu.
        /// </summary>
        private void SpawnStoryImages()
        {
            if (imageStackParent == null || storyImagePrefab == null)
                return;

            foreach (var data in storyList)
            {
                if (data == null) continue;

                Image image = Instantiate(storyImagePrefab, imageStackParent);
                ConfigureStoryImage(image, data);
                stackedImages.Add(image);
            }
        }

        /// <summary>
        /// Cấu hình 1 ảnh story vừa spawn.
        /// </summary>
        private void ConfigureStoryImage(Image image, StoryDataSO data)
        {
            image.sprite = data.illustration;
            image.color = Color.white;
            image.gameObject.SetActive(true);

            RectTransform rectTransform = image.rectTransform;
            rectTransform.anchoredPosition = Vector2.zero;

            CanvasGroup canvasGroup = GetOrAddCanvasGroup(image.gameObject);
            canvasGroup.alpha = 1f;
        }

        /// <summary>
        /// Sắp xếp thứ tự layer của các ảnh story.
        /// </summary>
        private void ArrangeImagesInStack()
        {
            int count = stackedImages.Count;

            for (int i = 0; i < count; i++)
            {
                // Page đầu tiên nằm trên cùng.
                stackedImages[i].transform.SetSiblingIndex(count - 1 - i);
            }
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === STORY FLOW ===

        /// <summary>
        /// Hiển thị page story hiện tại.
        /// </summary>
        private void ShowCurrentStory()
        {
            KillRunningFlow();

            if (!HasValidCurrentStory())
            {
                EndStory();
                return;
            }

            PrepareCurrentPageVisual();

            StoryDataSO currentData = storyList[currentIndex];
            currentStoryText = currentData != null ? currentData.storyText : string.Empty;

            introCoroutine = StartCoroutine(PlayPageIntroThenType(currentStoryText));

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayStoryVoice(currentIndex);
        }

        /// <summary>
        /// Chuẩn bị ảnh, overlay và text cho page hiện tại.
        /// </summary>
        private void PrepareCurrentPageVisual()
        {
            ResetStoryTextUI();
            SetOverlayAlphaInstant(0f);

            if (TryGetCurrentImage(out var image, out var canvasGroup))
            {
                canvasGroup.alpha = 1f;
                image.rectTransform.anchoredPosition = Vector2.zero;
            }

            if (nextButton != null)
                nextButton.interactable = true;
        }

        /// <summary>
        /// Chạy intro page rồi mới bắt đầu gõ chữ.
        /// </summary>
        private IEnumerator PlayPageIntroThenType(string text)
        {
            pageState = StoryPageState.Intro;

            if (!TryGetCurrentImage(out _, out _))
            {
                introCoroutine = null;
                StartTyping(text);
                yield break;
            }

            yield return new WaitForSeconds(GetRandomIntroDelay());
            yield return FadeOverlayTo(overlayAlphaWhenTextShows, overlayFadeInDuration);

            introCoroutine = null;
            StartTyping(text);
        }

        /// <summary>
        /// Bắt đầu coroutine gõ chữ.
        /// </summary>
        private void StartTyping(string text)
        {
            typingCoroutine = StartCoroutine(Typewriter(text));
        }

        /// <summary>
        /// Hiệu ứng typewriter cho text.
        /// </summary>
        private IEnumerator Typewriter(string text)
        {
            pageState = StoryPageState.Typing;

            if (storyTextUI == null)
            {
                typingCoroutine = null;
                pageState = StoryPageState.Ready;
                yield break;
            }

            storyTextUI.text = string.Empty;

            foreach (char character in text)
            {
                storyTextUI.text += character;
                yield return new WaitForSeconds(typeSpeed);
            }

            typingCoroutine = null;
            pageState = StoryPageState.Ready;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === NEXT / SKIP INPUT ===

        /// <summary>
        /// Xử lý khi bấm nút next.
        /// </summary>
        private void OnClickNext()
        {
            if (!CanProcessNextClick())
                return;

            if (!HasAnyStory())
            {
                EndStory();
                return;
            }

            switch (pageState)
            {
                case StoryPageState.Intro:
                    SkipIntroAndStartText();
                    return;

                case StoryPageState.Typing:
                    CompleteCurrentTextImmediately();
                    return;

                case StoryPageState.Ready:
                    GoToNextPageOrEnd();
                    return;

                case StoryPageState.Transitioning:
                    return;
            }
        }

        /// <summary>
        /// Kiểm tra cooldown input của nút next.
        /// </summary>
        private bool CanProcessNextClick()
        {
            if (Time.unscaledTime < nextAllowedTime)
                return false;

            nextAllowedTime = Time.unscaledTime + nextInputCooldown;
            return true;
        }

        /// <summary>
        /// Bỏ qua phần chờ intro, fade overlay mượt lên rồi mới chạy chữ.
        /// </summary>
        private void SkipIntroAndStartText()
        {
            StopCoroutineSafely(ref introCoroutine);
            StopCoroutineSafely(ref typingCoroutine);
            KillTweenSafely(ref overlayTween);

            ResetStoryTextUI();
            StartCoroutine(SkipIntroFadeThenType());
        }

        /// <summary>
        /// Fade overlay mượt lên alpha mục tiêu rồi mới bắt đầu chạy chữ.
        /// </summary>
        private IEnumerator SkipIntroFadeThenType()
        {
            pageState = StoryPageState.Transitioning;

            yield return FadeOverlayTo(overlayAlphaWhenTextShows, skipIntroOverlayFadeDuration);

            StartTyping(currentStoryText);
        }

        /// <summary>
        /// Hiện full toàn bộ text ngay lập tức.
        /// </summary>
        private void CompleteCurrentTextImmediately()
        {
            StopCoroutineSafely(ref typingCoroutine);

            if (storyTextUI != null)
            {
                storyTextUI.text = currentStoryText;
                storyTextUI.ForceMeshUpdate();
            }

            pageState = StoryPageState.Ready;
        }

        /// <summary>
        /// Sang page tiếp theo hoặc kết thúc story.
        /// </summary>
        private void GoToNextPageOrEnd()
        {
            bool isLastPage = currentIndex >= storyList.Count - 1;

            if (isLastPage)
            {
                if (Game.instance != null)
                    Game.instance.MarkIntroStoryAsSeen(true);

                EndStory();
                return;
            }

            SlideOutCurrentPage();
        }

        // Bỏ qua story và đánh dấu đã xem ở lần đầu.
        private void SkipStory()
        {
            if (Game.instance != null)
                Game.instance.MarkIntroStoryAsSeen(true);

            EndStory();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PAGE TRANSITION ===

        /// <summary>
        /// Cho page hiện tại trượt ra ngoài rồi chuyển sang page mới.
        /// </summary>
        private void SlideOutCurrentPage()
        {
            if (!TryGetCurrentImage(out var image, out var imageCanvasGroup))
            {
                EndStory();
                return;
            }

            KillRunningFlow();
            pageState = StoryPageState.Transitioning;

            RectTransform rectTransform = image.rectTransform;

            if (AudioManager.Instance != null)
                AudioManager.Instance.PlaySound(0);

            transitionSequence = CreateSlideOutSequence(rectTransform, imageCanvasGroup);

            transitionSequence.OnComplete(() =>
            {
                transitionSequence = null;
                ResetStoryTextUI();

                currentIndex++;

                if (currentIndex < storyList.Count)
                    nextPageCoroutine = StartCoroutine(ShowNextPageAfterDelay());
                else EndStory();
            });
        }

        /// <summary>
        /// Tạo sequence chuyển page cho ảnh hiện tại.
        /// </summary>
        private Sequence CreateSlideOutSequence(RectTransform rectTransform, CanvasGroup imageCanvasGroup)
        {
            Sequence sequence = DOTween.Sequence().SetTarget(this);

            JoinTextFadeOut(sequence);
            JoinOverlayFadeOut(sequence);
            JoinImageSlideOut(sequence, rectTransform, imageCanvasGroup);

            return sequence;
        }

        /// <summary>
        /// Thêm fade out cho text vào sequence.
        /// </summary>
        private void JoinTextFadeOut(Sequence sequence)
        {
            if (storyTextUI == null) return;

            sequence.Join(
                storyTextUI
                    .DOFade(0f, Mathf.Min(slideDuration, 0.25f))
                    .SetEase(Ease.OutQuad)
            );
        }

        /// <summary>
        /// Thêm fade out cho overlay vào sequence.
        /// </summary>
        private void JoinOverlayFadeOut(Sequence sequence)
        {
            if (storyOverlay == null) return;

            sequence.Join(
                storyOverlay
                    .DOFade(0f, overlayFadeOutDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        /// <summary>
        /// Thêm chuyển động và fade cho ảnh hiện tại.
        /// </summary>
        private void JoinImageSlideOut(Sequence sequence, RectTransform rectTransform, CanvasGroup imageCanvasGroup)
        {
            sequence.Join(
                rectTransform
                    .DOAnchorPosX(slideOutX, slideDuration)
                    .SetEase(Ease.InOutQuad)
            );

            sequence.Join(
                imageCanvasGroup
                    .DOFade(0f, slideDuration)
                    .SetEase(Ease.OutQuad)
            );
        }

        /// <summary>
        /// Đợi một nhịp nhỏ rồi mới show page kế tiếp.
        /// </summary>
        private IEnumerator ShowNextPageAfterDelay()
        {
            if (nextPageDelay > 0f)
                yield return new WaitForSeconds(nextPageDelay);

            nextPageCoroutine = null;
            ShowCurrentStory();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === RESET / CLEANUP ===

        /// <summary>
        /// Reset lại story từ đầu.
        /// </summary>
        public void ResetStory()
        {
            BuildStory();
        }

        /// <summary>
        /// Dừng coroutine và tween đang chạy.
        /// </summary>
        private void KillRunningFlow()
        {
            StopCoroutineSafely(ref introCoroutine);
            StopCoroutineSafely(ref typingCoroutine);
            StopCoroutineSafely(ref nextPageCoroutine);

            KillTweenSafely(ref overlayTween);
            KillSequenceSafely(ref transitionSequence);
        }

        /// <summary>
        /// Dừng coroutine nếu đang tồn tại.
        /// </summary>
        private void StopCoroutineSafely(ref Coroutine coroutine)
        {
            if (coroutine == null) return;

            StopCoroutine(coroutine);
            coroutine = null;
        }

        /// <summary>
        /// Kill tween nếu đang tồn tại.
        /// </summary>
        private void KillTweenSafely(ref Tween tween)
        {
            if (tween == null || !tween.IsActive()) return;

            tween.Kill();
            tween = null;
        }

        /// <summary>
        /// Kill sequence nếu đang tồn tại.
        /// </summary>
        private void KillSequenceSafely(ref Sequence sequence)
        {
            if (sequence == null || !sequence.IsActive()) return;

            sequence.Kill();
            sequence = null;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === UI HELPERS ===

        /// <summary>
        /// Reset text UI về rỗng và alpha đầy đủ.
        /// </summary>
        private void ResetStoryTextUI()
        {
            if (storyTextUI == null) return;

            Color color = storyTextUI.color;
            color.a = 1f;
            storyTextUI.color = color;
            storyTextUI.text = string.Empty;
            storyTextUI.ForceMeshUpdate();
        }

        /// <summary>
        /// Đặt alpha overlay ngay lập tức.
        /// </summary>
        private void SetOverlayAlphaInstant(float alpha)
        {
            if (storyOverlay == null) return;
            storyOverlay.alpha = alpha;
        }

        /// <summary>
        /// Fade overlay tới giá trị mong muốn.
        /// </summary>
        private IEnumerator FadeOverlayTo(float targetAlpha, float duration)
        {
            if (storyOverlay == null)
                yield break;

            KillTweenSafely(ref overlayTween);

            overlayTween = storyOverlay
                .DOFade(targetAlpha, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);

            yield return overlayTween.WaitForCompletion();
            overlayTween = null;
        }

        /// <summary>
        /// Bật/tắt tương tác của các nút.
        /// </summary>
        private void SetButtonsInteractable(bool interactable)
        {
            if (nextButton != null)
                nextButton.interactable = interactable;

            if (skipButton != null)
                skipButton.interactable = interactable;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === DATA HELPERS ===

        /// <summary>
        /// Kiểm tra có data story hay không.
        /// </summary>
        private bool HasAnyStory()
        {
            return storyList != null && storyList.Count > 0;
        }

        /// <summary>
        /// Kiểm tra index story hiện tại có hợp lệ không.
        /// </summary>
        private bool HasValidCurrentStory()
        {
            return HasAnyStory() && currentIndex >= 0 && currentIndex < storyList.Count;
        }

        /// <summary>
        /// Lấy ảnh story hiện tại và CanvasGroup của nó.
        /// </summary>
        private bool TryGetCurrentImage(out Image image, out CanvasGroup canvasGroup)
        {
            image = null;
            canvasGroup = null;

            if (currentIndex < 0 || currentIndex >= stackedImages.Count)
                return false;

            image = stackedImages[currentIndex];
            if (image == null)
                return false;

            canvasGroup = GetOrAddCanvasGroup(image.gameObject);
            return true;
        }

        /// <summary>
        /// Lấy CanvasGroup, nếu chưa có thì tự thêm.
        /// </summary>
        private CanvasGroup GetOrAddCanvasGroup(GameObject target)
        {
            CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = target.AddComponent<CanvasGroup>();

            return canvasGroup;
        }

        /// <summary>
        /// Lấy delay ngẫu nhiên cho intro page.
        /// </summary>
        private float GetRandomIntroDelay()
        {
            float min = Mathf.Min(pageIntroDelayRange.x, pageIntroDelayRange.y);
            float max = Mathf.Max(pageIntroDelayRange.x, pageIntroDelayRange.y);
            return Random.Range(min, max);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === END STORY ===

        /// <summary>
        /// Kết thúc story, fade đen rồi chuyển scene.
        /// </summary>
        private void EndStory()
        {
            if (isEnding)
                return;

            isEnding = true;

            KillRunningFlow();
            DOTween.Kill(this);

            SetButtonsInteractable(false);
            pageState = StoryPageState.Transitioning;

            if (AudioManager.Instance != null)
                AudioManager.Instance.StopStoryVoices();

            if (useFadeWhenEnding && Fader.instance != null)
            {
                Fader.instance.FadeOut(() =>
                {
                    if (endSceneLoadDelay > 0f)
                        DOVirtual.DelayedCall(endSceneLoadDelay, LoadNextScene).SetUpdate(true);

                    else LoadNextScene();
                });

                return;
            }

            LoadNextScene();
        }

        /// <summary>
        /// Load scene kế tiếp sau khi kết thúc story.
        /// </summary>
        private void LoadNextScene()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogWarning("[StoryManager] nextSceneName is empty.");
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }

        #endregion
    }
}