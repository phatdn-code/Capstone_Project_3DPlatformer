using System.Collections;
using UnityEngine;
using AmazingAssets.AdvancedDissolve;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Final Sequence SoldierRobot:
    /// Bắn bomb → Player reflect → Bomb trúng boss → SlowMo → Dissolve → Boss chết.
    /// </summary>
    public class SoldierRobotFinalSequence : BossFinalSequenceBase
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR: BOMB & EFFECTS ===

        [Header("Bomb Settings")]
        [SerializeField] private BossBomb giantBombPrefab;
        [SerializeField] private Transform bombSpawnPoint;
        [SerializeField] private Transform bombCenterPoint;

        [Header("VFX")]
        [SerializeField] private GameObject flashFinalBombEffect;
        [SerializeField] private GameObject explosionFinalBombEffect;
        [SerializeField] private GameObject portalEnd;
        [SerializeField] private GameObject zoneEffect;

        #endregion

        //─────────────────────────────────────────────
        #region === INSPECTOR: BOSS SUPPORT ===

        [Header("Recharge Settings")]
        [SerializeField] private Transform rechargePoint;

        [Header("Support Gunner")]
        [SerializeField] private SupportGunnerAI supportGunner;

        #endregion

        //─────────────────────────────────────────────
        #region === INSPECTOR: FINAL FX ===

        [Header("Slow Motion")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 1.5f;

        [Header("Dissolve Settings")]
        [SerializeField] private AdvancedDissolvePropertiesController dissolveCtrl;
        [SerializeField] private float dissolveSpeed = 1f;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME REFERENCES ===

        private BossCore boss;
        private SoldierRobot soldierBoss;
        private SoldierRobotRechargeSequence rechargeTransition;

        private BossBomb currentBomb;
        private bool bombReflected;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY EVENTS ===

        private void Start()
        {
            boss = GetComponent<BossCore>();
            soldierBoss = boss as SoldierRobot;
            rechargeTransition = GetComponent<SoldierRobotRechargeSequence>();

            portalEnd.SetActive(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MAIN ENTRY ===

        /// <summary>Điểm bắt đầu toàn bộ Final Sequence.</summary>
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
        #region === CAMERA + PLAYER HANDLING ===

        /// <summary>Quản lý camera và lock/unlock player trong suốt sequence.</summary>
        private IEnumerator HandleCameraAndPlayerControl()
        {
            PlayerHub.Instance.LockPlayer(true);

            yield return CameraCutsceneController.Instance.FocusTo(BossCamType.Boss);
            yield return ShootGiantBomb();
            yield return CameraCutsceneController.Instance.FocusTo(BossCamType.Boss);

            yield return MoveToRechargeStation();

            yield return CameraCutsceneController.Instance.ReleaseToPlayer();
            PlayerHub.Instance.LockPlayer(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SHOOT GIANT BOMB ===

        /// <summary>Boss chuẩn bị + bắn bomb lớn vào giữa sân.</summary>
        private IEnumerator ShootGiantBomb()
        {
            yield return EnsureSafeDistanceFromCenter();

            boss.BossAnim.PlayFinalSkill();
            yield return new WaitForSeconds(0.5f);

            yield return SpawnAndFocusBomb();
        }

        /// <summary>Spawn bomb, đăng ký event, chuyển camera vào bomb.</summary>
        private IEnumerator SpawnAndFocusBomb()
        {
            flashFinalBombEffect.SetActive(true);
            currentBomb = null;

            // Spawn + setup bomb
            if (giantBombPrefab && bombSpawnPoint && bombCenterPoint)
            {
                currentBomb = Instantiate(giantBombPrefab, bombSpawnPoint.position, Quaternion.identity);

                currentBomb.SetupForFinalSequence(PlayerHub.Instance.Player, soldierBoss);
                currentBomb.OnBombReflected += OnBombReflected;
                currentBomb.OnFinalBombHitBoss += OnFinalBombHitBoss;

                currentBomb.LaunchToPosition(bombCenterPoint.position);
            }

            // Camera focus
            if (currentBomb != null)
            {
                yield return null;
                CameraCutsceneController.Instance.AssignSpecialTarget(currentBomb.transform);
                yield return CameraCutsceneController.Instance.FocusTo(BossCamType.Special);
            }

            yield return new WaitForSeconds(1.5f);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === ENSURE SAFE DISTANCE BEFORE SHOOT ===

        /// <summary>Đảm bảo boss đứng đủ xa tâm bomb để không bị vướng.</summary>
        private IEnumerator EnsureSafeDistanceFromCenter()
        {
            soldierBoss.SetPaused(true);

            const float safeDistance = 16f;
            float dist = Vector3.Distance(transform.position, bombCenterPoint.position);

            // Nếu đã đủ xa → chỉ xoay hướng
            if (dist >= safeDistance)
            {
                yield return soldierBoss.RotateTowardsPoint(bombCenterPoint.position);
                yield break;
            }

            // Nếu gần quá → lùi ra xa
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
        #region === MOVE TO RECHARGE STATION ===

        /// <summary>Boss lùi về trạm recharge + chạy cutscene recharge.</summary>
        private IEnumerator MoveToRechargeStation()
        {
            if (!rechargePoint)
            {
                Debug.LogError("[FinalSequence] rechargePoint missing!");
                yield break;
            }

            Coroutine cSupport = supportGunner != null
                ? StartCoroutine(supportGunner.ReturnToIdlePoint())
                : null;

            Coroutine cBoss = rechargeTransition != null
                ? StartCoroutine(rechargeTransition.PlayRechargeCutsceneOnly())
                : null;

            if (cSupport != null) yield return cSupport;
            if (cBoss != null) yield return cBoss;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === WAIT FOR REFLECT → SLOWMO ===

        /// <summary>Chờ player đánh phản bomb.</summary>
        private IEnumerator WaitForBombReflect()
        {
            if (currentBomb == null) yield break;

            bombReflected = false;

            while (!bombReflected)
                yield return null;

            yield return PlaySlowMotionWin();
        }

        /// <summary>Bomb bị đánh phản.</summary>
        private void OnBombReflected()
        {
            bombReflected = true;
            StartCoroutine(CameraCutsceneController.Instance.FocusTo(BossCamType.Special));
        }

        /// <summary>Slow motion chiến thắng.</summary>
        private IEnumerator PlaySlowMotionWin()
        {
            float original = Time.timeScale;
            Time.timeScale = slowMoScale;

            yield return new WaitForSecondsRealtime(slowMoDuration);

            Time.timeScale = original;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === FINAL HIT → DEATH + DISSOLVE ===

        /// <summary>Bomb trúng boss → boss chết → chuyển camera lại.</summary>
        private void OnFinalBombHitBoss(SoldierRobot robot)
        {
            // Ngắt recharge
            rechargeTransition?.EndPumpAndRechargeEffect();
            robot.PlayRechargeAnimation(false);

            explosionFinalBombEffect.SetActive(true);

            robot.SetPaused(true);
            robot.BossAnim.PlayDeath();
            supportGunner?.PlayDeath();

            boss.IsInCutscene = false;

            StartCoroutine(DissolveZoneAndReturnCamera());
        }

        /// <summary>Chạy dissolve toàn zone.</summary>
        private IEnumerator StartDissolve()
        {
            if (dissolveCtrl == null)
                yield break;

            float t = dissolveCtrl.cutoutStandard.clip;

            while (t < 1f)
            {
                t += Time.deltaTime * dissolveSpeed;
                dissolveCtrl.cutoutStandard.clip = Mathf.Clamp01(t);
                dissolveCtrl.ForceUpdateShaderData();
                yield return null;
            }
        }

        /// <summary>Chờ dissolve → trả camera → tắt boundary.</summary>
        private IEnumerator DissolveZoneAndReturnCamera()
        {
            yield return StartDissolve();
            yield return new WaitForSeconds(.5f);

            yield return CameraCutsceneController.Instance.ReleaseToPlayer();

            MovementBoundaryZone.Instance.enabled = false;
            zoneEffect.SetActive(false);
            portalEnd.SetActive(true);
        }

        #endregion
    }
}
