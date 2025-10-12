using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class Puzzle01Controller : MonoBehaviour
{
    [SerializeField] private GameObject[] puzzlePieces; // Array to hold references to puzzle piece GameObjects


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetLog()
    {
        for (int i = 0; i < puzzlePieces.Length; i++)
        {
            puzzlePieces[i].TryGetComponent<Mover>(out var mover);
            mover.ResetMover();
        }
    }
}
