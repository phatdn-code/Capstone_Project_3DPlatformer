using UnityEngine;

[CreateAssetMenu(fileName = "PuzzleSO", menuName = "Puzzle/PieceSO", order = 1)]
public class PieceSO : ScriptableObject
{
    public int x;
    public int y;
    public Sprite pieceImage;
}
