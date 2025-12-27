using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PuzzleSlot : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private PieceSO pieceSO;
    [SerializeField] private Image pieceImage;
    [SerializeField] private Puzzle02Controller puzzleController;

    private bool isEmpty = true;
    public PieceSO PieceSO => pieceSO;
    public bool IsEmpty => isEmpty;

    private void Start()
    {
        puzzleController = gameObject.GetComponentInParent<Puzzle02Controller>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        puzzleController.MovePiece(pieceSO);
    }

    public void SetPiece(PieceSO data)
    {
        pieceSO = data;
        pieceImage.sprite = data.pieceImage;
        isEmpty = false;
    }
    public void ResetPiece()
    {
        pieceSO = null;
        pieceImage.sprite = null;
        isEmpty = true;
    }
}
