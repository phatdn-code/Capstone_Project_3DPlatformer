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
        NextGenerate(dungeons[indexX, indexY], dungeonDataSO.startIndex);
    }
    void NextGenerate(CellType lastCellType, Vector2Int index)
    {
        switch (lastCellType)
        {
            case CellType.Room:
                RoadGenerate(lastCellType, index);
                break;
            case CellType.Road:
                RoomGenerate(lastCellType, index);
                break;
            case CellType.Empty: return;
        }
    }

    bool UpAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x, index.y + 1] == CellType.Empty) return true;
        return false;
    }
    bool DownAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x, index.y - 1] == CellType.Empty) return true;
        return false;
    }
    bool RightAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x + 1, index.y] == CellType.Empty) return true;
        return false;
    }
    bool LeftAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x - 1, index.y] == CellType.Empty) return true;
        return false;
    }
    bool UpLeftAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x - 1, index.y + 1] == CellType.Empty) return true;
        return false;
    }
    bool UpRightAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x + 1, index.y + 1] == CellType.Empty) return true;
        return false;
    }
    bool DownLeftAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x - 1, index.y - 1] == CellType.Empty) return true;
        return false;
    }
    bool DownRightAvailableCheck(Vector2Int index)
    {
        if (dungeons[index.x + 1, index.y - 1] == CellType.Empty) return true;
        return false;
    }

    void RoomGenerate(CellType lastCellType, Vector2Int index)
    {
        if (lastCellType != CellType.Road) return;
        Vector2Int availableDirection = CheckRoomDirection(index);
        if (availableDirection == new Vector2Int(-1,-1)) return;
        SpawnRoom(availableDirection);
        NextGenerate(dungeons[availableDirection.x, availableDirection.y], availableDirection); 
    }
    void RoadGenerate(CellType lastCellType, Vector2Int lastIndex)
    {
        if (lastCellType != CellType.Room) return;
        Vector2Int[] availableDirection = CheckRoadDirection(lastIndex);
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
                SpawnRoad(availableDirection[i],lastIndex);
                NextGenerate(dungeons[availableDirection[i].x, availableDirection[i].y], availableDirection[i]);
            }
        }
    }
    Vector2Int[] CheckRoadDirection(Vector2Int index)
    {
        List<Vector2Int> directions = new List<Vector2Int>();
        if (index.x == 0)
        {
            if (index.y == 0) //=> (0,0) - 2 Directions: Up, Right
            {
                if (UpAvailableCheck(index) && UpRightAvailableCheck(index))
                {
                    if (index.y + 2 < roomLength - 1)
                    {
                        if(UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpRightAvailableCheck(new Vector2Int(index.x, index.y+1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y + 1));
                        }
                    }
                }
                if (RightAvailableCheck(index) && UpRightAvailableCheck(index))
                {
                    if (index.x + 2 < roomLength - 1)
                    {
                        if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && UpRightAvailableCheck(new Vector2Int(index.x + 1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x + 1, index.y));
                        }
                    }
                }
            }
            else if (index.y == roomLength - 1) //=> (0,roomLength) - 2 Directions: Left, Down
            {
                if (RightAvailableCheck(index) && DownRightAvailableCheck(index))
                {
                    if (index.x + 2 < roomLength - 1)
                    {
                        if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && DownRightAvailableCheck(new Vector2Int(index.x + 1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x + 1, index.y));
                        }
                    }
                }
                if (DownAvailableCheck(index) && DownRightAvailableCheck(index))
                {
                    if(index.y - 2 >= 0)
                    {
                        if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownRightAvailableCheck(new Vector2Int(index.x, index.y - 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y - 1));
                        }
                    }
                }
            }
            else//=> (0, y) - 3 Directions: Up, Right, Down
            {
                if (UpAvailableCheck(index) && UpRightAvailableCheck(index))
                {
                    if (index.y + 2 < roomLength - 1)
                    {
                        if (UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpRightAvailableCheck(new Vector2Int(index.x, index.y + 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y + 1));
                        }
                    }
                }
                if (RightAvailableCheck(index) && UpRightAvailableCheck(index) && DownRightAvailableCheck(index))
                {
                    if (index.x + 2 < roomLength - 1)
                    {
                        if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && UpRightAvailableCheck(new Vector2Int(index.x + 1, index.y))
                            && DownAvailableCheck(new Vector2Int(index.x+1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x + 1, index.y));
                        }
                    }
                }
                if (DownAvailableCheck(index) && DownRightAvailableCheck(index))
                {
                    if (index.y - 2 >= 0)
                    {
                        if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownRightAvailableCheck(new Vector2Int(index.x, index.y - 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y - 1));
                        }
                    }
                }
            }
        }
        else if (index.x == roomLength - 1)
        {
            if (index.y == 0)//=> (roomLength,0) - 2 Directions: Left, Up
            {
                if (LeftAvailableCheck(index) && UpLeftAvailableCheck(index))
                {
                    if(index.x - 2 >= 0)
                    {
                        if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) && UpLeftAvailableCheck(new Vector2Int(index.x - 1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x - 1, index.y));
                        }
                    }
                }
                if (UpAvailableCheck(index) && UpLeftAvailableCheck(index))
                {
                    if (index.y + 2 < roomLength - 1)
                    {
                        if (UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpLeftAvailableCheck(new Vector2Int(index.x, index.y + 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y + 1));
                        }
                    }
                }
            }
            else if (index.y == roomLength - 1)//=> (roomLength,roomLength) - 2 Directions: Left, Down
            {
                if (LeftAvailableCheck(index) && DownLeftAvailableCheck(index))
                {
                    if (index.x - 2 >= 0)
                    {
                        if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) && DownLeftAvailableCheck(new Vector2Int(index.x - 1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x - 1, index.y));
                        }
                    }
                }
                if (DownAvailableCheck(index) && DownLeftAvailableCheck(index))
                {
                    if (index.y - 2 >= 0)
                    {
                        if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownLeftAvailableCheck(new Vector2Int(index.x, index.y - 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y - 1));
                        }
                    }
                }
            }
            else//=> (roomLength, y) - 3 Directions: Up, Left, Down
            {
                if (UpAvailableCheck(index) && UpLeftAvailableCheck(index))
                {
                    if (index.y + 2 < roomLength - 1)
                    {
                        if (UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpLeftAvailableCheck(new Vector2Int(index.x, index.y + 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y + 1));
                        }
                    }
                }
                if (LeftAvailableCheck(index) && UpLeftAvailableCheck(index) && DownLeftAvailableCheck(index))
                {
                    if (index.x - 2 >= 0)
                    {
                        if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) && UpLeftAvailableCheck(new Vector2Int(index.x - 1, index.y))
                            && DownLeftAvailableCheck(new Vector2Int(index.x-1, index.y)))
                        {
                            directions.Add(new Vector2Int(index.x - 1, index.y));
                        }
                    }
                }
                if (DownAvailableCheck(index) && DownLeftAvailableCheck(index))
                {
                    if (index.y - 2 >= 0)
                    {
                        if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownLeftAvailableCheck(new Vector2Int(index.x, index.y - 1)))
                        {
                            directions.Add(new Vector2Int(index.x, index.y - 1));
                        }
                    }
                }
            }
        }
        else if (index.y == 0) //=> (x,0) - 3 Directions: Left, Up, Right
        {
            if (LeftAvailableCheck(index) && UpLeftAvailableCheck(index))
            {
                if (index.x - 2 >= 0)
                {
                    if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) && UpLeftAvailableCheck(new Vector2Int(index.x - 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x - 1, index.y));
                    }
                }
            }
            if (UpAvailableCheck(index) && UpLeftAvailableCheck(index) && UpRightAvailableCheck(index))
            {
                if (index.y + 2 < roomLength - 1)
                {
                    if (UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpLeftAvailableCheck(new Vector2Int(index.x, index.y + 1)) 
                        && UpRightAvailableCheck(new Vector2Int(index.x, index.y+1)))
                    {
                        directions.Add(new Vector2Int(index.x, index.y + 1));
                    }
                }
            }
            if (RightAvailableCheck(index) && UpRightAvailableCheck(index))
            {
                if (index.x + 2 < roomLength - 1)
                {
                    if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && UpRightAvailableCheck(new Vector2Int(index.x + 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x + 1, index.y));
                    }
                }
            }
        }
        else if (index.y == roomLength - 1) //=> (x,roomLength) - 3 Directions: Left, Down, Right
        {
            if (LeftAvailableCheck(index) && DownLeftAvailableCheck(index))
            {
                if (index.x - 2 >= 0)
                {
                    if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) &&  DownLeftAvailableCheck(new Vector2Int(index.x - 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x - 1, index.y));
                    }
                }
            }
            if (DownAvailableCheck(index) && DownLeftAvailableCheck(index) && DownRightAvailableCheck(index))
            {
                if (index.y - 2 >= 0)
                {
                    if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownLeftAvailableCheck(new Vector2Int(index.x, index.y - 1))
                        && DownRightAvailableCheck(new Vector2Int(index.x, index.y-1)))
                    {
                        directions.Add(new Vector2Int(index.x, index.y - 1));
                    }
                }
            }
            if (RightAvailableCheck(index) && DownRightAvailableCheck(index))
            {
                if (index.x + 2 < roomLength - 1)
                {
                    if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && DownAvailableCheck(new Vector2Int(index.x + 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x + 1, index.y));
                    }
                }
            }
        }
        else//=> (x,y) - 4 Directions: Left, Up, Right, Down
        {
            if (LeftAvailableCheck(index) && UpLeftAvailableCheck(index) && DownLeftAvailableCheck(index))
            {
                if (index.x - 2 >= 0)
                {
                    if (LeftAvailableCheck(new Vector2Int(index.x - 1, index.y)) && UpLeftAvailableCheck(new Vector2Int(index.x - 1, index.y))
                        && DownLeftAvailableCheck(new Vector2Int(index.x - 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x - 1, index.y));
                    }
                }
            }
            if (UpAvailableCheck(index) && UpLeftAvailableCheck(index) && UpRightAvailableCheck(index))
            {
                if (index.y + 2 < roomLength - 1)
                {
                    if (UpAvailableCheck(new Vector2Int(index.x, index.y + 1)) && UpLeftAvailableCheck(new Vector2Int(index.x, index.y + 1))
                        && UpRightAvailableCheck(new Vector2Int(index.x, index.y + 1)))
                    {
                        directions.Add(new Vector2Int(index.x, index.y + 1));
                    }
                }
            }
            if (RightAvailableCheck(index) && UpRightAvailableCheck(index) && DownRightAvailableCheck(index))
            {
                if (index.x + 2 < roomLength - 1)
                {
                    if (RightAvailableCheck(new Vector2Int(index.x + 1, index.y)) && UpRightAvailableCheck(new Vector2Int(index.x + 1, index.y))
                        && DownAvailableCheck(new Vector2Int(index.x + 1, index.y)))
                    {
                        directions.Add(new Vector2Int(index.x + 1, index.y));
                    }
                }
            }
            if (DownAvailableCheck(index) && DownLeftAvailableCheck(index) && DownRightAvailableCheck(index))
            {
                if (index.y - 2 >= 0)
                {
                    if (DownAvailableCheck(new Vector2Int(index.x, index.y - 1)) && DownLeftAvailableCheck(new Vector2Int(index.x, index.y - 1))
                        && DownRightAvailableCheck(new Vector2Int(index.x, index.y - 1)))
                    {
                        directions.Add(new Vector2Int(index.x, index.y - 1));
                    }
                }
            }
        }
        Vector2Int[] availableDirection = directions.ToArray();
        return availableDirection;
    }

    Vector2Int CheckRoomDirection(Vector2Int index)
    {
        Vector2Int availableDirection;
        if (index.x == 0 || index.x == roomLength - 1)
        {
            if (!UpAvailableCheck(index)) return new Vector2Int(index.x, index.y - 1);
            else return new Vector2Int(index.x, index.y + 1);
        }
        else if (index.y ==0 || index.y == roomLength - 1)
        {
            if(!LeftAvailableCheck(index)) return new Vector2Int(index.x + 1, index.y);
            else return new Vector2Int(index.x - 1, index.y);
        }
        else
        {
            if (!UpAvailableCheck(index)) return new Vector2Int(index.x, index.y - 1);
            else if(!DownAvailableCheck(index)) return new Vector2Int(index.x, index.y + 1);
            else if (!LeftAvailableCheck(index)) return new Vector2Int(index.x + 1, index.y);
            else if(!RightAvailableCheck(index)) return new Vector2Int(index.x - 1, index.y);
        }
        return new Vector2Int(-1, -1);
    }

    void SpawnRoom(Vector2Int pos)
    {
        if (dungeons[pos.x, pos.y] != CellType.Empty) return;

        dungeons[pos.x, pos.y] = CellType.Room;
        roomList.Add(pos);

        Instantiate(roomPrefab, new Vector3(pos.x * roomSize, 0, pos.y * roomSize), Quaternion.identity, transform);
    }

    void SpawnRoad(Vector2Int pos, Vector2Int lastPos)
    {
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
