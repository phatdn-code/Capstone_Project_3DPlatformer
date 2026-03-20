using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class PlayerPuzzleController : PlayerInputManager
{
    [SerializeField] private Puzzle02Controller _controller;
    [SerializeField] private GameObject _selectionUI;

    private Puzzle02UIController _puzzle02UIController;

    private bool _canSelect = false;
    private bool _isPuzzlePlaying = false;

    protected override void Awake()
    {
        base.Awake();
        if (_selectionUI != null)
            _selectionUI.SetActive(false);
    }

    private void UpdateSelectionUI()
    {
        if (_selectionUI == null) return;

        if (_canSelect && !_isPuzzlePlaying)
            _selectionUI.SetActive(true);

        else _selectionUI.SetActive(false);
    }

    public void SelectPuzzle()
    {
        if (!_canSelect || !m_player.inputs.GetStompDown())
            return;

        _isPuzzlePlaying = !_isPuzzlePlaying;

        if (_isPuzzlePlaying)
            EnterPuzzle();

        else ExitPuzzle();
    }

    public void EnterPuzzle()
    {
        LockCursor(true);
        _puzzle02UIController?.EnterPuzzle02(this);
    }

    public void ExitPuzzle()
    {
        LockCursor(false);
        _canSelect = true;
        _isPuzzlePlaying=false;
        _puzzle02UIController?.ExitPuzzle02();
    }

    public void LockCursor(bool isLocked)
    {
        LockAllInputs(isLocked);
        Game.LockCursor(!isLocked);
        UpdateSelectionUI();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("ColliderChecking"))
            return;

        _canSelect = true;
        _puzzle02UIController = other.GetComponentInParent<Puzzle02UIController>();
        UpdateSelectionUI();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("ColliderChecking"))
            return;

        _canSelect = false;
        _isPuzzlePlaying = false;
        _puzzle02UIController = null;
        UpdateSelectionUI();
    }
}
