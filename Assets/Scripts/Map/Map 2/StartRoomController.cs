using UnityEngine;

public class StartRoomController : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject spawnPoint;
    [SerializeField] private GameObject UIPrefab;
    [SerializeField] private GameObject UISpawnPoint;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Instantiate(playerPrefab, spawnPoint.transform.position, Quaternion.identity);
        UISpawnPoint = GameObject.Find("===SYSTEM===");
        if (UISpawnPoint != null)
        {
            Instantiate(UIPrefab, UISpawnPoint.transform.position, Quaternion.identity, UISpawnPoint.transform);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
