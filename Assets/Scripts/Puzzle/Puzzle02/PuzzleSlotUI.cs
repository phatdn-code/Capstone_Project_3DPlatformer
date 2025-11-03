using UnityEngine;
using UnityEngine.UI;

public class PuzzleSlotUI : MonoBehaviour
{
    [SerializeField] private PieceSO pieceSO;
    [SerializeField] private Image pieceImage;

    private void Start()
    {
        pieceImage.sprite = pieceSO.pieceImage;
    }
}
