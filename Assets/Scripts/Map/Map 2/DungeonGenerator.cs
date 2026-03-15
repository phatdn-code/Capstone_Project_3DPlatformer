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
    [SerializeField] private DungeonController dungeonController;

    [SerializeField] private GameObject[] roomObjArray = new GameObject[2];
    [SerializeField] private GameObject[] puzzleRoomObjArray = new GameObject[4];
    [SerializeField] private GameObject[] minigameRoomObjArray = new GameObject[2];
    [SerializeField] private GameObject[] enemyRoomObjArray = new GameObject[2];
    private int minRoomCount = 7;
    private int roomCount = 0;
    private CellType[,] dungeons;
    private List<Vector2Int> roomList = new List<Vector2Int>();

    [SerializeField] private int puzzleRoomCount = 2;
    [SerializeField] private int minigameRoomCount = 1;



    private static Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    private void Start()
    {
        ClearDungeon();
        RoomCount();
        puzzleRoomObjArray = ShuffleArray(puzzleRoomObjArray);
        ChooseStartRoom();
    }

    private void ClearDungeon()
    {
        dungeonDataSO.ResetData();
        dungeons = new CellType[roomLength, roomLength];
        roomList.Clear();
    }
    private void RoomCount()
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

    private void ChooseStartRoom()
    {
        int indexX = Random.Range(0, roomLength);
        int indexY = Random.Range(0, roomLength);
        dungeonDataSO.startIndex = new Vector2Int(indexX, indexY);
        GameObject room = SpawnBaseRoom(dungeonDataSO.startIndex);
        room.GetComponent<RoomManager>().dungeonController = dungeonController;
        SpawnRoom(room, maxRoomCount);
        roomCount--;
        StartCoroutine(NextGenerateCoroutine(dungeonDataSO.startIndex,room));
    }


    private void NextGenerate(Vector2Int index, GameObject room)
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
                newRoom.GetComponent<RoomManager>().dungeonController = dungeonController;
                SpawnRoom(newRoom, roomCount);
                SetDirectionDoorActive(newRoom, index - availableDirection[i]);
                StartCoroutine(NextGenerateCoroutine(availableDirection[i], newRoom));
            }
        }
    }
    private IEnumerator NextGenerateCoroutine(Vector2Int index, GameObject room)
    {
        yield return new WaitForSeconds(0.5f);
        NextGenerate(index,room);
    }

    private void SetDirectionDoorActive(GameObject room,Vector2Int dir )
    {
        if ((room == null) || (dir == Vector2Int.zero)) return;
        room.GetComponent<RoomManager>().AddActiveDoor(DoorDir(dir));
    }
    private int DoorDir(Vector2Int dir)
    {
        if(dir == Vector2Int.up) return 0;
        else if (dir == Vector2Int.down) return 1;
        else if (dir == Vector2Int.left) return 2;
        else if (dir == Vector2Int.right) return 3;
        else return -1;
    }

    private bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < roomLength 
            && pos.y >= 0 && pos.y < roomLength;
    }
    private Vector2Int[] CheckDirection(Vector2Int index)
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
    private GameObject SpawnBaseRoom(Vector2Int pos)
    {
        if (!InBounds(pos)) return null;
        if (dungeons[pos.x, pos.y] != CellType.Empty) return null;

        dungeons[pos.x, pos.y] = CellType.Room;
        roomList.Add(pos);

        return Instantiate(roomPrefab, new Vector3(pos.x * roomSize, 0, pos.y * roomSize), Quaternion.identity, transform);
    }

    private void SpawnRoom(GameObject location, int value)
    {
        GameObject room =null;
        if (location == null) return;
        if (value == maxRoomCount)
        {
            if (roomObjArray[0] == null) return;
            room = roomObjArray[0];
        }
        else if ((value >= puzzleRoomCount + minigameRoomCount) && (puzzleRoomCount +minigameRoomCount >0))
        {
            int randomValue = Random.Range(1, 100);
            if (value > puzzleRoomCount + minigameRoomCount)
            {
                if (randomValue % 2 == 0)
                {
                    room = ChooseFunctionRoom();
                }
                else
                {
                    room = enemyRoomObjArray[Random.Range(0, enemyRoomObjArray.Length)];
                }
            }
            else
            {
                room = ChooseFunctionRoom();
            }
        }
        else if (value >0)
        {
            room = enemyRoomObjArray[Random.Range(0, enemyRoomObjArray.Length)];
        }
        else if (value == 0)
        {
            if (roomObjArray[1] == null)
                return;
            room = roomObjArray[1];
        }

        if (room == null) return;
        GameObject roomParent = location.GetComponent<RoomManager>().roomSpawnPoint;
        Instantiate(room, roomParent.transform.position, Quaternion.identity, roomParent.transform);
    }

    private GameObject ChooseFunctionRoom()
    {
        GameObject room = null;
        if (puzzleRoomCount == 0)
        {
            // Chọn ngẫu nhiên 1 phòng trong roomList để đặt minigame room
            minigameRoomCount--;
        }
        else if (minigameRoomCount == 0)
        {
            room = puzzleRoomObjArray[puzzleRoomCount];
            puzzleRoomCount--;
        }
        else
        {
            int randomValue = Random.Range(1, 100);
            if (randomValue % 2 == 0)
            {
                room = puzzleRoomObjArray[puzzleRoomCount];
                puzzleRoomCount--;
            }
            else
            {
                // Chọn ngẫu nhiên 1 phòng trong roomList để đặt minigame room
                minigameRoomCount--;
            }
        }

        return room;
    }

    private GameObject[] ShuffleArray(GameObject[] array)
    {
        for (int i = array.Length - 1; i > 0; i--) //shuffle
        {
            int j = Random.Range(0, i + 1);
            (array[i], array[j]) = (array[j], array[i]);
        }
        return array;
    }
}
