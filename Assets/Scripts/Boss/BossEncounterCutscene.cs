using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Collider))]
    public class BossEncounterCutscene : MonoBehaviour
    {
        [Header("Boss References")]
        [SerializeField] private BossCore bossCore;
        [SerializeField] private Transform bossCombatPoint;
        [SerializeField] private string bossDisplayName = "Soldier Robot";
        [SerializeField] private bool triggerOnce = true;

        [Header("Boss Movement")]
        [SerializeField] private float moveSpeedBoost = 5f;

        [Header("Dissolve Plane")]
        [SerializeField] private Transform dissolvePlane;
        [SerializeField] private float planeDropHeight = -2f;
        [SerializeField] private float planeTargetHeight = 22f;
        [SerializeField] private float planeDropSpeed = 5f;

        private bool triggered;
        private MovementBoundaryZone boundary;
        private Player player;

        //─────────────────────────────────────────────

        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            boundary = FindFirstObjectByType<MovementBoundaryZone>();
            player = PlayerHub.Instance.Player;

            // Đưa plane xuống vị trí ban đầu (ẩn)
            if (dissolvePlane != null)
            {
                Vector3 pos = dissolvePlane.position;
                pos.y = planeDropHeight;
                dissolvePlane.position = pos;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && triggerOnce) return;
            if (!other.CompareTag("Player")) return;

            triggered = true;

            SetPlayerRespawnPoint();
            StartCoroutine(RunSequence());
        }

        #endregion
        //─────────────────────────────────────────────

        #region === CUTSCENE SEQUENCE ===

        /// <summary>Chạy toàn bộ cutscene vào sàn đấu boss</summary>
        private IEnumerator RunSequence()
        {
            // Lock player di chuyển
            PlayerHub.Instance.LockPlayer(true);

            // Chuyển camera vào boss
            yield return CameraCutsceneController.Instance.FocusTo(BossCamType.Boss);

            // Thả plane dissolve từ dưới lên
            yield return StartCoroutine(DropDissolvePlane());

            // Boss chạy vào vị trí combat
            var soldier = bossCore as SoldierRobot;

            if (soldier != null && bossCombatPoint != null)
            {
                soldier.SetSpeedMultiplier(moveSpeedBoost);

                var moveRoutine = soldier.MoveToCombatPoint(bossCombatPoint);
                if (moveRoutine != null)
                    yield return moveRoutine;

                soldier.SetSpeedMultiplier(1f);
            }

            // Hiện UI intro của boss
            var ui = bossCore.GetComponent<BossUI>();
            ui?.ShowBossIntro(bossDisplayName);

            // Trả camera về player
            yield return CameraCutsceneController.Instance.ReleaseToPlayer();

            // Unlock player
            PlayerHub.Instance.LockPlayer(false);

            // Boss chơi animation bắt đầu combat
            bossCore.BossAnim?.PlayBattleStart();
            yield return new WaitForSeconds(2f);

            // Bật boundary và bắt đầu combat
            boundary?.ActivateBoundary();
            bossCore.StartBattle();
        }

        /// <summary>Kéo dissolve plane lên cao với tốc độ tùy chỉnh</summary>
        private IEnumerator DropDissolvePlane()
        {
            Vector3 start = dissolvePlane.position;
            Vector3 end = new Vector3(start.x, planeTargetHeight, start.z);

            while (Vector3.Distance(dissolvePlane.position, end) > 0.05f)
            {
                dissolvePlane.position = Vector3.MoveTowards(
                    dissolvePlane.position,
                    end,
                    planeDropSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Snap vào đúng vị trí cho sạch sẽ
            dissolvePlane.position = end;
        }


        #endregion
        //─────────────────────────────────────────────


        #region === PLAYER RESPAWN ===

        /// <summary>Lưu lại vị trí respawn của player khi chạm trigger</summary>
        private void SetPlayerRespawnPoint()
        {
            if (player != null)
                player.SetRespawn(player.transform.position, player.transform.rotation);
        }

        #endregion
        //─────────────────────────────────────────────
    }
}
