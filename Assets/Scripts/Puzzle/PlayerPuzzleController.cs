using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class PlayerPuzzleController : MonoBehaviour
{
    private const string k_puzzleTriggerTag = "ColliderChecking";

    #region Inspector

    [SerializeField] private GameObject _selectionUI;
    [SerializeField, Min(0f)] private float _toggleCooldown = 0.2f;

    #endregion

    #region Runtime

    private PlayerInputManager _playerInputManager;
    private Puzzle02UIController _currentPuzzleUI;
    private bool _canSelectPuzzle;
    private bool _isPuzzlePlaying;
    private float _nextToggleTime;

    #endregion

    #region Properties

    public bool IsPuzzlePlaying => _isPuzzlePlaying;

    #endregion

    #region Unity Events

    /// <summary>
    /// VN: Cache input manager chính của player và ẩn UI ban đầu.
    /// </summary>
    private void Awake()
    {
        if (_playerInputManager == null)
            _playerInputManager = GetComponent<PlayerInputManager>();

        SetSelectionUI(false);
    }

    /// <summary>
    /// VN: Khi player vào vùng puzzle thì cho phép tương tác.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!IsPuzzleTrigger(other))
            return;

        _canSelectPuzzle = true;
        _currentPuzzleUI = other.GetComponentInParent<Puzzle02UIController>();

        RefreshSelectionUI();
    }

    /// <summary>
    /// VN: Khi player ra khỏi vùng puzzle thì thoát puzzle nếu cần và dọn state.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (!IsPuzzleTrigger(other))
            return;

        if (_isPuzzlePlaying)
            ExitPuzzle();

        _canSelectPuzzle = false;
        _currentPuzzleUI = null;

        RefreshSelectionUI();
    }

    #endregion

    #region Public API

    /// <summary>
    /// VN: Được Player gọi để xử lý nhấn E vào hoặc thoát puzzle.
    /// </summary>
    public void SelectPuzzle()
    {
        if (!CanTogglePuzzle())
            return;

        if (_isPuzzlePlaying)
            ExitPuzzle();
        else
            EnterPuzzle();
    }

    /// <summary>
    /// VN: Vào chế độ puzzle, khóa gameplay và bật UI puzzle.
    /// </summary>
    public void EnterPuzzle()
    {
        if (_isPuzzlePlaying || !CanEnterPuzzle())
            return;

        _isPuzzlePlaying = true;
        SetPuzzleGameplayLock(true);
        _currentPuzzleUI.EnterPuzzle02(this);
    }

    /// <summary>
    /// VN: Thoát chế độ puzzle, mở lại gameplay và tắt UI puzzle.
    /// </summary>
    public void ExitPuzzle()
    {
        if (!_isPuzzlePlaying)
            return;

        _isPuzzlePlaying = false;
        SetPuzzleGameplayLock(false);
        _currentPuzzleUI?.ExitPuzzle02();
    }

    /// <summary>
    /// VN: Ép player thoát khỏi trạng thái puzzle từ script khác.
    /// </summary>
    public void ForceExitPuzzleState()
    {
        _isPuzzlePlaying = false;
        SetPuzzleGameplayLock(false);
    }

    #endregion

    #region Conditions

    /// <summary>
    /// VN: Kiểm tra có đủ điều kiện để đổi trạng thái puzzle hay không.
    /// </summary>
    private bool CanTogglePuzzle()
    {
        if (Time.unscaledTime < _nextToggleTime)
            return false;

        if (!CanInteractWithPuzzle())
            return false;

        bool pressed = _isPuzzlePlaying
            ? _playerInputManager != null && _playerInputManager.GetStompDownRaw()
            : _playerInputManager != null && _playerInputManager.GetStompDown();

        if (!pressed)
            return false;

        _nextToggleTime = Time.unscaledTime + _toggleCooldown;
        return true;
    }

    /// <summary>
    /// VN: Kiểm tra player có được phép tương tác puzzle ở thời điểm hiện tại không.
    /// </summary>
    private bool CanInteractWithPuzzle()
    {
        if (_isPuzzlePlaying)
            return _currentPuzzleUI != null;

        return _canSelectPuzzle && _currentPuzzleUI != null;
    }

    /// <summary>
    /// VN: Kiểm tra có thể vào puzzle hay không.
    /// </summary>
    private bool CanEnterPuzzle()
    {
        return _canSelectPuzzle && _currentPuzzleUI != null;
    }

    #endregion

    #region State & UI

    /// <summary>
    /// VN: Khóa hoặc mở gameplay bằng input manager chính của player.
    /// </summary>
    private void SetPuzzleGameplayLock(bool isLocked)
    {
        _playerInputManager?.LockGameplayInputs(isLocked);
        Game.LockCursor(!isLocked);
        RefreshSelectionUI();
    }

    /// <summary>
    /// VN: Cập nhật UI nhắc bấm E khi đứng gần puzzle.
    /// </summary>
    private void RefreshSelectionUI()
    {
        SetSelectionUI(_canSelectPuzzle && !_isPuzzlePlaying);
    }

    /// <summary>
    /// VN: Bật hoặc tắt UI chọn puzzle an toàn.
    /// </summary>
    private void SetSelectionUI(bool isVisible)
    {
        if (_selectionUI == null)
            return;

        _selectionUI.SetActive(isVisible);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// VN: Kiểm tra collider có phải vùng trigger của puzzle hay không.
    /// </summary>
    private bool IsPuzzleTrigger(Collider other)
    {
        return other != null && other.CompareTag(k_puzzleTriggerTag);
    }

    #endregion
}