using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(SphereCollider))]
    public class NPC : MonoBehaviour
    {
        #region Inspector - References

        [TitleGroup("References")]
        [SerializeField]
        private Transform modelRoot;

        [TitleGroup("References"), SerializeField, Required]
        private Transform bubbleTextRoot;

        #endregion

        #region Inspector - Dialogue

        [TitleGroup("Dialogue")]
        [SerializeField, ListDrawerSettings(ShowFoldout = true, DefaultExpandedState = true)]
        private string[] conversations = { };

        [TitleGroup("Dialogue")]
        [SerializeField]
        private bool useTypewriter = true;

        [TitleGroup("Dialogue"), SerializeField, MinValue(0.001f), ShowIf(nameof(useTypewriter))]
        private float characterInterval = 0.03f;

        [TitleGroup("Dialogue"), SerializeField, MinValue(0f)]
        private float lineHoldDuration = 1f;

        [TitleGroup("Dialogue"), SerializeField, MinValue(0.01f)]
        private float textFadeDuration = 0.3f;

        [TitleGroup("Credits"), SerializeField, Required]
        private CreditsPanelScroller creditsPanelScroller;

        [TitleGroup("Credits"), SerializeField]
        private bool freezeCameraDuringCredits = true;

        #endregion

        #region Inspector - Visibility

        [TitleGroup("Visibility"), SerializeField, Range(0f, 180f)]
        private float visibleAngle = 90f;

        [TitleGroup("Visibility"), SerializeField, MinValue(0f)]
        private float hideGraceDuration = 0.12f;

        #endregion

        #region Inspector - Camera Checks

        [TitleGroup("Camera Checks")]
        [SerializeField]
        private bool requireCameraLookAtBubble = true;

        [TitleGroup("Camera Checks")]
        [SerializeField]
        private bool requireCameraInFrontOfNpc = true;

        [TitleGroup("Camera Checks")]
        [SerializeField]
        private bool requireBubbleInsideScreen = true;

        [TitleGroup("Camera Checks"), SerializeField, Range(-1f, 1f), ShowIf(nameof(requireCameraLookAtBubble))]
        private float cameraLookDotThreshold = 0.15f;

        [TitleGroup("Camera Checks"), SerializeField, Range(-1f, 1f), ShowIf(nameof(requireCameraInFrontOfNpc))]
        private float cameraFrontDotThreshold = 0.05f;

        [TitleGroup("Camera Checks"), SerializeField, MinValue(0f), ShowIf(nameof(requireBubbleInsideScreen))]
        private float cameraScreenMargin = 0.08f;

        #endregion

        #region Inspector - Rotation

        [TitleGroup("Rotation")]
        [SerializeField]
        private bool rotateModelToPlayer = true;

        [TitleGroup("Rotation")]
        [SerializeField]
        private bool rotateBubbleToModel = true;

        [TitleGroup("Rotation"), SerializeField, MinValue(0f)]
        private float rotationSpeed = 8f;

        #endregion

        #region Inspector - Canvas

        [TitleGroup("Canvas"), SerializeField, MinValue(0.01f)]
        private float canvasScaleDuration = 0.25f;

        #endregion

        #region Cached Components

        private SphereCollider _triggerCollider;
        private Animator _animator;
        private Camera _mainCamera;

        private Canvas _bubbleCanvas;
        private TMP_Text _bubbleText;

        #endregion

        #region Runtime State

        private Vector3 _bubbleCanvasShownScale;
        private Color _defaultBubbleTextColor;

        private bool _isBubbleVisible;
        private bool _hasPlayedGreetingThisTrigger;
        private bool _isDialogueFinished;

        private int _currentLineIndex;
        private float _lastValidShowTime;

        private Coroutine _canvasScaleCoroutine;
        private Coroutine _dialogueCoroutine;

        private Transform _playerInTrigger;
        private Coroutine _creditsCoroutine;
        private bool _isPlayingCredits;
        private bool _isCreditsFinished;

        private bool _cachedCanPauseBeforeCredits;

        #endregion

        #region Unity Events

        /// <summary>
        /// Cache dữ liệu và chuẩn bị trạng thái ban đầu.
        /// </summary>
        protected virtual void Start()
        {
            CacheReferences();
            InitializeVisualState();
        }

        /// <summary>
        /// Khi đang chạy credits thì giữ Space sẽ tăng tốc.
        /// </summary>
        protected virtual void Update()
        {
            if (!_isPlayingCredits || creditsPanelScroller == null)
                return;

            if (PlayerHub.Instance == null || PlayerHub.Instance.InputManager == null)
                return;

            bool isHoldingJump = PlayerHub.Instance.InputManager.GetJumpHeldRaw();
            creditsPanelScroller.SetFastMode(isHoldingJump);
        }

        /// <summary>
        /// Tự gán nhanh reference khi mới add script.
        /// </summary>
        protected virtual void Reset()
        {
            AutoAssignReferences();
        }

        /// <summary>
        /// Đồng bộ dữ liệu cơ bản khi sửa trong Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            if (modelRoot == null)
                modelRoot = transform;
        }

        /// <summary>
        /// Trong trigger thì kiểm tra để hiện hoặc ẩn bubble.
        /// </summary>
        protected virtual void OnTriggerStay(Collider other)
        {
            if (_isPlayingCredits)
                return;

            if (!other.CompareTag(GameTags.Player))
                return;

            if (CanShowBubble(other.transform))
            {
                _lastValidShowTime = Time.time;
                RotateModelTowardsPlayer(other.transform);
                ShowBubble();
            }
            else if (Time.time - _lastValidShowTime > hideGraceDuration)
            {
                HideBubbleTemporarily();
            }

            _playerInTrigger = other.transform;
            TryStartCredits();
        }

        /// <summary>
        /// Ra khỏi trigger thì ẩn hẳn bubble và reset trạng thái.
        /// </summary>
        protected virtual void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(GameTags.Player))
                return;

            if (_playerInTrigger == other.transform)
                _playerInTrigger = null;

            HideBubbleCompletely();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Hiện bubble từ ngoài nếu cần.
        /// </summary>
        public virtual void Show()
        {
            ShowBubble();
        }

        /// <summary>
        /// Ẩn hẳn bubble từ ngoài nếu cần.
        /// </summary>
        public virtual void Hide()
        {
            HideBubbleCompletely();
        }

        #endregion

        #region Credits

        /// <summary>
        /// Kiểm tra điều kiện và bắt đầu sequence credits.
        /// </summary>
        private void TryStartCredits()
        {
            if (_isPlayingCredits)
                return;

            if (_playerInTrigger == null)
                return;

            if (!_isBubbleVisible)
                return;

            if (creditsPanelScroller == null)
                return;

            if (PlayerHub.Instance == null || PlayerHub.Instance.InputManager == null)
                return;

            if (!PlayerHub.Instance.InputManager.GetStompDown())
                return;

            if (_creditsCoroutine != null)
                StopCoroutine(_creditsCoroutine);

            _creditsCoroutine = StartCoroutine(CreditsSequenceRoutine());
        }

        /// <summary>
        /// Sequence: fade tối màn hình -> chạy credits -> fade sáng lại.
        /// </summary>
        private IEnumerator CreditsSequenceRoutine()
        {
            _isPlayingCredits = true;
            _isCreditsFinished = false;

            HideBubbleForCredits();

            DisablePauseForCredits();

            if (PlayerHub.Instance != null)
                PlayerHub.Instance.LockGameplay(true, freezeCameraDuringCredits);

            yield return FadeOutRoutine();

            creditsPanelScroller.ShowPanel();
            creditsPanelScroller.PlayFromStart(OnCreditsFinished);

            yield return new WaitUntil(() => _isCreditsFinished);

            creditsPanelScroller.SetFastMode(false);
            creditsPanelScroller.HidePanel();

            yield return FadeInRoutine();

            if (PlayerHub.Instance != null)
                PlayerHub.Instance.LockGameplay(false, freezeCameraDuringCredits);

            RestorePauseAfterCredits();

            _isPlayingCredits = false;
            _creditsCoroutine = null;
        }

        /// <summary>
        /// Callback khi credits chạy xong.
        /// </summary>
        private void OnCreditsFinished()
        {
            _isCreditsFinished = true;
        }

        /// <summary>
        /// Chờ fader tối xong.
        /// </summary>
        private IEnumerator FadeOutRoutine()
        {
            if (Fader.instance == null)
                yield break;

            bool finished = false;
            Fader.instance.FadeOut(() => finished = true);

            yield return new WaitUntil(() => finished);
        }

        /// <summary>
        /// Chờ fader sáng lại xong.
        /// </summary>
        private IEnumerator FadeInRoutine()
        {
            if (Fader.instance == null)
                yield break;

            bool finished = false;
            Fader.instance.FadeIn(() => finished = true);

            yield return new WaitUntil(() => finished);
        }

        /// <summary>
        /// Khóa chức năng pause trong lúc chạy credits.
        /// </summary>
        private void DisablePauseForCredits()
        {
            if (LevelPauser.instance == null)
                return;

            _cachedCanPauseBeforeCredits = LevelPauser.instance.canPause;
            LevelPauser.instance.Pause(false);
            LevelPauser.instance.canPause = false;
        }

        /// <summary>
        /// Trả lại trạng thái pause sau khi credits kết thúc.
        /// </summary>
        private void RestorePauseAfterCredits()
        {
            if (LevelPauser.instance == null)
                return;

            LevelPauser.instance.Pause(false);
            LevelPauser.instance.canPause = _cachedCanPauseBeforeCredits;
        }

        #endregion

        #region Setup

        /// <summary>
        /// Tự gán reference theo hierarchy cố định.
        /// </summary>
        private void AutoAssignReferences()
        {
            if (modelRoot == null)
                modelRoot = transform;

            if (bubbleTextRoot == null)
                bubbleTextRoot = FindBubbleTextRoot();

            _bubbleCanvas = FindBubbleCanvas();
            _bubbleText = FindBubbleText();
        }

        /// <summary>
        /// Cache các component cần dùng lúc chạy.
        /// </summary>
        private void CacheReferences()
        {
            AutoAssignReferences();

            _triggerCollider = GetComponent<SphereCollider>();
            _mainCamera = Camera.main;

            _animator = modelRoot != null
                ? modelRoot.GetComponentInChildren<Animator>(true)
                : GetComponentInChildren<Animator>(true);
        }

        /// <summary>
        /// Thiết lập bubble và text về trạng thái ẩn ban đầu.
        /// </summary>
        private void InitializeVisualState()
        {
            if (_bubbleCanvas != null)
            {
                _bubbleCanvasShownScale = _bubbleCanvas.transform.localScale;
                _bubbleCanvas.transform.localScale = Vector3.zero;
                _bubbleCanvas.gameObject.SetActive(true);
            }

            if (_bubbleText != null)
            {
                _defaultBubbleTextColor = _bubbleText.color;
                _bubbleText.text = string.Empty;
                _bubbleText.maxVisibleCharacters = 0;
                SetBubbleTextAlpha(0f);
            }
        }

        /// <summary>
        /// Lấy lại camera chính nếu chưa cache được.
        /// </summary>
        private void RefreshMainCamera()
        {
            if (_mainCamera == null)
                _mainCamera = Camera.main;
        }

        /// <summary>
        /// Tìm bubbleTextRoot là con của object NPC.
        /// </summary>
        private Transform FindBubbleTextRoot()
        {
            Transform namedChild = transform.Find("BubbleTextRoot");
            if (namedChild != null)
                return namedChild;

            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);

                if (child == modelRoot)
                    continue;

                Canvas childCanvas = child.GetComponentInChildren<Canvas>(true);
                if (childCanvas != null)
                    return child;
            }

            return null;
        }

        /// <summary>
        /// Tìm bubbleCanvas là con của bubbleTextRoot.
        /// </summary>
        private Canvas FindBubbleCanvas()
        {
            if (bubbleTextRoot == null)
                return null;

            for (int i = 0; i < bubbleTextRoot.childCount; i++)
            {
                Transform child = bubbleTextRoot.GetChild(i);

                if (child.TryGetComponent(out Canvas canvas))
                    return canvas;
            }

            return bubbleTextRoot.GetComponentInChildren<Canvas>(true);
        }

        /// <summary>
        /// Tìm bubbleText là con của bubbleCanvas.
        /// </summary>
        private TMP_Text FindBubbleText()
        {
            if (_bubbleCanvas == null)
                return null;

            return _bubbleCanvas.GetComponentInChildren<TMP_Text>(true);
        }

        #endregion

        #region Bubble State

        /// <summary>
        /// Hiện bubble và bắt đầu hội thoại.
        /// </summary>
        protected virtual void ShowBubble()
        {
            if (_isBubbleVisible)
                return;

            _isBubbleVisible = true;

            if (!_hasPlayedGreetingThisTrigger)
            {
                TriggerBowAnimation();
                _hasPlayedGreetingThisTrigger = true;
            }

            PlayBubbleCanvasScale(
                _bubbleCanvas != null ? _bubbleCanvas.transform.localScale : Vector3.zero,
                _bubbleCanvasShownScale
            );

            if (!_isDialogueFinished)
                StartDialogue();
        }

        /// <summary>
        /// Ẩn tạm bubble nhưng không reset toàn bộ trạng thái.
        /// </summary>
        protected virtual void HideBubbleTemporarily()
        {
            if (!_isBubbleVisible)
                return;

            _isBubbleVisible = false;

            PlayBubbleCanvasScale(
                _bubbleCanvas != null ? _bubbleCanvas.transform.localScale : Vector3.zero,
                Vector3.zero
            );

            StopDialogue();
        }

        /// <summary>
        /// Ẩn hẳn bubble và reset toàn bộ trạng thái.
        /// </summary>
        protected virtual void HideBubbleCompletely()
        {
            if (_isBubbleVisible)
            {
                _isBubbleVisible = false;

                PlayBubbleCanvasScale(
                    _bubbleCanvas != null ? _bubbleCanvas.transform.localScale : Vector3.zero,
                    Vector3.zero
                );
            }

            StopDialogue();
            ResetDialogue(clearText: true);

            _hasPlayedGreetingThisTrigger = false;
        }

        /// <summary>
        /// Ẩn bubble để chạy credits nhưng không reset tiến trình hội thoại.
        /// </summary>
        protected virtual void HideBubbleForCredits()
        {
            if (_isBubbleVisible)
            {
                _isBubbleVisible = false;

                PlayBubbleCanvasScale(
                    _bubbleCanvas != null ? _bubbleCanvas.transform.localScale : Vector3.zero,
                    Vector3.zero
                );
            }

            StopDialogue();
        }

        #endregion

        #region Dialogue

        /// <summary>
        /// Bắt đầu coroutine hội thoại.
        /// </summary>
        protected virtual void StartDialogue()
        {
            if (_bubbleText == null || conversations == null || conversations.Length == 0)
                return;

            StopDialogue();
            _dialogueCoroutine = StartCoroutine(DialogueSequenceRoutine());
        }

        /// <summary>
        /// Dừng coroutine hội thoại đang chạy.
        /// </summary>
        protected virtual void StopDialogue()
        {
            if (_dialogueCoroutine == null)
                return;

            StopCoroutine(_dialogueCoroutine);
            _dialogueCoroutine = null;
        }

        /// <summary>
        /// Reset tiến trình hội thoại về ban đầu.
        /// </summary>
        protected virtual void ResetDialogue(bool clearText)
        {
            _currentLineIndex = 0;
            _isDialogueFinished = false;

            if (_bubbleText == null)
                return;

            if (clearText)
                _bubbleText.text = string.Empty;

            _bubbleText.maxVisibleCharacters = 0;
            SetBubbleTextAlpha(clearText ? 0f : 1f);
        }

        /// <summary>
        /// Chạy lần lượt từng câu thoại trong mảng.
        /// </summary>
        protected virtual IEnumerator DialogueSequenceRoutine()
        {
            while (_currentLineIndex < conversations.Length)
            {
                string line = conversations[_currentLineIndex];

                if (string.IsNullOrWhiteSpace(line))
                {
                    _currentLineIndex++;
                    continue;
                }

                yield return PlayDialogueLineRoutine(line);

                int nextLineIndex = FindNextValidLineIndex(_currentLineIndex + 1);

                if (nextLineIndex < 0)
                {
                    _isDialogueFinished = true;
                    _dialogueCoroutine = null;
                    yield break;
                }

                yield return new WaitForSeconds(lineHoldDuration);
                yield return FadeBubbleTextAlphaRoutine(GetBubbleTextAlpha(), 0f);

                _currentLineIndex = nextLineIndex;
            }

            _isDialogueFinished = true;
            _dialogueCoroutine = null;
        }

        /// <summary>
        /// Hiện một câu thoại với fade và typewriter.
        /// </summary>
        protected virtual IEnumerator PlayDialogueLineRoutine(string line)
        {
            if (_bubbleText == null)
                yield break;

            _bubbleText.text = line;
            _bubbleText.maxVisibleCharacters = 0;
            _bubbleText.ForceMeshUpdate();

            if (!useTypewriter)
            {
                _bubbleText.maxVisibleCharacters = int.MaxValue;
                SetBubbleTextAlpha(0f);
                yield return FadeBubbleTextAlphaRoutine(0f, 1f);
                yield break;
            }

            int totalCharacters = _bubbleText.textInfo.characterCount;
            float fadeElapsed = 0f;

            SetBubbleTextAlpha(0f);

            for (int i = 0; i <= totalCharacters; i++)
            {
                _bubbleText.maxVisibleCharacters = i;

                if (fadeElapsed < textFadeDuration)
                {
                    fadeElapsed += characterInterval;
                    SetBubbleTextAlpha(Mathf.Clamp01(fadeElapsed / textFadeDuration));
                }

                yield return new WaitForSeconds(characterInterval);
            }

            _bubbleText.maxVisibleCharacters = int.MaxValue;
            SetBubbleTextAlpha(1f);
        }

        /// <summary>
        /// Tìm câu tiếp theo không rỗng trong mảng.
        /// </summary>
        protected virtual int FindNextValidLineIndex(int startIndex)
        {
            if (conversations == null || conversations.Length == 0)
                return -1;

            for (int i = startIndex; i < conversations.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(conversations[i]))
                    return i;
            }

            return -1;
        }

        #endregion

        #region Canvas & Text

        /// <summary>
        /// Bắt đầu scale bubble canvas.
        /// </summary>
        protected virtual void PlayBubbleCanvasScale(Vector3 from, Vector3 to)
        {
            if (_bubbleCanvas == null)
                return;

            if (_canvasScaleCoroutine != null)
                StopCoroutine(_canvasScaleCoroutine);

            _canvasScaleCoroutine = StartCoroutine(BubbleCanvasScaleRoutine(from, to));
        }

        /// <summary>
        /// Scale bubble canvas mượt theo thời gian.
        /// </summary>
        protected virtual IEnumerator BubbleCanvasScaleRoutine(Vector3 from, Vector3 to)
        {
            if (_bubbleCanvas == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < canvasScaleDuration)
            {
                float t = elapsed / canvasScaleDuration;
                _bubbleCanvas.transform.localScale = Vector3.Lerp(from, to, t);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _bubbleCanvas.transform.localScale = to;
            _canvasScaleCoroutine = null;
        }

        /// <summary>
        /// Fade alpha của text bubble.
        /// </summary>
        protected virtual IEnumerator FadeBubbleTextAlphaRoutine(float from, float to)
        {
            if (_bubbleText == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < textFadeDuration)
            {
                float t = elapsed / textFadeDuration;
                SetBubbleTextAlpha(Mathf.Lerp(from, to, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetBubbleTextAlpha(to);
        }

        /// <summary>
        /// Đặt alpha cho text bubble.
        /// </summary>
        protected virtual void SetBubbleTextAlpha(float alpha)
        {
            if (_bubbleText == null)
                return;

            Color color = _defaultBubbleTextColor;
            color.a = alpha;
            _bubbleText.color = color;
        }

        /// <summary>
        /// Lấy alpha hiện tại của text bubble.
        /// </summary>
        protected virtual float GetBubbleTextAlpha()
        {
            if (_bubbleText == null)
                return 1f;

            return _bubbleText.color.a;
        }

        #endregion

        #region Animation

        /// <summary>
        /// Kích hoạt animation cúi chào bằng Trigger Bow.
        /// </summary>
        protected virtual void TriggerBowAnimation()
        {
            if (_animator == null)
                return;

            _animator.ResetTrigger("Bow");
            _animator.SetTrigger("Bow");
        }

        #endregion

        #region Rotation

        /// <summary>
        /// Xoay model NPC về phía player.
        /// </summary>
        protected virtual void RotateModelTowardsPlayer(Transform player)
        {
            if (!rotateModelToPlayer || player == null || modelRoot == null)
                return;

            Vector3 lookDirection = player.position - modelRoot.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude <= 0.001f)
                return;

            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);

            modelRoot.rotation = Quaternion.Slerp(
                modelRoot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );

            RotateBubbleTowardsModel();
        }

        /// <summary>
        /// Xoay bubbleTextRoot theo trục Y của model.
        /// </summary>
        protected virtual void RotateBubbleTowardsModel()
        {
            if (!rotateBubbleToModel || bubbleTextRoot == null || modelRoot == null)
                return;

            Vector3 currentEuler = bubbleTextRoot.rotation.eulerAngles;
            float targetY = modelRoot.rotation.eulerAngles.y;

            Quaternion targetRotation = Quaternion.Euler(currentEuler.x, targetY, currentEuler.z);

            bubbleTextRoot.rotation = Quaternion.Slerp(
                bubbleTextRoot.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime
            );
        }

        #endregion

        #region Visibility Checks

        /// <summary>
        /// Kiểm tra tổng điều kiện để bubble được hiện.
        /// </summary>
        protected virtual bool CanShowBubble(Transform player)
        {
            if (player == null)
                return false;

            RefreshMainCamera();

            return IsPlayerInFront(player) &&
                   IsPlayerAtValidHeight(player) &&
                   IsCameraLookingAtBubble() &&
                   IsCameraInFrontOfNpc() &&
                   IsBubbleInsideCameraView();
        }

        /// <summary>
        /// Kiểm tra player có đang đứng trước mặt NPC không.
        /// </summary>
        protected virtual bool IsPlayerInFront(Transform player)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;

            if (toPlayer.sqrMagnitude <= 0.001f)
                return false;

            float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
            return angle <= visibleAngle;
        }

        /// <summary>
        /// Kiểm tra độ cao của player có hợp lệ không.
        /// </summary>
        protected virtual bool IsPlayerAtValidHeight(Transform player)
        {
            if (_triggerCollider == null || player == null)
                return true;

            return player.position.y > _triggerCollider.bounds.min.y;
        }

        /// <summary>
        /// Kiểm tra camera có đang nhìn vào bubble không.
        /// </summary>
        protected virtual bool IsCameraLookingAtBubble()
        {
            if (!requireCameraLookAtBubble || _mainCamera == null)
                return true;

            Transform target = bubbleTextRoot != null ? bubbleTextRoot : transform;
            Vector3 toBubble = target.position - _mainCamera.transform.position;

            if (toBubble.sqrMagnitude <= 0.001f)
                return true;

            toBubble.Normalize();
            float dot = Vector3.Dot(_mainCamera.transform.forward, toBubble);
            return dot > cameraLookDotThreshold;
        }

        /// <summary>
        /// Kiểm tra camera có đang ở phía trước NPC không.
        /// </summary>
        protected virtual bool IsCameraInFrontOfNpc()
        {
            if (!requireCameraInFrontOfNpc || _mainCamera == null)
                return true;

            Vector3 toCamera = _mainCamera.transform.position - transform.position;
            toCamera.y = 0f;

            if (toCamera.sqrMagnitude <= 0.001f)
                return true;

            toCamera.Normalize();
            float dot = Vector3.Dot(transform.forward, toCamera);
            return dot > cameraFrontDotThreshold;
        }

        /// <summary>
        /// Kiểm tra bubble có đang nằm trong khung hình camera không.
        /// </summary>
        protected virtual bool IsBubbleInsideCameraView()
        {
            if (!requireBubbleInsideScreen || _mainCamera == null)
                return true;

            Transform target = bubbleTextRoot != null ? bubbleTextRoot : transform;
            Vector3 viewportPoint = _mainCamera.WorldToViewportPoint(target.position);

            if (viewportPoint.z <= 0f)
                return false;

            return viewportPoint.x >= -cameraScreenMargin &&
                   viewportPoint.x <= 1f + cameraScreenMargin &&
                   viewportPoint.y >= -cameraScreenMargin &&
                   viewportPoint.y <= 1f + cameraScreenMargin;
        }

        #endregion
    }
}