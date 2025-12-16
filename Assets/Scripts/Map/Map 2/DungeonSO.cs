using UnityEngine;

[CreateAssetMenu(fileName = "DungeonSO", menuName = "MapSO/Map02/DungeonSO")]
public class DungeonSO : ScriptableObject
{
    public Vector2Int startIndex;
    public Vector2Int endIndex;

    public void ResetData()
    {
        startIndex = Vector2Int.zero;
        endIndex = Vector2Int.zero;
    }
}


