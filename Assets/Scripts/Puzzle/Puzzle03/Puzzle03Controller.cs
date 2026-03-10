using UnityEngine;

public class Puzzle03Controller : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomManager = transform.parent.GetComponentInParent<RoomManager>();
    }


    public void ButtonDown()
    {
        if (roomManager != null)
        {
            roomManager.isRoomCleared = true;
            roomManager.OpenAllDoors();
        }
    }
    public void ButtonUp()
    {
        if (roomManager != null)
        {
            roomManager.isRoomCleared = false;
            roomManager.CloseAllDoors();
        }
    }
}
