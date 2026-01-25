using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RoomGenerator : MonoBehaviour
{
    [SerializeField] private GameObject[] doors;
    [SerializeField] private GameObject[] roads;
    [SerializeField] private GameObject[] walls;

    private List<GameObject> activeDoors;



    public void AddActiveDoor(int index)
    {
        if ((index > 3) || (index < 0 )) return;
        doors[index].SetActive(true);
        //activeDoors.Add(doors[index]);
        roads[index].SetActive(true);
        walls[index].SetActive(false);
    }
}
