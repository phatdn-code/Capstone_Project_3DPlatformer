using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Puzzle02Controller : MonoBehaviour
{
    [SerializeField] private PieceSO[] pieces; // Array of puzzle pieces // Lưu các mảnh ghép của câu đố
    [SerializeField] private GameObject puzzleSlotPrefab; // Prefab for puzzle slots // Prefab cho ô của mảnh ghép

    private int puzzleSize = 3; // Size of the puzzle (e.g., 3x3) // Kích thước của câu đố (ví dụ: 3x3)
    private PuzzleSlot[,] puzzleSlots= new PuzzleSlot[3,3]; // 2D array to hold puzzle slots // Mảng 2D để giữ các ô mảnh ghép

    private List<PieceSO> tempPiece = new List<PieceSO>(9); // Temporary list of pieces for random assignment // Danh sách tạm thời của các mảnh ghép để gán ngẫu nhiên
    private PieceSO removePiece; // Piece removed // Mảnh ghép đã bị loại bỏ

    private Position emptySlot; // Position of the empty slot // Vị trí của ô trống

    private bool isCompleted = false; // Flag to check if the puzzle is completed // Cờ để kiểm tra xem câu đố đã hoàn thành chưa
    private void Start()
    {
        if (pieces.Length == 0) return;
        tempPiece = new List<PieceSO>(pieces);
        InitializePuzzleSlot();
    }

    private void InitializePuzzleSlot() // Initialize puzzle slots based on size // Khởi tạo các ô mảnh ghép dựa trên kích thước
    {
        if (pieces.Length < puzzleSize * puzzleSize)
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
                SetPiece(puzzleSlots[i, j]);
                if (puzzleSlots[i, j].IsEmpty)
                {
                    emptySlot.x = i;
                    emptySlot.y = j;
                }
            }
        }
        GetRandomEmptyPiece();
        RandomMove();
    }
    private void SetPiece(PuzzleSlot slot)// Set piece to slot // Gán mảnh ghép vào ô
    {
            slot.SetPiece(tempPiece[0]);
            tempPiece.RemoveAt(0);
    }

    private void GetRandomEmptyPiece() // Get a random piece to be the empty slot // Lấy một mảnh ngẫu nhiên để làm ô trống
    {
        int randomX = Random.Range(0, puzzleSize);
        int randomY = Random.Range(0, puzzleSize);
        removePiece = puzzleSlots[randomX, randomY].PieceSO;
        puzzleSlots[randomX, randomY].ResetPiece();
        emptySlot.x = randomX;
        emptySlot.y = randomY;
    }

    private void  RandomMove()// Randomly move pieces to shuffle the puzzle // Di chuyển ngẫu nhiên các mảnh để xáo trộn câu đố
    {
        int randomMoves = Random.Range(10, 100);
        int lastMoveIndex = -1;
        int randomIndex = -1;
        for (int i = 0; i < randomMoves; i++)
        {
            List<Position> positions = EmptySlotFamily();
            
            switch (lastMoveIndex)
            {
                case 0:
                case 1:
                    if (positions.Count < 4)
                    {
                        randomIndex = Random.Range(0, positions.Count);
                    }
                    else
                    {
                        randomIndex = Random.Range(2, 3);
                       
                    }
                    break;
                case 2:
                case 3:
                    if (positions.Count < 4)
                    {
                        randomIndex = Random.Range(0, positions.Count);
                    }
                    else
                    {
                        randomIndex = Random.Range(0, 1);

                    }
                    break;
                default: randomIndex = Random.Range(0, positions.Count); break;
            }
            Position pos = positions[randomIndex];
            puzzleSlots[emptySlot.x, emptySlot.y].SetPiece(puzzleSlots[pos.x, pos.y].PieceSO);
            puzzleSlots[pos.x, pos.y].ResetPiece();
            emptySlot = pos;
            lastMoveIndex = randomIndex;
        }

    }

    public void PuzzleMove(PieceSO piece)
    {
        if(isCompleted) return;
        //Debug.Log("EmptySlot: "+ emptySlot);
        if (!puzzleSlots[emptySlot.x, emptySlot.y].IsEmpty) return;

        List<Position> positions = EmptySlotFamily();
        for (int i = 0; i< positions.Count;++i)

        {
            if (puzzleSlots[positions[i].x, positions[i].y] != null)
            if (puzzleSlots[positions[i].x, positions[i].y].PieceSO == piece)
            {
                //Debug.Log("Action Move Piece from "+ emptySlot + " to "+ positions[i]);
                puzzleSlots[emptySlot.x, emptySlot.y].SetPiece(piece);
                puzzleSlots[positions[i].x, positions[i].y].ResetPiece();
                emptySlot = positions[i];
                break;
            }
        }
        CheckComplete();
    }

    private void CheckComplete()
    {
        if (isCompleted) return;
        for (int i = 0; i < puzzleSize; i++)
        {
            for (int j = 0; j < puzzleSize; j++)
            {
                if (puzzleSlots[i, j].IsEmpty)
                {
                    if (removePiece.x != i || removePiece.y != j)
                    {
                        return;
                    }
                }
                else
                {
                    if(puzzleSlots[i, j].PieceSO.x != i || puzzleSlots[i, j].PieceSO.y != j)
                    {
                        return;
                    }
                }
            }
        }
        isCompleted = true;
        CompletedPuzzle();
    }

    private void CompletedPuzzle()
    {
        if(!isCompleted) return;
        // Handle puzzle completion logic here // Xử lý logic khi câu đố hoàn thành
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
