using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using Unity.Cinemachine;
using TMPro;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    public class LevelPortalPin : MonoBehaviour, IPortalReturnPoint
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR ===

        [TitleGroup("Level Portal Pin")]
        [BoxGroup("Level Portal Pin/Scene")]
        [SerializeField] private string targetSceneName;

        [BoxGroup("Level Portal Pin/Scene")]
        [ToggleLeft]
        [InfoBox("Bật nếu khi đi qua portal này sẽ xóa return point đang chờ của scene đích.")]
        [SerializeField] private bool clearTargetSceneReturnPoint = false;

        [BoxGroup("Level Portal Pin/Scene")]
        [InfoBox("Danh sách ID minigame sẽ bị reset khi portal này thực hiện clear target scene return point.")]
        [SerializeField] private string[] miniGameIdsToReset;

        [BoxGroup("Level Portal Pin/Scene")]
        [SerializeField] private string mapDisplayName;

        [BoxGroup("Level Portal Pin/Return Spawn")]
        [ToggleLeft]
        [InfoBox("Bật nếu portal này được dùng làm điểm quay lại khi trở về map.")]
        [SerializeField] private bool useAsReturnPoint = true;

        [BoxGroup("Level Portal Pin/Return Spawn")]
        [ShowIf(nameof(useAsReturnPoint))]
        [InfoBox("ID duy nhất của điểm quay lại. Có thể đặt theo sceneName nếu mỗi portal chỉ vào 1 scene.")]
        [SerializeField] private string returnPointId;

        [BoxGroup("Level Portal Pin/Return Spawn")]
        [ShowIf(nameof(useAsReturnPoint))]
        [InfoBox("Point player sẽ đứng khi quay lại map. Nếu để trống sẽ dùng chính transform của portal.")]
        [SerializeField] private Transform returnPoint;

        [BoxGroup("Level Portal Pin/Unlock")]
        [ToggleLeft]
        [InfoBox("Bật nếu đây là map mở sẵn ngay từ đầu, ví dụ map đầu tiên.")]
        [SerializeField] private bool unlockByDefault = false;

        [BoxGroup("Level Portal Pin/Unlock")]
        [ShowIf("@!unlockByDefault")]
        [InfoBox("Scene name của map cần clear trước để mở khóa map này.")]
        [SerializeField] private string requiredClearedMapScene;

        [BoxGroup("Level Portal Pin/Unlock")]
        [InfoBox("Sprite[0] = Locked, Sprite[1] = Unlocked.")]
        [PreviewField(70, ObjectFieldAlignment.Left)]
        [SerializeField] private Sprite[] distantFrameSprites;

        [BoxGroup("Level Portal Pin/Unlock")]
        [Required("Thiếu Image frame của distantMarker.")]
        [SerializeField] private Image distantMarkerFrame;

        [BoxGroup("Level Portal Pin/Unlock")]
        [ReadOnly]
        [ShowInInspector]
        [LabelText("Is Unlocked (Runtime)")]
        private bool Inspector_IsUnlocked => _isUnlocked;

        [BoxGroup("Level Portal Pin/Markers")]
        [Required("Thiếu distantMarker.")]
        [SerializeField] private GameObject distantMarker;

        [BoxGroup("Level Portal Pin/Markers")]
        [Required("Thiếu focusMarker.")]
        [SerializeField] private GameObject focusMarker;

        [BoxGroup("Level Portal Pin/UI")]
        [SerializeField] private TextMeshProUGUI mapNameText;

        [BoxGroup("Level Portal Pin/UI")]
        [SerializeField] private GameObject starGroup;

        [BoxGroup("Level Portal Pin/UI")]
        [ToggleLeft]
        [SerializeField] private bool showStarGroup = true;

        [BoxGroup("Level Portal Pin/UI")]
        [SerializeField] private GameObject[] stars;

        [BoxGroup("Level Portal Pin/Player")]
        [SerializeField] private string playerTag = "Player";

        [BoxGroup("Level Portal Pin/Scale")]
        [SerializeField] private float distantScale = 0.75f;

        [BoxGroup("Level Portal Pin/Scale")]
        [SerializeField] private float focusMinScale = 0.5f;

        [BoxGroup("Level Portal Pin/Scale")]
        [SerializeField] private float focusMaxScale = 1f;

        [BoxGroup("Level Portal Pin/Scale")]
        [SerializeField] private float focusOvershootScale = 1.06f;

        [BoxGroup("Level Portal Pin/Tween")]
        [SerializeField] private float switchDuration = 0.2f;

        [BoxGroup("Level Portal Pin/Tween")]
        [SerializeField] private Ease fadeEase = Ease.OutQuad;

        [BoxGroup("Level Portal Pin/Tween")]
        [SerializeField] private Ease focusEnterEase = Ease.OutBack;

        [BoxGroup("Level Portal Pin/Tween")]
        [SerializeField] private Ease focusExitEase = Ease.OutQuad;

        [BoxGroup("Level Portal Pin/Zone Camera")]
        [SerializeField] private CinemachineCamera zoneCamera;

        [BoxGroup("Level Portal Pin/Zone Camera")]
        [SerializeField] private int enterPriority = 100;

        [BoxGroup("Level Portal Pin/Zone Camera")]
        [SerializeField] private int exitPriority = 0;

        [BoxGroup("Level Portal Pin/Boss")]
        [ToggleLeft]
        [InfoBox("Bật nếu portal này là portal của màn Boss.")]
        [SerializeField] private bool isBossPortal = false;

        [BoxGroup("Level Portal Pin/Boss")]
        [ShowIf(nameof(isBossPortal))]
        [Required("Thiếu BossMaterialSwitcher.")]
        [SerializeField] private BossMaterialSwitcher bossMaterialSwitcher;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private CanvasGroup _distantCanvasGroup;
        private CanvasGroup _focusCanvasGroup;

        private Sequence _switchSequence;

        private bool _isFocused;
        private bool _playerInside;
        private bool _isLoading;
        private bool _isUnlocked;

        private GameLevel _levelData;

        #endregion

        //─────────────────────────────────────────────
        #region === INTERFACE ===

        public Transform ReturnPoint => returnPoint != null ? returnPoint : transform;

        public string ReturnPointId => string.IsNullOrEmpty(returnPointId) ? targetSceneName : returnPointId;

        public bool UseAsReturnPoint => useAsReturnPoint;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY ===

        private void Awake()
        {
            CacheReferences();
        }

        private void Start()
        {
            SetupMapNameText();
            RefreshStarGroupVisibility();
            CacheLevelData();
            RefreshStarsUI();
            RefreshUnlockState();
            RefreshBossVisual();
            ApplyInstantState(false);
            ApplyCameraPriority(exitPriority);
        }

        /// <summary>
        /// VN: Đăng ký event load save để cập nhật sao và trạng thái mở khóa khi game nạp dữ liệu xong.
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
            if (_isLoading || !_playerInside || !_isUnlocked) return;
            if (PlayerHub.Instance == null || PlayerHub.Instance.InputManager == null) return;

            if (PlayerHub.Instance.InputManager.GetStompDown())
                ConfirmEnter();
        }

        /// <summary>
        /// VN: Player vào trigger, nếu map đã mở khóa thì bật focus, chưa mở khóa thì giữ distant.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;

            _playerInside = true;

            RefreshUnlockState();
            ApplyCameraPriority(enterPriority);

            if (!_isUnlocked)
            {
                SetFocused(false);
                return;
            }

            SetFocused(true);
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

            if (Game.instance == null || string.IsNullOrEmpty(targetSceneName))
                return;

            _levelData = Game.instance.levels.Find(level => level.scene == targetSceneName);
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
        /// VN: Nhận callback khi Game load save xong thì cập nhật lại portal.
        /// </summary>
        private void HandleGameLoaded(int dataIndex)
        {
            CacheLevelData();
            RefreshStarsUI();
            RefreshUnlockState();
            RefreshBossVisual();
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

        /// <summary>
        /// VN: Kiểm tra map này đã được mở khóa chưa dựa trên cấu hình mở sẵn hoặc map điều kiện đã clear.
        /// </summary>
        private void RefreshUnlockState()
        {
            _isUnlocked = IsMapUnlocked();
            RefreshDistantFrame();
        }

        /// <summary>
        /// VN: Nếu đây là boss portal thì đổi material boss theo trạng thái đã clear map hay chưa.
        /// </summary>
        private void RefreshBossVisual()
        {
            if (!isBossPortal)
                return;

            if (bossMaterialSwitcher == null)
                return;

            if (_levelData == null)
                CacheLevelData();

            bool wasDefeated = _levelData != null && _levelData.wasCompletedOnce;
            bossMaterialSwitcher.ApplyState(wasDefeated);
        }

        /// <summary>
        /// VN: Nếu bật mở sẵn thì map được mở khóa ngay từ đầu, ngược lại kiểm tra map điều kiện đã clear chưa.
        /// </summary>
        private bool IsMapUnlocked()
        {
            if (unlockByDefault)
                return true;

            if (string.IsNullOrEmpty(requiredClearedMapScene))
                return false;

            if (Game.instance == null || Game.instance.levels == null)
                return false;

            GameLevel requiredLevel = Game.instance.levels.Find(level => level.scene == requiredClearedMapScene);

            if (requiredLevel == null)
                return false;

            return requiredLevel.wasCompletedOnce;
        }

        /// <summary>
        /// VN: Đổi sprite frame của distant marker theo trạng thái khóa / mở khóa.
        /// </summary>
        private void RefreshDistantFrame()
        {
            if (distantMarkerFrame == null)
                return;

            if (distantFrameSprites == null || distantFrameSprites.Length < 2)
                return;

            distantMarkerFrame.sprite = _isUnlocked ? distantFrameSprites[1] : distantFrameSprites[0];
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
        /// VN: Ghi nhớ điểm quay lại trước khi rời scene hiện tại.
        /// </summary>
        private void RememberReturnPoint()
        {
            if (!UseAsReturnPoint)
                return;

            if (Game.instance == null)
                return;

            string currentScene = SceneManager.GetActiveScene().name;
            Game.instance.SetPendingReturnPoint(currentScene, ReturnPointId);
        }

        /// <summary>
        /// VN: Nếu bật cờ này thì xóa return point đang chờ của scene đích
        /// và reset các minigame ID đã cấu hình.
        /// </summary>
        private void ClearTargetSceneReturnPointIfNeeded()
        {
            if (!clearTargetSceneReturnPoint)
                return;

            if (Game.instance == null)
                return;

            if (!string.IsNullOrEmpty(targetSceneName))
                Game.instance.ClearPendingReturnPoint(targetSceneName);

            Game.instance.ResetMiniGamesCompleted(miniGameIdsToReset);
        }

        /// <summary>
        /// VN: Xử lý khi người chơi xác nhận đi vào portal.
        /// </summary>
        private void ConfirmEnter()
        {
            if (!CanEnterPortal()) return;

            _isLoading = true;

            PrepareForEnter();

            ClearTargetSceneReturnPointIfNeeded();
            RememberReturnPoint();
            LoadTargetScene();

            AudioManager.Instance.PlaySound(1);
        }

        /// <summary>
        /// VN: Kiểm tra portal có đủ điều kiện để vào hay không.
        /// </summary>
        private bool CanEnterPortal()
        {
            return !_isLoading &&
                   _isUnlocked &&
                   !string.IsNullOrEmpty(targetSceneName);
        }

        /// <summary>
        /// VN: Khóa các trạng thái cần thiết trước khi chuyển scene.
        /// </summary>
        private void PrepareForEnter()
        {
            if (LevelPauser.instance != null)
            {
                LevelPauser.instance.Pause(false);
                LevelPauser.instance.canPause = false;
            }

            PlayerHub.Instance?.LockPlayer(true);
        }

        /// <summary>
        /// VN: Load scene đích, ưu tiên đi qua GameLoader nếu có.
        /// </summary>
        private void LoadTargetScene()
        {
            if (GameLoader.instance != null)
            {
                GameLoader.instance.Load(targetSceneName);
                return;
            }

            Fader.instance?.FadeOut(() => SceneManager.LoadScene(targetSceneName));
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

#if UNITY_EDITOR
        //─────────────────────────────────────────────
        #region === ODIN DEBUG ===

        [BoxGroup("Level Portal Pin/Debug")]
        [Button("Refresh Unlock State")]
        private void OdinRefreshUnlockState()
        {
            CacheLevelData();
            RefreshUnlockState();
        }

        [BoxGroup("Level Portal Pin/Debug")]
        [Button("Apply Locked Frame")]
        private void OdinApplyLockedFrame()
        {
            if (distantMarkerFrame == null) return;
            if (distantFrameSprites == null || distantFrameSprites.Length < 1) return;

            distantMarkerFrame.sprite = distantFrameSprites[0];
        }

        [BoxGroup("Level Portal Pin/Debug")]
        [Button("Apply Unlocked Frame")]
        private void OdinApplyUnlockedFrame()
        {
            if (distantMarkerFrame == null) return;
            if (distantFrameSprites == null || distantFrameSprites.Length < 2) return;

            distantMarkerFrame.sprite = distantFrameSprites[1];
        }

        #endregion
#endif
    }
}