using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PLAYERTWO.PlatformerProject
{
    public class MapReturnSpawnManager : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Options")]
        [SerializeField] private bool resolveOnStart = true;
        [SerializeField] private bool setPlayerRespawnAfterMove = true;
        [SerializeField, Min(0)] private int waitFramesBeforeResolve = 2;
        [SerializeField, Min(0)] private int maxWaitFramesForPlayer = 20;

        [Header("Debug")]
        [SerializeField] private bool showDebugLog = false;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY ===

        /// <summary>
        /// VN: Vào scene map thì thử áp dụng điểm spawn đã lưu.
        /// </summary>
        private void Start()
        {
            if (!resolveOnStart)
                return;

            StartCoroutine(ResolveSpawnRoutine());
        }

        #endregion

        //─────────────────────────────────────────────
        #region === RESOLVE ===

        /// <summary>
        /// VN: Chờ vài frame cho scene và player khởi tạo xong rồi mới xử lý spawn.
        /// </summary>
        private IEnumerator ResolveSpawnRoutine()
        {
            for (int i = 0; i < waitFramesBeforeResolve; i++)
                yield return null;

            int waitedFrames = 0;

            while (!HasPlayer() && waitedFrames < maxWaitFramesForPlayer)
            {
                waitedFrames++;
                yield return null;
            }

            TryApplyReturnSpawn();
        }

        /// <summary>
        /// VN: Đọc dữ liệu điểm quay lại và đặt player vào đúng portal.
        /// </summary>
        private void TryApplyReturnSpawn()
        {
            if (Game.instance == null)
                return;

            string currentScene = SceneManager.GetActiveScene().name;

            if (!Game.instance.TryConsumePendingReturnPoint(currentScene, out string pointId))
            {
                Log($"Không có pending return point trong scene: {currentScene}");
                return;
            }

            IPortalReturnPoint portal = FindPortalById(pointId);

            if (portal == null)
            {
                Debug.LogWarning($"[MapSpawnResolver] Không tìm thấy portal có ReturnPointId = {pointId}", this);
                return;
            }

            if (portal.ReturnPoint == null)
            {
                Debug.LogWarning($"[MapSpawnResolver] Portal {pointId} không có ReturnPoint hợp lệ.", this);
                return;
            }

            Player player = GetPlayer();

            if (player == null)
            {
                Debug.LogWarning("[MapSpawnResolver] Không tìm thấy Player để áp dụng return spawn.", this);
                return;
            }

            ApplyPlayerSpawn(player, portal.ReturnPoint);
            Log($"Đã spawn player tại return point: {pointId}");
        }

        /// <summary>
        /// VN: Đặt vị trí player vào point đã chọn.
        /// </summary>
        private void ApplyPlayerSpawn(Player player, Transform point)
        {
            if (player == null || point == null)
                return;

            player.transform.SetPositionAndRotation(point.position, point.rotation);

            if (setPlayerRespawnAfterMove)
                player.SetRespawn(point.position, point.rotation);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === FIND PORTAL ===

        /// <summary>
        /// VN: Tìm portal theo ReturnPointId trong scene hiện tại.
        /// </summary>
        private IPortalReturnPoint FindPortalById(string pointId)
        {
            if (string.IsNullOrEmpty(pointId))
                return null;

            var portals = GetAllReturnPortalsInScene();

            foreach (var portal in portals)
            {
                if (portal == null)
                    continue;

                if (!portal.UseAsReturnPoint)
                    continue;

                if (portal.ReturnPointId != pointId)
                    continue;

                return portal;
            }

            return null;
        }

        /// <summary>
        /// VN: Lấy toàn bộ object có implement IPortalReturnPoint, kể cả object đang inactive.
        /// </summary>
        private List<IPortalReturnPoint> GetAllReturnPortalsInScene()
        {
            List<IPortalReturnPoint> result = new List<IPortalReturnPoint>();
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] roots = activeScene.GetRootGameObjects();

            foreach (var root in roots)
            {
                if (root == null)
                    continue;

                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);

                foreach (var behaviour in behaviours)
                {
                    if (behaviour is IPortalReturnPoint portal)
                        result.Add(portal);
                }
            }

            return result;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === PLAYER ===

        /// <summary>
        /// VN: Kiểm tra scene hiện tại đã có player hay chưa.
        /// </summary>
        private bool HasPlayer()
        {
            return GetPlayer() != null;
        }

        /// <summary>
        /// VN: Lấy player hiện tại từ Level.
        /// </summary>
        private Player GetPlayer()
        {
            if (Level.instance == null)
                return null;

            return Level.instance.player;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === DEBUG ===

        /// <summary>
        /// VN: In log khi cần debug.
        /// </summary>
        private void Log(string message)
        {
            if (!showDebugLog)
                return;

            Debug.Log($"[MapReturnSpawnManager] {message}", this);
        }

        #endregion
    }
}