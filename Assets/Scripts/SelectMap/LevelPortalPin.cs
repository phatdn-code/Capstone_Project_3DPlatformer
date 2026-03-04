using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Unity.Cinemachine;

namespace PLAYERTWO.PlatformerProject
{
    public class LevelPortalPin : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Scene")]
        [SerializeField] private string sceneName;

        [Header("Markers (UI)")]
        [SerializeField] private GameObject distantMarker; // xa: có CanvasGroup
        [SerializeField] private GameObject focusMarker;   // gần: có CanvasGroup

        [Header("Player")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode confirmKey = KeyCode.Space;

        [Header("Scale")]
        [SerializeField] private float distantScale = 0.75f;
        [SerializeField] private float focusMinScale = 0.5f;
        [SerializeField] private float focusMaxScale = 1f;

        [Header("Tween")]
        [SerializeField] private float switchDuration = 0.2f;
        [SerializeField] private Ease switchEase = Ease.OutQuad;

        [Header("Zone Camera (Optional)")]
        [SerializeField] private CinemachineCamera zoneCamera;
        [SerializeField] private int enterPriority = 100;
        [SerializeField] private int exitPriority = 0;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private CanvasGroup _distantCg;
        private CanvasGroup _focusCg;

        private Sequence _switchSeq;

        private bool _isFocused;
        private bool _playerInside;
        private bool _isLoading;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>
        /// VN: Cache CanvasGroup + set trạng thái ban đầu.
        /// </summary>
        private void Awake()
        {
            CacheReferences();
            ApplyInstantState(focused: false);
            ApplyCameraPriority(exitPriority);
        }

        /// <summary>
        /// VN: Dọn tween khi object bị disable để tránh leak/bug.
        /// </summary>
        private void OnDisable()
        {
            KillSwitchTween();
        }

        /// <summary>
        /// VN: Lắng nghe input khi player đang đứng trong vùng portal.
        /// </summary>
        private void Update()
        {
            if (_isLoading || !_playerInside) return;

            // Nếu cần bật lại:
            // if (Input.GetKeyDown(confirmKey)) ConfirmEnter();
        }

        /// <summary>
        /// VN: Player vào trigger -> focus marker + tăng priority camera.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInside = true;
            SetFocused(true);
            ApplyCameraPriority(enterPriority);
        }

        /// <summary>
        /// VN: Player ra trigger -> về distant marker + giảm priority camera.
        /// </summary>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInside = false;
            SetFocused(false);
            ApplyCameraPriority(exitPriority);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SETUP ===

        /// <summary>
        /// VN: Lấy CanvasGroup từ marker để fade mượt.
        /// </summary>
        private void CacheReferences()
        {
            _distantCg = distantMarker != null ? distantMarker.GetComponent<CanvasGroup>() : null;
            _focusCg = focusMarker != null ? focusMarker.GetComponent<CanvasGroup>() : null;
        }

        /// <summary>
        /// VN: Kiểm tra đủ component cần thiết trước khi tween.
        /// </summary>
        private bool IsMarkerReady()
        {
            return distantMarker != null
                && focusMarker != null
                && _distantCg != null
                && _focusCg != null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MARKER SWITCH ===

        /// <summary>
        /// VN: Set trạng thái focus/distant (có tween).
        /// </summary>
        private void SetFocused(bool focused)
        {
            if (_isFocused == focused) return;
            _isFocused = focused;

            if (!IsMarkerReady()) return;

            KillSwitchTween();
            _switchSeq = DOTween.Sequence();

            // VN: bật cả 2 để cross-fade không bị “cụt”
            distantMarker.SetActive(true);
            focusMarker.SetActive(true);

            if (focused)
                BuildFocusOnTween(_switchSeq);
            else
                BuildFocusOffTween(_switchSeq);
        }

        /// <summary>
        /// VN: Tween khi vào vùng (focus ON): focus scale 0.5->1 + fade in.
        /// </summary>
        private void BuildFocusOnTween(Sequence seq)
        {
            _focusCg.alpha = 0f;
            focusMarker.transform.localScale = Vector3.one * focusMinScale;

            seq.Join(_focusCg.DOFade(1f, switchDuration).SetEase(switchEase));
            seq.Join(_distantCg.DOFade(0f, switchDuration).SetEase(switchEase));

            seq.Join(focusMarker.transform.DOScale(focusMaxScale, switchDuration).SetEase(switchEase));
            seq.Join(distantMarker.transform.DOScale(distantScale, switchDuration).SetEase(switchEase));

            seq.OnComplete(() => distantMarker.SetActive(false));
        }

        /// <summary>
        /// VN: Tween khi ra vùng (focus OFF): focus scale 1->0.5 + fade out.
        /// </summary>
        private void BuildFocusOffTween(Sequence seq)
        {
            _distantCg.alpha = 0f;
            distantMarker.transform.localScale = Vector3.one * distantScale;

            seq.Join(_distantCg.DOFade(1f, switchDuration).SetEase(switchEase));
            seq.Join(_focusCg.DOFade(0f, switchDuration).SetEase(switchEase));

            seq.Join(focusMarker.transform.DOScale(focusMinScale, switchDuration).SetEase(switchEase));
            seq.Join(distantMarker.transform.DOScale(distantScale, switchDuration).SetEase(switchEase));

            seq.OnComplete(() => focusMarker.SetActive(false));
        }

        /// <summary>
        /// VN: Áp trạng thái ngay lập tức (không tween).
        /// </summary>
        private void ApplyInstantState(bool focused)
        {
            _isFocused = focused;

            if (distantMarker != null) distantMarker.SetActive(!focused);
            if (focusMarker != null) focusMarker.SetActive(focused);

            if (_distantCg != null) _distantCg.alpha = focused ? 0f : 1f;
            if (_focusCg != null) _focusCg.alpha = focused ? 1f : 0f;

            if (distantMarker != null)
                distantMarker.transform.localScale = Vector3.one * distantScale;

            if (focusMarker != null)
                focusMarker.transform.localScale = Vector3.one * (focused ? focusMaxScale : focusMinScale);
        }

        /// <summary>
        /// VN: Kill tween chuyển marker để tránh chồng tween.
        /// </summary>
        private void KillSwitchTween()
        {
            _switchSeq?.Kill();
            _switchSeq = null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SCENE LOAD ===

        /// <summary>
        /// VN: FadeOut rồi LoadScene.
        /// </summary>
        private void ConfirmEnter()
        {
            if (_isLoading) return;
            if (string.IsNullOrEmpty(sceneName)) return;

            _isLoading = true;

            if (Fader.instance == null)
            {
                SceneManager.LoadScene(sceneName);
                return;
            }

            Fader.instance.FadeOut(() => SceneManager.LoadScene(sceneName));
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CINEMACHINE ===

        /// <summary>
        /// VN: Đổi priority camera theo vùng trigger.
        /// </summary>
        private void ApplyCameraPriority(int value)
        {
            if (zoneCamera == null) return;
            zoneCamera.Priority = value;
        }

        #endregion
    }
}