using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class Puzzle03Controller : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private GameObject star;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomManager = transform.parent.GetComponentInParent<RoomManager>();
        star.GetComponent<Star>().index = 2 - GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>().StarCout;
        GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>().StarCout--;
        if (roomManager != null)
        {
            roomManager.isRoomCleared = false;
            
        }
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
