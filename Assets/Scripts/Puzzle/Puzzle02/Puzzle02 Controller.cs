using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class Puzzle02Controller : MonoBehaviour
{
    [SerializeField] private PieceSO[] _pieces;
    [SerializeField] private GameObject _puzzleSlotPrefab;
    [SerializeField] private Puzzle02UIController _uiController;
    [SerializeField] private GameObject _objPuzzle02;
    [SerializeField] private GameObject _puzzleCanvas;
    [SerializeField] private GameObject _objBlock;

    [SerializeField, Min(3)] private int _puzzleSize = 3;

    private PuzzleSlot[,] _puzzleSlots;
    private List<PieceSO> _remainingPieces;
    private PieceSO _removedPiece;
    private Position _emptySlot;
    private bool _isCompleted = false;

    private void Start()
    {
        if (_pieces == null || _pieces.Length == 0) return;

        _remainingPieces = new List<PieceSO>(_pieces);
        InitializePuzzleSlots();
    }

    private void InitializePuzzleSlots()
    {
        if (_pieces.Length < _puzzleSize * _puzzleSize)
        {
            Debug.LogError("Not enough pieces to fill the puzzle!");
            return;
        }

        _puzzleSlots = new PuzzleSlot[_puzzleSize, _puzzleSize];

        for (int x = 0; x < _puzzleSize; x++)
        {
            for (int y = 0; y < _puzzleSize; y++)
            {
                GameObject slotObj = Instantiate(_puzzleSlotPrefab, transform.position, Quaternion.identity, transform);
                _puzzleSlots[x, y] = slotObj.GetComponent<PuzzleSlot>();
                AssignPieceToSlot(_puzzleSlots[x, y]);
            }
        }

        CreateEmptySlot();
        ShufflePuzzle();
    }

    private void AssignPieceToSlot(PuzzleSlot slot)
    {
        slot.SetPiece(_remainingPieces[0]);
        _remainingPieces.RemoveAt(0);
    }

    private void CreateEmptySlot()
    {
        int x = Random.Range(0, _puzzleSize);
        int y = Random.Range(0, _puzzleSize);

        _removedPiece = _puzzleSlots[x, y].PieceSO;
        _puzzleSlots[x, y].ResetPiece();
        _emptySlot = new Position { x = x, y = y };
    }

    private void ShufflePuzzle()
    {
        int moves = Random.Range(10, 100);
        for (int i = 0; i < moves; i++)
        {
            var neighbors = GetAdjacentPositions(_emptySlot);
            var randomPos = neighbors[Random.Range(0, neighbors.Count)];

            SwapWithEmpty(randomPos);
        }
    }

    private void SwapWithEmpty(Position pos)
    {
        _puzzleSlots[_emptySlot.x, _emptySlot.y].SetPiece(_puzzleSlots[pos.x, pos.y].PieceSO);
        _puzzleSlots[pos.x, pos.y].ResetPiece();
        _emptySlot = pos;
    }

    public void MovePiece(PieceSO piece)
    {
        if (_isCompleted) return;

        var neighbors = GetAdjacentPositions(_emptySlot);
        foreach (var pos in neighbors)
        {
            if (_puzzleSlots[pos.x, pos.y].PieceSO == piece)
            {
                SwapWithEmpty(pos);
                break;
            }
        }

        CheckCompletion();
    }

    private void CheckCompletion()
    {
        for (int x = 0; x < _puzzleSize; x++)
        {
            for (int y = 0; y < _puzzleSize; y++)
            {
                var slot = _puzzleSlots[x, y];
                if (slot.IsEmpty)
                {
                    if (_removedPiece.x != x || _removedPiece.y != y) return;
                }
                else if (slot.PieceSO.x != x || slot.PieceSO.y != y)
                {
                    return;
                }
            }
        }

        _isCompleted = true;
        OnPuzzleCompleted();
    }

    private void OnPuzzleCompleted()
    {
        _puzzleSlots[_emptySlot.x, _emptySlot.y].SetPiece(_removedPiece);
        StartCoroutine(WaitOneSecondForDisiablePuzzlePanel());
        StartCoroutine(WaitOneSecondForDisiablePuzzleObject());
    }

    private IEnumerator WaitOneSecondForDisiablePuzzlePanel()
    {
        yield return new WaitForSeconds(1f);
        if (_puzzleCanvas != null)
        {
            _puzzleCanvas.GetComponent<Canvas>().enabled = false;
        }
        if (_objBlock != null)
        {
            _objBlock?.SetActive(false);
        }
    }
    private IEnumerator WaitOneSecondForDisiablePuzzleObject()
    {
        yield return new WaitForSeconds(1f);
        _uiController?.OnPuzzleCompleted();
        if (_objPuzzle02 != null)
        {
            _objPuzzle02?.SetActive(false);
        }
    }


    private List<Position> GetAdjacentPositions(Position pos)
    {
        var positions = new List<Position>();

        if (pos.x > 0) positions.Add(new Position { x = pos.x - 1, y = pos.y });
        if (pos.x < _puzzleSize - 1) positions.Add(new Position { x = pos.x + 1, y = pos.y });
        if (pos.y > 0) positions.Add(new Position { x = pos.x, y = pos.y - 1 });
        if (pos.y < _puzzleSize - 1) positions.Add(new Position { x = pos.x, y = pos.y + 1 });

        return positions;
    }
}

public struct Position
{
    public int x;
    public int y;
}
