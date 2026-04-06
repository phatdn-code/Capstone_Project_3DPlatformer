using Unity.Cinemachine;
using UnityEngine;

public class Puzzle02UIController : MonoBehaviour
{
    private const int k_activeCameraPriority = 20;
    private const int k_inactiveCameraPriority = 0;

    [SerializeField] private CinemachineCamera _uiCamera;

    private PlayerPuzzleController _currentPlayerPuzzleController;

    /// <summary>
    /// VN: Vào puzzle thì lưu player controller và bật camera UI.
    /// </summary>
    public void EnterPuzzle02(PlayerPuzzleController playerPuzzleController)
    {
        _currentPlayerPuzzleController = playerPuzzleController;
        SetUICameraActive(true);
    }

    /// <summary>
    /// VN: Thoát puzzle thì tắt camera UI.
    /// </summary>
    public void ExitPuzzle02()
    {
        SetUICameraActive(false);
    }

    /// <summary>
    /// VN: Puzzle hoàn thành thì đóng UI puzzle và trả player về trạng thái bình thường.
    /// </summary>
    public void OnPuzzleCompleted()
    {
        ExitPuzzle02();
        _currentPlayerPuzzleController?.ForceExitPuzzleState();
    }

    /// <summary>
    /// VN: Bật hoặc tắt camera UI bằng cách đổi priority.
    /// </summary>
    private void SetUICameraActive(bool isActive)
    {
        if (_uiCamera == null)
            return;

        _uiCamera.Priority = isActive
            ? k_activeCameraPriority
            : k_inactiveCameraPriority;
    }
}