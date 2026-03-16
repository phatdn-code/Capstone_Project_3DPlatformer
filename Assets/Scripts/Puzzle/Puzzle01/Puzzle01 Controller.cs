using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class Puzzle01Controller : MonoBehaviour
    {
        [SerializeField] private GameObject[] puzzlePieces; // Array to hold references to puzzle piece GameObjects
        [SerializeField] private List<GameObject> puzzleInput = new List<GameObject>();// List to hold references to puzzle input GameObjects


        [SerializeField] private Door woodenDoor;


        [SerializeField] private bool canReset = false;

        public void AddPuzzle(GameObject piece)
        {
            puzzleInput.Add(piece);
            canReset = true;
        }


        public void ButtonPress()
        {
            if (puzzleInput.Count >= puzzlePieces.Length)
            {
                Debug.Log($"puzzleInput ={puzzleInput.Count}, puzzlePieces length = {puzzlePieces.Length} ");
                if (InCorrectOrder())
                {
                    Debug.Log("Puzzle completed in correct order!");
                    woodenDoor.OpenDoor();
                    return;
                }
            }
            ResetLog();
        }
        private bool InCorrectOrder()
        {
            return puzzlePieces.SequenceEqual(puzzleInput);
        }


        public void ResetLog()
        {
            if (!canReset)
            {
                Debug.Log("Reset is locked ¡ª skipping reset.");
                return;
            }

            foreach (var piece in puzzlePieces)
            {
                if (piece == null) continue;

                if (piece.TryGetComponent(out Mover mover))
                    mover.ResetMover();

                if (piece.TryGetComponent(out Panel panel))
                    panel.SetActivatedFalse();
            }

            puzzleInput.Clear();
            canReset = false;
        }
    }

}
