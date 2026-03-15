using NUnit.Framework;
using PLAYERTWO.PlatformerProject;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRoomController : MonoBehaviour
{
    [SerializeField] private RoomManager roomManager;
    [SerializeField] private List<EntityController> enemyList = new List<EntityController>();

    private void Start()
    {
        roomManager = transform.parent.GetComponentInParent<RoomManager>();
        if (roomManager != null)
        {
            roomManager.isRoomCleared = false;
        }
        
    }
    private void Update()
    {
        if (enemyList.Count > 0)
        {
            RemoveEnemy();
        }
        else
        {
            GameClear();
        }
    }
    private void RemoveEnemy()
    {
        enemyList.RemoveAll(enemy => enemy == null || enemy.enabled == false);
    }
    private void GameClear()
    {
        if (roomManager != null)
        {
            roomManager.isRoomCleared = true;
            roomManager.OpenAllDoors();
            this.enabled = false;
        }
    }
}
