using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DungeonGenerator : MonoBehaviour
{
    public int roomLength = 10; // số phòng theo chiều dài
    public float roomSize = 59.05f; // đúng bằng size prefab
    public int maxRoomCount = 30;
    public GameObject roomPrefab;
    public enum CellType { Empty, Room}
    [SerializeField] private DungeonSO dungeonDataSO;

    private int minRoomCount = 4;
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
        SpawnRoom(dungeonDataSO.startIndex);
        roomCount--;
        NextGenerate(dungeons[indexX, indexY], dungeonDataSO.startIndex);
    }


    void NextGenerate(CellType lastCellType, Vector2Int index)
    {
        if (roomCount <= 0) return;
        Vector2Int[] availableDirection = CheckDirection(index);
        if (availableDirection.Length == 0) return;
        else
        {
            int count = Random.Range(1, availableDirection.Length + 1);
            if (count <= availableDirection.Length)
            {
                for (int i = availableDirection.Length - 1; i > 0; i--) //shuffle
                {
                    int j = Random.Range(0, i + 1);
                    (availableDirection[i], availableDirection[j]) = (availableDirection[j], availableDirection[i]);
                }
            }
            for (int i = 0; i < count; i++)
            {
                roomCount--;
                if (roomCount < 0) return;
                SpawnRoom(availableDirection[i]);
                NextGenerate(dungeons[availableDirection[i].x, availableDirection[i].y], availableDirection[i]);
            }
        }
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
    void SpawnRoom(Vector2Int pos)
    {
        if (!InBounds(pos)) return;
        if (dungeons[pos.x, pos.y] != CellType.Empty) return;

        dungeons[pos.x, pos.y] = CellType.Room;
        roomList.Add(pos);

        Instantiate(roomPrefab, new Vector3(pos.x * roomSize, 0, pos.y * roomSize), Quaternion.identity, transform);
    }
}
