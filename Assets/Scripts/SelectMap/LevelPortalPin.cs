using UnityEngine;
using UnityEngine.SceneManagement;
using DG.Tweening;
using Unity.Cinemachine;
using TMPro;

namespace PLAYERTWO.PlatformerProject
{
    public class LevelPortalPin : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Scene")]
        [SerializeField] private string sceneName;
        [SerializeField] private string mapDisplayName;

        [Header("Markers (UI)")]
        [SerializeField] private GameObject distantMarker;
        [SerializeField] private GameObject focusMarker;

        [Header("Map Name UI")]
        [SerializeField] private TextMeshProUGUI mapNameText;

        [Header("Stars UI")]
        [SerializeField] private GameObject starGroup;
        [SerializeField] private bool showStarGroup = true;
        [SerializeField] private GameObject[] stars;

        [Header("Player")]
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private KeyCode confirmKey = KeyCode.Space;

        [Header("Scale")]
        [SerializeField] private float distantScale = 0.75f;
        [SerializeField] private float focusMinScale = 0.5f;
        [SerializeField] private float focusMaxScale = 1f;
        [SerializeField] private float focusOvershootScale = 1.06f;

        [Header("Tween")]
        [SerializeField] private float switchDuration = 0.2f;
        [SerializeField] private Ease fadeEase = Ease.OutQuad;
        [SerializeField] private Ease focusEnterEase = Ease.OutBack;
        [SerializeField] private Ease focusExitEase = Ease.OutQuad;

        [Header("Zone Camera")]
        [SerializeField] private CinemachineCamera zoneCamera;
        [SerializeField] private int enterPriority = 100;
        [SerializeField] private int exitPriority = 0;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private CanvasGroup _distantCanvasGroup;
        private CanvasGroup _focusCanvasGroup;

        private Sequence _switchSequence;

        private bool _isFocused;
        private bool _playerInside;
        private bool _isLoading;

        private GameLevel _levelData;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>
        /// VN: Cache component, set text map, load sao và áp trạng thái ban đầu.
        /// </summary>
        private void Start()
        {
            CacheReferences();
            SetupMapNameText();
            RefreshStarGroupVisibility();
            CacheLevelData();
            RefreshStarsUI();
            ApplyInstantState(false);
            ApplyCameraPriority(exitPriority);
        }

        /// <summary>
        /// VN: Đăng ký event load save để cập nhật sao khi game nạp dữ liệu xong.
        /// </summary>
        private void OnEnable()
        {
            if (Game.instance != null)
                Game.instance.onLoadState.AddListener(HandleGameLoaded);
        }

        /// <summary>
        /// VN: Dọn tween và hủy event khi object bị disable.
        /// </summary>
        private void OnDisable()
        {
            KillSwitchTween();

            if (Game.instance != null)
                Game.instance.onLoadState.RemoveListener(HandleGameLoaded);
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
        /// VN: Player vào trigger thì bật focus và tăng priority camera.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInside = true;
            SetFocused(true);
            ApplyCameraPriority(enterPriority);
        }

        /// <summary>
        /// VN: Player ra trigger thì tắt focus và giảm priority camera.
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
        /// VN: Cache CanvasGroup từ marker để tween fade.
        /// </summary>
        private void CacheReferences()
        {
            _distantCanvasGroup = distantMarker != null ? distantMarker.GetComponent<CanvasGroup>() : null;
            _focusCanvasGroup = focusMarker != null ? focusMarker.GetComponent<CanvasGroup>() : null;
        }

        /// <summary>
        /// VN: Set text tên map một lần lúc bắt đầu.
        /// </summary>
        private void SetupMapNameText()
        {
            if (mapNameText == null) return;
            mapNameText.text = mapDisplayName;
        }

        /// <summary>
        /// VN: Bật/tắt cả cụm sao theo biến cấu hình.
        /// </summary>
        private void RefreshStarGroupVisibility()
        {
            if (starGroup == null) return;
            starGroup.SetActive(showStarGroup);
        }

        /// <summary>
        /// VN: Tìm dữ liệu level trong Game theo sceneName của portal.
        /// </summary>
        private void CacheLevelData()
        {
            _levelData = null;

            if (Game.instance == null || string.IsNullOrEmpty(sceneName))
                return;

            _levelData = Game.instance.levels.Find(level => level.scene == sceneName);
        }

        /// <summary>
        /// VN: Đếm tổng số sao đã mở của màn chơi.
        /// </summary>
        private int GetCollectedStarCount()
        {
            if (_levelData == null || _levelData.stars == null)
                return 0;

            int count = 0;

            for (int i = 0; i < _levelData.stars.Length; i++)
            {
                if (_levelData.stars[i])
                    count++;
            }

            return count;
        }

        /// <summary>
        /// VN: Cập nhật hiển thị sao theo tổng số sao đã đạt, bật từ trái sang phải.
        /// </summary>
        private void RefreshStarsUI()
        {
            RefreshStarGroupVisibility();

            if (!showStarGroup)
                return;

            if (stars == null || stars.Length == 0)
                return;

            if (_levelData == null)
                CacheLevelData();

            int collectedStars = GetCollectedStarCount();

            for (int i = 0; i < stars.Length; i++)
            {
                if (stars[i] == null) continue;
                stars[i].SetActive(i < collectedStars);
            }
        }

        /// <summary>
        /// VN: Nhận callback khi Game load save xong thì load lại sao cho portal.
        /// </summary>
        private void HandleGameLoaded(int dataIndex)
        {
            CacheLevelData();
            RefreshStarsUI();
        }

        /// <summary>
        /// VN: Kiểm tra marker đã đủ component cần thiết chưa.
        /// </summary>
        private bool IsMarkerReady()
        {
            return distantMarker != null
                && focusMarker != null
                && _distantCanvasGroup != null
                && _focusCanvasGroup != null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MARKER SWITCH ===

        /// <summary>
        /// VN: Đổi trạng thái focus/distant bằng tween mượt.
        /// </summary>
        private void SetFocused(bool focused)
        {
            if (_isFocused == focused) return;
            _isFocused = focused;

            if (!IsMarkerReady()) return;

            KillSwitchTween();
            _switchSequence = DOTween.Sequence();

            distantMarker.SetActive(true);
            focusMarker.SetActive(true);

            if (focused)
                BuildFocusOnTween(_switchSequence);
            else
                BuildFocusOffTween(_switchSequence);
        }

        /// <summary>
        /// VN: Tween khi vào vùng: focus fade in và scale 0.5 -> 1.06 -> 1.
        /// </summary>
        private void BuildFocusOnTween(Sequence sequence)
        {
            _focusCanvasGroup.alpha = 0f;
            focusMarker.transform.localScale = Vector3.one * focusMinScale;

            sequence.Join(_focusCanvasGroup.DOFade(1f, switchDuration).SetEase(fadeEase));
            sequence.Join(_distantCanvasGroup.DOFade(0f, switchDuration).SetEase(fadeEase));
            sequence.Join(distantMarker.transform.DOScale(distantScale, switchDuration).SetEase(fadeEase));

            sequence.Append(focusMarker.transform.DOScale(focusOvershootScale, switchDuration * 0.65f).SetEase(focusEnterEase));
            sequence.Append(focusMarker.transform.DOScale(focusMaxScale, switchDuration * 0.35f).SetEase(Ease.OutQuad));

            sequence.OnComplete(() => distantMarker.SetActive(false));
        }

        /// <summary>
        /// VN: Tween khi ra vùng: focus fade out và scale 1 -> 0.5.
        /// </summary>
        private void BuildFocusOffTween(Sequence sequence)
        {
            _distantCanvasGroup.alpha = 0f;
            distantMarker.transform.localScale = Vector3.one * distantScale;

            sequence.Join(_distantCanvasGroup.DOFade(1f, switchDuration).SetEase(fadeEase));
            sequence.Join(_focusCanvasGroup.DOFade(0f, switchDuration).SetEase(fadeEase));
            sequence.Join(focusMarker.transform.DOScale(focusMinScale, switchDuration).SetEase(focusExitEase));
            sequence.Join(distantMarker.transform.DOScale(distantScale, switchDuration).SetEase(fadeEase));

            sequence.OnComplete(() => focusMarker.SetActive(false));
        }

        /// <summary>
        /// VN: Áp trạng thái marker ngay lập tức, không tween.
        /// </summary>
        private void ApplyInstantState(bool focused)
        {
            _isFocused = focused;

            if (distantMarker != null) distantMarker.SetActive(!focused);
            if (focusMarker != null) focusMarker.SetActive(focused);

            if (_distantCanvasGroup != null) _distantCanvasGroup.alpha = focused ? 0f : 1f;
            if (_focusCanvasGroup != null) _focusCanvasGroup.alpha = focused ? 1f : 0f;

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
            _switchSequence?.Kill();
            _switchSequence = null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SCENE LOAD ===

        /// <summary>
        /// VN: FadeOut rồi chuyển scene.
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
        #region === CAMERA ===

        /// <summary>
        /// VN: Đổi priority camera khi vào/ra vùng trigger.
        /// </summary>
        private void ApplyCameraPriority(int value)
        {
            if (zoneCamera == null) return;
            zoneCamera.Priority = value;
        }

        #endregion
    }
}