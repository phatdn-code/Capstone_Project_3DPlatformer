using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Final Sequence cho SoldierRobot:
    /// Bắn bomb → Player reflect → Bomb trúng boss → SlowMo → Boss chết.
    /// </summary>
    public class SoldierRobotFinalSequence : BossFinalSequenceBase
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR: BOMB & RECHARGE ===

        [Header("Bomb Settings")]
        [SerializeField] private BossBomb giantBombPrefab;
        [SerializeField] private Transform bombSpawnPoint;
        [SerializeField] private Transform bombCenterPoint;

        [Header("Effects")]
        [SerializeField] private GameObject flashFinalBombEffect;
        [SerializeField] private GameObject explosionFinalBombEffect;
        [SerializeField] private GameObject zoneEffect;

        [Header("Recharge Settings")]
        [SerializeField] private Transform rechargePoint;

        [Header("Support Gunner")]
        [SerializeField] private SupportGunnerAI supportGunner;

        #endregion

        //─────────────────────────────────────────────
        #region === INSPECTOR: FX ===

        [Header("Slow Motion")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 1.5f;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME ===

        private BossCore boss;
        private SoldierRobot soldierBoss;
        private SoldierRobotRechargeSequence rechargeTransition;
        private BossBomb currentBomb;
        private bool bombReflected;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY ===

        private void Start()
        {
            boss = GetComponent<BossCore>();
            soldierBoss = boss as SoldierRobot;
            rechargeTransition = GetComponent<SoldierRobotRechargeSequence>();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === ENTRY POINT ===

        public override IEnumerator ExecuteFinalSequence()
        {
            boss.IsInCutscene = true;
            bombReflected = false;
            currentBomb = null;

            soldierBoss.StopAttackSequence();

            yield return HandleCameraAndPlayerControl();
            yield return WaitForBombReflect();

            boss.IsInCutscene = false;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CAMERA / PLAYER CONTROL ===

        private IEnumerator HandleCameraAndPlayerControl()
        {
            PlayerHub.Instance.LockPlayer(true);

            yield return CameraCutsceneController.instance.FocusTo(BossCamType.Boss);
            yield return ShootGiantBomb();
            yield return CameraCutsceneController.instance.FocusTo(BossCamType.Boss);

            yield return MoveToRechargeStation();

            yield return CameraCutsceneController.instance.ReleaseToPlayer();
            PlayerHub.Instance.LockPlayer(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SHOOT BOMB ===

        private IEnumerator ShootGiantBomb()
        {
            yield return EnsureSafeDistanceFromCenter();

            boss.BossAnim.PlayFinalSkill();
            yield return new WaitForSeconds(0.5f);

            yield return SpawnAndFocusBomb();
        }

        private IEnumerator SpawnAndFocusBomb()
        {
            flashFinalBombEffect.SetActive(true);

            currentBomb = null;

            if (giantBombPrefab && bombSpawnPoint && bombCenterPoint)
            {
                // Spawn bomb
                currentBomb = Instantiate(giantBombPrefab, bombSpawnPoint.position, Quaternion.identity);

                // Setup final sequence behavior
                currentBomb.SetupForFinalSequence(PlayerHub.Instance.Player, soldierBoss);

                // Register events ONCE
                currentBomb.OnBombReflected += OnBombReflected;
                currentBomb.OnFinalBombHitBoss += OnFinalBombHitBoss;

                // Launch to center
                currentBomb.LaunchToPosition(bombCenterPoint.position);
            }

            if (currentBomb != null)
            {
                yield return null;

                CameraCutsceneController.instance.AssignSpecialTarget(currentBomb.transform);
                yield return CameraCutsceneController.instance.FocusTo(BossCamType.Special);
            }

            yield return new WaitForSeconds(1.5f);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SAFE DISTANCE BEFORE SHOOT ===

        private IEnumerator EnsureSafeDistanceFromCenter()
        {
            soldierBoss.SetPaused(true);

            const float safeDistance = 16f;
            float distance = Vector3.Distance(transform.position, bombCenterPoint.position);

            // Nếu đủ xa → chỉ xoay
            if (distance >= safeDistance)
            {
                yield return soldierBoss.RotateTowardsPoint(bombCenterPoint.position);
                yield break;
            }

            // Nếu gần quá → lùi lại
            Vector3 dir = (transform.position - bombCenterPoint.position).normalized;
            Vector3 targetPos = bombCenterPoint.position + dir * safeDistance;
            targetPos.y = transform.position.y;

            GameObject temp = new("Temp_MoveTarget");
            temp.transform.position = targetPos;

            yield return soldierBoss.MoveToTarget(temp.transform);

            while (Vector3.Distance(soldierBoss.transform.position, targetPos) > 0.3f)
                yield return null;

            yield return soldierBoss.RotateTowardsPoint(bombCenterPoint.position);

            Destroy(temp);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MOVE TO RECHARGE ===

        private IEnumerator MoveToRechargeStation()
        {
            if (!rechargePoint)
            {
                Debug.LogError("[FinalSequence] rechargePoint missing!");
                yield break;
            }

            Coroutine cSupport =
                (supportGunner != null) ? StartCoroutine(supportGunner.ReturnToIdlePoint()) : null;

            Coroutine cBoss =
                (rechargeTransition != null) ? StartCoroutine(rechargeTransition.PlayRechargeCutsceneOnly()) : null;

            if (cSupport != null) yield return cSupport;
            if (cBoss != null) yield return cBoss;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === WAIT FOR REFLECT → SLOWMO ===

        private IEnumerator WaitForBombReflect()
        {
            // Nếu không có bomb → stop luôn
            if (currentBomb == null)
                yield break;

            bombReflected = false;

            while (!bombReflected)
                yield return null;

            yield return PlaySlowMotionWin();
        }

        private void OnBombReflected()
        {
            bombReflected = true;

            StartCoroutine(CameraCutsceneController.instance.FocusTo(BossCamType.Special));
        }

        private IEnumerator PlaySlowMotionWin()
        {
            float original = Time.timeScale;
            Time.timeScale = slowMoScale;

            yield return new WaitForSecondsRealtime(slowMoDuration);

            Time.timeScale = original;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === FINAL HIT → BOSS DEATH ===

        private void OnFinalBombHitBoss(SoldierRobot robot)
        {
            // Tắt recharge
            if (rechargeTransition != null)
            {
                rechargeTransition.EndPumpAndRechargeEffect();
                robot.PlayRechargeAnimation(false);
            }

            explosionFinalBombEffect.SetActive(true);

            robot.SetPaused(true);
            robot.BossAnim.PlayDeath();

            supportGunner?.PlayDeath();

            boss.IsInCutscene = false;

            StartCoroutine(ReturnCameraAfterDelay());
        }

        private IEnumerator ReturnCameraAfterDelay()
        {
            yield return new WaitForSeconds(2f);

            yield return CameraCutsceneController.instance.ReleaseToPlayer();

            MovementBoundaryZone.Instance.enabled = false;

            zoneEffect.SetActive(false);
        }

        #endregion
    }
}
