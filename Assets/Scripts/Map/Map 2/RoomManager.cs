using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RoomManager : MonoBehaviour
{
    public GameObject roomSpawnPoint;

    [SerializeField] private GameObject[] doors;
    [SerializeField] private GameObject[] roads;
    [SerializeField] private GameObject[] walls;
    [SerializeField] private GameObject baseCamera;
    [SerializeField] private bool isRoomCleared = false;

    private List<GameObject> activeDoors = new List<GameObject>();



    public void AddActiveDoor(int index)
    {
        if ((index > 3) || (index < 0 )) return;
        doors[index].SetActive(true);
        activeDoors.Add(doors[index]);
        roads[index].SetActive(true);
        walls[index].SetActive(false);
    }

    public void OpenAllDoors()
    {
        if (!isRoomCleared) return;
        foreach (var door in activeDoors)
        {
            //door.GetComponent<Door>().OpenDoor();
        }
    }
    public void CloseAllDoors()
    {
        if(isRoomCleared) return;
        foreach (var door in activeDoors)
        {
            //door.GetComponent<Door>().CloseDoor();
        }
    }

}
