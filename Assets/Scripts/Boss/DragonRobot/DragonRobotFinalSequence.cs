using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(DragonRobot))]
    public class DragonRobotFinalSequence : BossFinalSequenceBase
    {
        #region Inspector

        // Các tham chiếu cần thiết từ Unity Editor
        [Header("Refs")]
        [SerializeField] private WaterCannon waterCannon;

        [Header("Disable Cannons (khi vào FinalSequence)")]
        [SerializeField] private CannonInput[] cannonsToDisable;

        [Header("Final Poses")]
        [SerializeField] private Transform finalBossPose;
        [SerializeField] private Transform finalPlayerPose;

        [Header("Final Camera (Priority)")]
        [SerializeField] private CinemachineCamera finalCam;
        [SerializeField] private int finalCamActivePriority = 100;
        [SerializeField] private int finalCamInactivePriority = 0;

        [Header("Cannon Burst")]
        [SerializeField, Min(1)] private int burstShots = 3;
        [SerializeField, Min(0f)] private float shotInterval = 1.5f;

        [Header("Hit Waiting (Anti-miss)")]
        [SerializeField, Min(0f)] private float hitWaitExtraTime = 3f;

        [Header("Cannon Scale (khi alpha về 0)")]
        [SerializeField] private float cannonScaleFrom = 0.075f;
        [SerializeField] private float cannonScaleTo = 0.15f;
        [SerializeField, Min(0f)] private float cannonScaleDuration = 0.25f;

        [Header("Cannon Grow Effect")]
        [SerializeField] private GameObject cannonGrowEffect;

        [Header("Explosion Effect on Death")]
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private GameObject portalEnd;
        [SerializeField] private float explosionDuration = 3f;

        [Header("Boss Camera (khi trúng phát cuối)")]
        [SerializeField, Min(0f)] private float bossCamHoldTime = 3f;

        #endregion

        #region Runtime Cache

        // Các tham chiếu cần thiết trong runtime
        private BossCore m_boss;
        private DragonRobot m_dragon;

        #endregion

        #region Runtime State

        // Trạng thái trong runtime
        private int m_cutsceneWaterHitCount;
        private bool m_listenCutsceneWaterHits;

        private bool m_lastHitTriggered;
        private bool m_bossCamHoldDone;
        private bool m_bossCamHoldRunning;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// VN: Cache component để dùng lại, tránh GetComponent nhiều lần.
        /// </summary>
        private void Start()
        {
            m_boss = GetComponent<BossCore>();
            m_dragon = GetComponent<DragonRobot>();

            portalEnd.SetActive(false);
        }

        /// <summary>
        /// VN: An toàn - reset camera/tween/state khi object bị disable giữa chừng.
        /// </summary>
        private void OnDisable()
        {
            SetFinalCameraActive(false);

            m_listenCutsceneWaterHits = false;
            m_lastHitTriggered = false;

            m_bossCamHoldDone = true;
            m_bossCamHoldRunning = false;

            if (waterCannon != null)
                waterCannon.transform.DOKill();
        }

        #endregion

        #region Final Sequence Flow

        /// <summary>
        /// VN: Luồng chính: disable cannon khác -> fade -> setup -> finalCam -> bắn -> move player -> tắt player.
        /// </summary>
        public override IEnumerator ExecuteFinalSequence()
        {
            DisableCannonsForFinal();

            PlayerHub.Instance?.SetPlayerControlAndModel(true);

            if (m_boss != null)
                m_boss.IsInCutscene = true;

            m_dragon?.EnterFinalSequenceState();

            yield return RunFadeAndSetup();

            yield return FireBurstAtDragon();

            MovePlayerToFinalPose();
            SetFinalCameraActive(false);

            PlayerHub.Instance?.SetPlayerControlAndModel(false);
        }

        #endregion

        #region Fade / Setup

        /// <summary>
        /// VN: Fade Out -> setup -> Fade In.
        /// Sau khi FadeIn xong thì bật effect pháo lớn lên, rồi mới scale cannon.
        /// </summary>
        private IEnumerator RunFadeAndSetup()
        {
            if (cannonGrowEffect != null)
                cannonGrowEffect.SetActive(false);

            var fader = Fader.instance;

            if (fader == null)
            {
                ActivateWaterCannonAndRepositionBoss();
                SetFinalCameraActive(true);
                yield return null;

                ActivateCannonGrowEffect();
                yield return TweenCannonScaleIfNeeded();
                yield break;
            }

            fader.SetAlpha(0f);

            yield return FadeOut(fader);

            SetFinalCameraActive(true);
            yield return null;

            ActivateWaterCannonAndRepositionBoss();

            yield return FadeIn(fader);

            ActivateCannonGrowEffect();
            yield return TweenCannonScaleIfNeeded();
        }

        /// <summary>
        /// VN: FadeOut tới alpha = 1.
        /// </summary>
        private static IEnumerator FadeOut(Fader fader)
        {
            bool done = false;
            fader.FadeOut(() => done = true);
            yield return new WaitUntil(() => done);
        }

        /// <summary>
        /// VN: FadeIn tới alpha = 0.
        /// </summary>
        private static IEnumerator FadeIn(Fader fader)
        {
            bool done = false;
            fader.FadeIn(() => done = true);
            yield return new WaitUntil(() => done);
        }

        /// <summary>
        /// VN: Bật WaterCannon + teleport boss về finalBossPose (thường gọi lúc đang đen).
        /// </summary>
        private void ActivateWaterCannonAndRepositionBoss()
        {
            if (waterCannon != null)
            {
                waterCannon.gameObject.SetActive(true);
                waterCannon.enabled = true;
                waterCannon.transform.DOKill();
                waterCannon.transform.localScale = new Vector3(cannonScaleFrom, .75f, cannonScaleFrom);
            }

            if (finalBossPose != null)
                transform.SetPositionAndRotation(finalBossPose.position, finalBossPose.rotation);
        }

        /// <summary>
        /// VN: Bật hiệu ứng hỗ trợ cảm giác pháo lớn lên.
        /// </summary>
        private void ActivateCannonGrowEffect()
        {
            if (cannonGrowEffect != null)
                cannonGrowEffect.SetActive(true);
        }

        /// <summary>
        /// VN: Tween scale cannon khi alpha đã về 0.
        /// </summary>
        private IEnumerator TweenCannonScaleIfNeeded()
        {
            if (waterCannon == null)
                yield break;

            var t = waterCannon.transform;
            t.DOKill();

            Vector3 targetScale = new Vector3(cannonScaleTo, 1.5f, cannonScaleTo);

            Tween tw = t.DOScale(targetScale, cannonScaleDuration)
                        .SetUpdate(true);

            yield return tw.WaitForCompletion();
        }

        #endregion

        #region Camera (Final)

        /// <summary>
        /// VN: Bật/tắt camera final bằng priority.
        /// </summary>
        private void SetFinalCameraActive(bool active)
        {
            if (finalCam == null) return;

            finalCam.Priority = active ? finalCamActivePriority : finalCamInactivePriority;
        }

        #endregion

        #region Cannon Disable

        /// <summary>
        /// VN: Tắt các CannonInput khác để không can thiệp cutscene.
        /// </summary>
        private void DisableCannonsForFinal()
        {
            if (cannonsToDisable == null || cannonsToDisable.Length == 0)
                return;

            for (int i = 0; i < cannonsToDisable.Length; i++)
            {
                var c = cannonsToDisable[i];
                if (c != null)
                    c.gameObject.SetActive(false);
            }
        }

        #endregion

        #region Cannon Burst / Hit Waiting

        /// <summary>
        /// VN: Bắn burstShots phát và chờ đủ hit.
        /// Nếu trúng phát cuối thì đợi boss camera focus + giữ 5s xong mới return.
        /// </summary>
        private IEnumerator FireBurstAtDragon()
        {
            if (waterCannon == null || m_dragon == null)
                yield break;

            m_cutsceneWaterHitCount = 0;
            m_listenCutsceneWaterHits = true;

            m_lastHitTriggered = false;
            m_bossCamHoldDone = false;
            m_bossCamHoldRunning = false;

            for (int i = 0; i < burstShots; i++)
            {
                waterCannon.FireProjectile();

                if (i < burstShots - 1)
                    yield return new WaitForSeconds(shotInterval);
            }

            float t = 0f;
            float timeout = (shotInterval * burstShots) + hitWaitExtraTime;

            while (m_listenCutsceneWaterHits && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }

            m_listenCutsceneWaterHits = false;

            if (m_lastHitTriggered)
            {
                StartBossCamHoldIfNeeded();
                yield return new WaitUntil(() => m_bossCamHoldDone);
            }
        }

        #endregion

        #region Player

        /// <summary>
        /// VN: Đưa Player về finalPlayerPose (PlayerHub nằm trên Player).
        /// </summary>
        private void MovePlayerToFinalPose()
        {
            if (finalPlayerPose == null || PlayerHub.Instance == null)
                return;

            PlayerHub.Instance.transform.SetPositionAndRotation(
                finalPlayerPose.position,
                finalPlayerPose.rotation
            );
        }

        #endregion

        #region Boss Camera (Hit cuối)

        /// <summary>
        /// VN: Start routine focus BossCam (chỉ chạy 1 lần).
        /// </summary>
        private void StartBossCamHoldIfNeeded()
        {
            if (m_bossCamHoldRunning) return;
            m_bossCamHoldRunning = true;
            StartCoroutine(BossCamHoldRoutine());
        }

        /// <summary>
        /// VN: Focus camera sang Boss, giữ bossCamHoldTime giây, rồi ReleaseToPlayer và báo done.
        /// </summary>
        private IEnumerator BossCamHoldRoutine()
        {
            var camCtrl = CameraCutsceneController.Instance;

            if (camCtrl == null)
            {
                m_bossCamHoldDone = true;
                yield break;
            }

            Transform target = (m_dragon != null) ? m_dragon.transform : transform;

            SetFinalCameraActive(false);

            camCtrl.AssignSpecialTarget(target);
            yield return camCtrl.FocusTo(BossCamType.Boss);

            if (explosionEffect != null)
            {
                Instantiate(explosionEffect, transform.position, explosionEffect.transform.rotation);
                AudioManager.Instance.PlaySound(5);

                m_dragon.DisableColliderAndModel();
                yield return new WaitForSeconds(explosionDuration);
            }

            portalEnd.SetActive(true);

            if (bossCamHoldTime > 0f)
                yield return new WaitForSeconds(bossCamHoldTime);

            yield return camCtrl.ReleaseToPlayer();
            camCtrl.ClearSpecialTarget();

            m_bossCamHoldDone = true;
        }

        #endregion

        #region Cutscene Projectile Callback

        /// <summary>
        /// VN: WaterProjectile gọi khi trúng boss trong cutscene.
        /// 2 hit đầu: TakeDamage; hit cuối: Death + chuyển sang BossCam.
        /// </summary>
        public void NotifyCutsceneWaterHit()
        {
            if (!m_listenCutsceneWaterHits)
                return;

            if (m_cutsceneWaterHitCount >= burstShots)
                return;

            if (m_boss == null) m_boss = GetComponent<BossCore>();
            if (m_boss == null) return;

            // VN: Double-safe, đảm bảo boss đang bị khóa combat trong cutscene.
            m_dragon?.EnterFinalSequenceState();

            var anim = m_boss.BossAnim;
            if (anim == null) return;

            m_cutsceneWaterHitCount++;

            if (m_cutsceneWaterHitCount < burstShots)
            {
                anim.PlayTakeDamage();
                return;
            }

            anim.PlayDeath();

            m_lastHitTriggered = true;
            m_listenCutsceneWaterHits = false;

            StartBossCamHoldIfNeeded();
        }

        #endregion
    }
}
