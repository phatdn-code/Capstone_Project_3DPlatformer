using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle02Controller : MonoBehaviour
{
    [SerializeField] private PieceSO[] pieces; // Array of puzzle pieces // Lưu các mảnh ghép của câu đố
    [SerializeField] private GameObject puzzleSlotPrefab; // Prefab for puzzle slots // Prefab cho ô của mảnh ghép

    private int puzzleSize = 3; // Size of the puzzle (e.g., 3x3) // Kích thước của câu đố (ví dụ: 3x3)
    private PuzzleSlot[,] puzzleSlots; // 2D array to hold puzzle slots // Mảng 2D để giữ các ô mảnh ghép

    private List<PieceSO> tempPiece; // Temporary list of pieces for random assignment // Danh sách tạm thời của các mảnh ghép để gán ngẫu nhiên
    private PieceSO removePiece; // Piece removed // Mảnh ghép đã bị loại bỏ

    private Position emptySlot; // Position of the empty slot // Vị trí của ô trống
    private void Start()
    {
        if (pieces.Length == 0) return;
        tempPiece = new List<PieceSO>(pieces);
        InitializePuzzleSlot();
    }

    private void InitializePuzzleSlot() // Initialize puzzle slots based on size // Khởi tạo các ô mảnh ghép dựa trên kích thước
    {
        if ( pieces.Length < puzzleSize) 
        {
            Debug.LogError("Not enough pieces to fill the puzzle! // Không đủ mảnh để làm câu đố");
            return;
        } 
        puzzleSlots = new PuzzleSlot[puzzleSize, puzzleSize]; 
        for (int i= 0; i < puzzleSize; i++)
        {
            for (int j = 0; j < puzzleSize; j++)
            {
                GameObject slotObj = Instantiate(puzzleSlotPrefab, transform.position, Quaternion.identity);
                slotObj.transform.SetParent(this.transform);
                puzzleSlots[i, j] = slotObj.GetComponent<PuzzleSlot>();
                SetRandomPiece(puzzleSlots[i, j]);
                if (puzzleSlots[i, j].IsEmpty)
                {
                    emptySlot.x = i;
                    emptySlot.y = j;
                }
            }
        }
    }
    private void SetRandomPiece(PuzzleSlot slot)// Assign a random piece to the slot // Gán một mảnh ngẫu nhiên cho ô
    {
        if (tempPiece.Count > 1)
        {
            int i = Random.Range(0, tempPiece.Count);
            slot.SetPiece(tempPiece[i]);
            tempPiece.RemoveAt(i);
        }
        else
        {
            removePiece = tempPiece[0];
            tempPiece.Clear();
        }
    }

    public void PuzzleMove(PieceSO piece)
    {
        Debug.Log("EmptySlot: "+ emptySlot);
        if (!puzzleSlots[emptySlot.x, emptySlot.y].IsEmpty) return;

        List<Position> positions = EmptySlotFamily();
        for (int i = 0; i< positions.Count;++i)

        {
            if (puzzleSlots[positions[i].x, positions[i].y] != null)
            if (puzzleSlots[positions[i].x, positions[i].y].PieceSO == piece)
            {
                Debug.Log("Action Move Piece from "+ emptySlot + " to "+ positions[i]);
                puzzleSlots[emptySlot.x, emptySlot.y].SetPiece(piece);
                puzzleSlots[positions[i].x, positions[i].y].ResetPiece();
                emptySlot = positions[i];
            }
        }
    }

    private List<Position> EmptySlotFamily() // Get positions adjacent to the empty slot // Lấy vị trí liền kề với ô trống
    {
        List<Position> validPositions = new List<Position>();

        if (emptySlot.x > 0) // Up // Lên
        {
            Position pos = new Position { x = emptySlot.x - 1, y = emptySlot.y };
            validPositions.Add(pos);
        }
        if (emptySlot.x < puzzleSize - 1) // Down // Xuống
        {
            Position pos = new Position { x = emptySlot.x + 1, y = emptySlot.y };
            validPositions.Add(pos);
        }
        if (emptySlot.y > 0) // Left // Trái
        {
            Position pos = new Position { x = emptySlot.x, y = emptySlot.y - 1 };
            validPositions.Add(pos);
        }
        if (emptySlot.y < puzzleSize - 1) // Right // Phải
        {
            Position pos = new Position { x = emptySlot.x, y = emptySlot.y + 1 };
            validPositions.Add(pos);
        }

        return validPositions;
    }


}
struct Position
{
    public int x;
    public int y;
}
