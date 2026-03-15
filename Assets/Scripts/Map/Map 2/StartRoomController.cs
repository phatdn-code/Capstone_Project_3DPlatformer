using UnityEngine;

public class StartRoomController : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject spawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = spawnPoint.transform.position;
        }
    }
}
