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

        [Header("Refs")]
        [SerializeField] private WaterCannon waterCannon;

        [Header("Final Poses")]
        [SerializeField] private Transform finalBossPose;
        [SerializeField] private Transform finalPlayerPose;

        [Header("Final Camera")]
        [SerializeField] private CinemachineCamera finalCam;
        [SerializeField] private int finalCamActivePriority = 100;
        [SerializeField] private int finalCamInactivePriority = 0;

        [Header("Cannon Burst")]
        [SerializeField, Min(1)] private int burstShots = 3;
        [SerializeField, Min(0f)] private float shotInterval = 1.5f;

        [Header("Cutscene Hit Wait")]
        [SerializeField, Min(0f)] private float hitWaitExtraTime = 3f; // Chống kẹt nếu đạn miss

        #endregion

        #region Runtime Cache / State

        private BossCore m_boss;
        private DragonRobot m_dragon;

        private int m_cutsceneWaterHitCount;
        private bool m_listenCutsceneWaterHits;

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// VN: Cache component để dùng lại, tránh GetComponent nhiều lần.
        /// </summary>
        private void Start()
        {
            m_boss = GetComponent<BossCore>();
            m_dragon = GetComponent<DragonRobot>();
        }

        /// <summary>
        /// VN: An toàn - nếu object bị disable giữa chừng thì trả priority camera về mặc định.
        /// </summary>
        private void OnDisable()
        {
            SetFinalCameraActive(false);
            m_listenCutsceneWaterHits = false;
        }

        #endregion

        #region Final Sequence Flow

        /// <summary>
        /// VN: Luồng final sequence: fade -> setup -> bật cam -> cannon bắn -> đưa player vào pose -> tắt player.
        /// </summary>
        public override IEnumerator ExecuteFinalSequence()
        {
            if (m_boss != null)
                m_boss.IsInCutscene = true;

            // 1) Fade để che màn hình khi setup/teleport
            yield return RunFadeAndSetup();

            // 2) Bật camera final
            SetFinalCameraActive(true);

            // Cho Cinemachine cập nhật 1 frame
            yield return null;

            // 3) Cannon bắn burstShots phát (đạn trúng boss sẽ gọi NotifyCutsceneWaterHit)
            yield return FireBurstAtDragon();

            // 4) Đưa player về final pose
            MovePlayerToFinalPose();

            // 5) Tắt camera final
            SetFinalCameraActive(false);

            // 6) Tắt điều khiển + model player (theo yêu cầu)
            if (PlayerHub.Instance != null)
                PlayerHub.Instance.SetPlayerControlAndModel(false);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// VN: Fade Out -> setup (bật cannon + teleport boss) -> Fade In. Nếu không có Fader thì vẫn setup.
        /// </summary>
        private IEnumerator RunFadeAndSetup()
        {
            var fader = Fader.instance;

            if (fader == null)
            {
                ActivateWaterCannonAndRepositionBoss();
                yield break;
            }

            fader.SetAlpha(0f);

            bool done = false;

            // FadeOut -> alpha lên 1 (màn hình đen)
            done = false;
            fader.FadeOut(() => done = true);
            yield return new WaitUntil(() => done);

            // Setup lúc đang đen
            ActivateWaterCannonAndRepositionBoss();

            // FadeIn -> alpha về 0 (hiện lại)
            done = false;
            fader.FadeIn(() => done = true);
            yield return new WaitUntil(() => done);
        }

        /// <summary>
        /// VN: Bật WaterCannon + teleport boss về finalBossPose (thường gọi lúc màn hình đang đen).
        /// </summary>
        private void ActivateWaterCannonAndRepositionBoss()
        {
            if (waterCannon != null)
            {
                waterCannon.gameObject.SetActive(true);
                waterCannon.enabled = true;
            }

            if (finalBossPose == null)
                return;

            transform.DOKill();
            transform.SetPositionAndRotation(finalBossPose.position, finalBossPose.rotation);
        }

        /// <summary>
        /// VN: Bật/tắt camera final bằng cách đổi priority.
        /// </summary>
        private void SetFinalCameraActive(bool active)
        {
            if (finalCam == null) return;
            finalCam.Priority = active ? finalCamActivePriority : finalCamInactivePriority;
        }

        /// <summary>
        /// VN: Cannon bắn burstShots phát; chờ đủ hit (hoặc timeout nếu đạn miss).
        /// </summary>
        private IEnumerator FireBurstAtDragon()
        {
            if (waterCannon == null || m_dragon == null)
                yield break;

            // Reset trạng thái nhận hit trong cutscene
            m_cutsceneWaterHitCount = 0;
            m_listenCutsceneWaterHits = true;

            // Bắn burstShots phát
            for (int i = 0; i < burstShots; i++)
            {
                waterCannon.FireProjectile();

                if (i < burstShots - 1)
                    yield return new WaitForSeconds(shotInterval);
            }

            // Chờ hit cuối để chắc death anim kịp chạy (tránh kẹt nếu đạn miss)
            float t = 0f;
            float timeout = (shotInterval * burstShots) + hitWaitExtraTime;

            while (m_listenCutsceneWaterHits && t < timeout)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // Nếu timeout mà chưa đủ hit, tắt listen để không ảnh hưởng về sau
            m_listenCutsceneWaterHits = false;
        }

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

        #region Cutscene Projectile Callback

        /// <summary>
        /// VN: Được gọi khi WaterProjectile trúng boss trong cutscene.
        /// Chỉ play animation (2 hit đầu TakeDamage, hit cuối Death), không chạy logic khác.
        /// </summary>
        public void NotifyCutsceneWaterHit()
        {
            if (!m_listenCutsceneWaterHits)
                return;

            // Chặn vượt số hit cần (phòng trường hợp 1 projectile trigger nhiều collider)
            if (m_cutsceneWaterHitCount >= burstShots)
                return;

            // Dùng cache; fallback nếu hiếm khi Start chưa kịp chạy
            if (m_boss == null) m_boss = GetComponent<BossCore>();
            if (m_boss == null) return;

            var anim = m_boss.BossAnim;
            if (anim == null) return;

            m_cutsceneWaterHitCount++;

            if (m_cutsceneWaterHitCount < burstShots)
                anim.PlayTakeDamage();

            else
            {
                anim.PlayDeath();
                m_listenCutsceneWaterHits = false;
            }
        }

        #endregion
    }
}
