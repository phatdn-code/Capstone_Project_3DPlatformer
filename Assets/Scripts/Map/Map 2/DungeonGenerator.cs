using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Collections;

public class DungeonGenerator : MonoBehaviour
{
    public int roomLength = 10; // số phòng theo chiều dài
    public float roomSize = 59.05f; // đúng bằng size prefab
    public int maxRoomCount = 30;
    public GameObject roomPrefab;
    public enum CellType { Empty, Room}
    [SerializeField] private DungeonSO dungeonDataSO;

    [SerializeField] private GameObject[] roomObjArray = new GameObject[4];
    private int minRoomCount = 10;
    private int roomCount = 0;
    private CellType[,] dungeons;
    private List<Vector2Int> roomList = new List<Vector2Int>();
    

    private static Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    void Start()
    {
        ClearDungeon();
        RoomCount();
        ChooseStartRoom();
    }

    void ClearDungeon()
    {
        dungeonDataSO.ResetData();
        dungeons = new CellType[roomLength, roomLength];
        roomList.Clear();
    }
    void RoomCount()
    {

        if (maxRoomCount <= (roomSize * roomSize))
        {
            roomCount = Random.Range(minRoomCount, maxRoomCount);
        }
        else
        {
            roomCount = Random.Range(minRoomCount, roomLength * roomLength);
        }
        Debug.Log("Total Room Count: " + roomCount);


    }

    void ChooseStartRoom()
    {
        int indexX = Random.Range(0, roomLength);
        int indexY = Random.Range(0, roomLength);
        dungeonDataSO.startIndex = new Vector2Int(indexX, indexY);
        GameObject room = SpawnBaseRoom(dungeonDataSO.startIndex);
        SpawnRoom(room, 30);
        roomCount--;
        StartCoroutine(NextGenerateCoroutine(dungeonDataSO.startIndex,room));
    }


    void NextGenerate(Vector2Int index, GameObject room)
    {
        if (roomCount <= 0) return;
        Vector2Int[] availableDirection = CheckDirection(index);
        if (availableDirection.Length == 0) return;
        else
        {
            int count = Random.Range(1, availableDirection.Length + 1);
            for (int i = availableDirection.Length - 1; i > 0; i--) //shuffle
            {
                int j = Random.Range(0, i + 1);
                (availableDirection[i], availableDirection[j]) = (availableDirection[j], availableDirection[i]);
            }
            for (int i = 0; i < count; i++)
            {
                roomCount--;
                if (roomCount < 0) return;
                SetDirectionDoorActive(room, availableDirection[i] - index);
                GameObject newRoom = SpawnBaseRoom(availableDirection[i]);
                SpawnRoom(newRoom, roomCount);
                SetDirectionDoorActive(newRoom, index - availableDirection[i]);
                StartCoroutine(NextGenerateCoroutine(availableDirection[i], newRoom));
            }
        }
    }

    IEnumerator NextGenerateCoroutine(Vector2Int index, GameObject room)
    {
        yield return new WaitForSeconds(0.5f);
        NextGenerate(index,room);
    }

    void SetDirectionDoorActive(GameObject room,Vector2Int dir )
    {
        if ((room == null) || (dir == Vector2Int.zero)) return;
        room.GetComponent<RoomManager>().AddActiveDoor(DoorDir(dir));
    }
    int DoorDir(Vector2Int dir)
    {
        if(dir == Vector2Int.up) return 0;
        else if (dir == Vector2Int.down) return 1;
        else if (dir == Vector2Int.left) return 2;
        else if (dir == Vector2Int.right) return 3;
        else return -1;
    }

    bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < roomLength 
            && pos.y >= 0 && pos.y < roomLength;
    }
    Vector2Int[] CheckDirection(Vector2Int index)
    {
        List<Vector2Int> availDirs = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            if (InBounds(index + 1 * dir))
            {
                if(dungeons[index.x + dir.x, index.y + dir.y] == CellType.Empty)
                {
                    availDirs.Add(index + dir);
                }
            }
        }
        Vector2Int[] availableDirection = availDirs.ToArray();
        return availableDirection;
    }
    GameObject SpawnBaseRoom(Vector2Int pos)
    {
        if (!InBounds(pos)) return null;
        if (dungeons[pos.x, pos.y] != CellType.Empty) return null;

        dungeons[pos.x, pos.y] = CellType.Room;
        roomList.Add(pos);

        return Instantiate(roomPrefab, new Vector3(pos.x * roomSize, 0, pos.y * roomSize), Quaternion.identity, transform);
    }

    void SpawnRoom(GameObject location, int value)
    {
        GameObject room =null;
        if (location == null) return;
        if (value == 30)
        {
            if (roomObjArray[0] == null) return;
            room = roomObjArray[0];
        }

        if (value == 0)
        {
            if (roomObjArray[1] == null) return;
            room = roomObjArray[1];
        }

        if(room == null) return;
        GameObject roomParent = location.GetComponent<RoomManager>().roomSpawnPoint;
        Instantiate(room, roomParent.transform.position, Quaternion.identity, roomParent.transform);
    }
}
