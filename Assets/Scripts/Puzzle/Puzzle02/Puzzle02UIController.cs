using Unity.Cinemachine;
using UnityEngine;

public class Puzzle02UIController : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _uiCamera;
    [SerializeField] private GameObject _eventSystem;

    private PlayerPuzzleController _playerPuzzleController;


    public void EnterPuzzle02(PlayerPuzzleController _input)
    {
        _playerPuzzleController = _input;
        _uiCamera.Priority = 20;
        _eventSystem.SetActive(true);
    }

    public void ExitPuzzle02()
    {
        _uiCamera.Priority = 0;
        _eventSystem.SetActive(false);
    }
    public void OnPuzzleCompleted()
    {
        ExitPuzzle02();
        _playerPuzzleController?.LockCursor(false);
    }
}
