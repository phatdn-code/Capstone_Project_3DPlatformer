using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Collider))]
    public class BossEncounterCutscene : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Refs")]
        [SerializeField] private BossCore bossCore;                 // Kéo BossCore (SoldierRobot) vào
        [SerializeField] private Transform bossCombatPoint;         // Điểm boss sẽ đứng combat
        [SerializeField] private string bossDisplayName = "Soldier Robot";
        [SerializeField] private bool triggerOnce = true;

        [Header("Timings")]
        [SerializeField] private float cameraIntroDelay = 0.4f;
        [SerializeField] private float cameraReturnDelay = 0.4f;

        [Header("Camera (Cinemachine optional)")]
        [SerializeField] private CinemachineCamera bossCam;         // tuỳ chọn
        [SerializeField] private Transform focusTarget;             // nếu không dùng CM, sẽ xoay/zoom thủ công
        [SerializeField] private float normalFOV = 60f;
        [SerializeField] private float zoomFOV = 28f;
        [SerializeField] private float zoomSpeed = 2f;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME VARIABLES ===

        private bool triggered;
        private Camera mainCam;
        private float camDefaultFOV;

        #endregion

        private MovementBoundaryZone boundary;

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            boundary = FindFirstObjectByType<MovementBoundaryZone>();

            mainCam = Camera.main;
            if (mainCam != null)
                camDefaultFOV = mainCam.fieldOfView;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggered && triggerOnce) return;
            if (!other.CompareTag("Player")) return;

            triggered = true;
            StartCoroutine(RunSequence());
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CUTSCENE SEQUENCE ===

        private IEnumerator RunSequence()
        {
            // 1) Khoá player
            PlayerHub.Instance.LockPlayer(true);

            yield return new WaitForSeconds(cameraIntroDelay);

            // 2) Kích hoạt camera cinematic
            if (bossCam != null)
                bossCam.Priority = 20;
            else if (mainCam != null && focusTarget != null)
                StartCoroutine(CameraFocusOnBoss());

            // 3) Boss tiến vào vị trí combat (dùng chính mover/anim của bạn)
            var soldier = bossCore as SoldierRobot;
            if (soldier != null && bossCombatPoint != null)
            {
                soldier.SetSpeedMultiplier(1.5f);

                var moveRoutine = soldier.MoveToCombatPoint(bossCombatPoint);
                if (moveRoutine != null)
                    yield return moveRoutine;

                soldier.SetSpeedMultiplier(1f);
            }

            // 4) Hiện UI Boss (tên + thanh máu) — intro
            var ui = bossCore.GetComponent<BossUI>();
            ui?.ShowBossIntro(bossDisplayName);

            // 5) Trả camera về player
            yield return new WaitForSeconds(cameraReturnDelay);
            if (bossCam != null)
                bossCam.Priority = 0;
            else if (mainCam != null)
                StartCoroutine(CameraResetFOV());

            // 6) Mở khoá player
            PlayerHub.Instance.LockPlayer(false);

            // 7) Thực hiện animation bắt đầu vào combat
            bossCore.bossAnim?.PlayBattleStart();
            yield return new WaitForSeconds(2);

            if (boundary != null) boundary.ActivateBoundary();

            bossCore.StartBattle();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CAMERA HELPERS (No-Cinemachine) ===

        private IEnumerator CameraFocusOnBoss()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * zoomSpeed;
                mainCam.fieldOfView = Mathf.Lerp(camDefaultFOV, zoomFOV, t);

                Vector3 dir = (focusTarget.position - mainCam.transform.position).normalized;
                Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                mainCam.transform.rotation = Quaternion.Slerp(
                    mainCam.transform.rotation,
                    lookRot,
                    Time.deltaTime * zoomSpeed
                );

                yield return null;
            }
        }

        private IEnumerator CameraResetFOV()
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * zoomSpeed;
                mainCam.fieldOfView = Mathf.Lerp(mainCam.fieldOfView, camDefaultFOV, t);
                yield return null;
            }
        }

        #endregion
    }
}
