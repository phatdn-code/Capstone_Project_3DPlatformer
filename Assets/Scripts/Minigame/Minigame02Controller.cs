using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class Minigame02Controller : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private GameObject star;
    [SerializeField] private SphereCollider starCollider;
    [SerializeField] private GameObject miniGameObj;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        roomManager = transform.parent.GetComponentInParent<RoomManager>();
        star.GetComponent<Star>().index = 2 - GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>().StarCout;
        starCollider = star.GetComponent<SphereCollider>();
        GameObject.FindGameObjectWithTag("DungeonController").GetComponent<DungeonController>().StarCout--;
        if (roomManager != null)
        {
            roomManager.isRoomCleared = false;

        }
    }
    // Update is called once per frame
    void FixedUpdate()
    {
        if (starCollider.enabled == false)
        {
            MinigameCleared();
        }
    }
    
    private void MinigameCleared()
    {
        roomManager.isRoomCleared = true;
        roomManager.OpenAllDoors();
        miniGameObj.SetActive(false);
        this.enabled = false;
    }
}
