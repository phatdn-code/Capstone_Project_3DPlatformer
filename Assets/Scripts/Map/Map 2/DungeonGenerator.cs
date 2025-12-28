using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;

public class DungeonGenerator : MonoBehaviour
{
    public int roomLength = 10; // số phòng theo chiều dài
    public float roomSize = 30f; // đúng bằng size prefab
    public GameObject roomPrefab;
    public GameObject roadPrefab;
    public enum CellType { Empty, Room, Road }
    [SerializeField] private DungeonSO dungeonDataSO;

    private CellType[,] dungeons;
    private List<Vector2Int> roomList = new List<Vector2Int>();

    private static Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

    void Start()
    {
        StartGenerate();
    }

    void StartGenerate()
    {
        dungeonDataSO.ResetData();
        dungeons = new CellType[roomLength, roomLength];
        roomList.Clear();
        int indexX = Random.Range(0, roomLength);
        int indexY = Random.Range(0, roomLength);
        dungeonDataSO.startIndex = new Vector2Int(indexX, indexY);
        SpawnRoom(dungeonDataSO.startIndex);
        NextGenerate(dungeons[indexX, indexY], dungeonDataSO.startIndex, new Vector2Int(-1,-1));
    }
    void NextGenerate(CellType lastCellType, Vector2Int index, Vector2Int lastRoomIndex)
    {
        switch (lastCellType)
        {
            case CellType.Room:
                RoadGenerate(lastCellType, index);
                break;
            case CellType.Road:
                RoomGenerate(lastCellType, index,lastRoomIndex);
                break;
            case CellType.Empty: return;
        }
    }

    bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < roomLength 
            && pos.y >= 0 && pos.y < roomLength;
    }
    void RoomGenerate(CellType lastCellType, Vector2Int index, Vector2Int lastIndex)
    {
        if(!InBounds(lastIndex)) return;
        if (lastCellType != CellType.Road) return;
        Vector2Int availableDirection = 2* index - lastIndex;
        if(!InBounds(availableDirection)) return;
        SpawnRoom(availableDirection);
        NextGenerate(dungeons[availableDirection.x, availableDirection.y], availableDirection,index); 
    }
    void RoadGenerate(CellType lastCellType, Vector2Int index)
    {
        if (lastCellType != CellType.Room) return;
        Vector2Int[] availableDirection = CheckRoadDirection(index);
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
                SpawnRoad(availableDirection[i],index);
                NextGenerate(dungeons[availableDirection[i].x, availableDirection[i].y], availableDirection[i],index);
            }
        }
    }
    Vector2Int[] CheckRoadDirection(Vector2Int index)
    {
        List<Vector2Int> availDirs = new List<Vector2Int>();
        foreach (var dir in directions)
        {
            if (InBounds(index + 2 * dir))
            {
                if((dungeons[index.x + dir.x, index.y + dir.y] == CellType.Empty) &&
                   (dungeons[index.x + 2 * dir.x, index.y + 2 * dir.y] == CellType.Empty))
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

    void SpawnRoad(Vector2Int pos, Vector2Int lastPos)
    {
        if (!InBounds(pos)) return;
        if (dungeons[pos.x, pos.y] != CellType.Empty) return;
        Quaternion rotation = Quaternion.identity;
        dungeons[pos.x, pos.y] = CellType.Road;
        if(RoadCheck(pos,lastPos))
        {
            rotation = Quaternion.Euler(0, 90, 0);
        }
        Instantiate(roadPrefab, new Vector3(pos.x * roomSize, 0, pos.y * roomSize), rotation, transform);
    }

    bool RoadCheck(Vector2Int pos, Vector2 lastPos)
    {
        if ( (pos.x != lastPos.x) && (pos.y == lastPos.y))
        {
            return false;
        }
        return true;
    }
}
