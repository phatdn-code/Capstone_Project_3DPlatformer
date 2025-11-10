using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// ✦ Xử lý đoạn Final Boss Sequence của SoldierRobot:
    /// - Boss bắn bomb khổng lồ, quay về hồi năng lượng, chờ player phản công.
    /// - Gồm các hiệu ứng camera, slow motion, cutscene và chiến thắng.
    /// </summary>
    public class SoldierRobotFinalSequence : BossFinalSequenceBase
    {
        //─────────────────────────────────────────────
        #region ✦ THAM CHIẾU INSPECTOR ✦

        [Header("Thiết lập Bomb")]
        [SerializeField] private BossBomb giantBombPrefab;
        [SerializeField] private Transform bombSpawnPoint;
        [SerializeField] private Transform bombCenterPoint;

        [Header("Tham chiếu khác")]
        [SerializeField] private BossRechargeTransition rechargeTransition;
        [SerializeField] private SupportGunnerAI supportGunner;
        [SerializeField] private CinemachineCamera bossCam;
        [SerializeField] private CinemachineCamera bombCam;

        [Header("Thiết lập Slow Motion")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 1.5f;

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BIẾN NỘI BỘ ✦

        #endregion

        //─────────────────────────────────────────────
        #region ✦ TRÌNH TỰ CHÍNH ✦

        /// <summary>
        /// Trình tự chính của Final Sequence: 
        /// Boss bắn bomb → hồi năng lượng → player đánh bomb → slow motion win.
        /// </summary>
        public override IEnumerator ExecuteFinalSequence(BossLinker linker)
        {
            var boss = linker.bossCore as SoldierRobot;
            if (boss == null) yield break;

            var anim = linker.bossAnim;

            // 🔒 Khóa điều khiển và tạm dừng boss
            PlayerHub.Instance.LockPlayer(true);
            boss.SetPaused(true);

            // 1️⃣ Focus camera vào boss
            yield return FocusCamera(bossCam);

            // 2️⃣ Boss bắn bomb khổng lồ ra giữa sân
            yield return ShootGiantBomb(anim);

            // 3️⃣ Focus lại camera về boss
            yield return FocusCamera(bossCam);

            // 4️⃣ Boss + Support chạy về trạm hồi năng lượng
            yield return MoveToRechargeStation(boss);

            // 5️⃣ Boss hồi năng lượng (cutscene)
            yield return rechargeTransition.PlayRechargeCutsceneOnly(boss);

            // 6️⃣ Focus camera về player
            yield return FocusCamera(null);

            // 7️⃣ Player được tự do → đánh bomb → slow motion win
            PlayerHub.Instance.LockPlayer(false);
            yield return WaitForBombReflect(linker);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ XỬ LÝ CAMERA ✦

        /// <summary>
        /// Focus camera vào camera được chọn, hoặc tắt toàn bộ.
        /// </summary>
        private IEnumerator FocusCamera(CinemachineCamera targetCam)
        {
            // Tắt tất cả camera
            if (bossCam != null) bossCam.Priority = 0;
            if (bombCam != null) bombCam.Priority = 0;

            // Kích hoạt camera mục tiêu
            if (targetCam != null)
                targetCam.Priority = 100;

            yield return new WaitForSeconds(0.5f); // thời gian chuyển mượt
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BẮN BOMB ✦

        /// <summary>
        /// Boss bắn bomb khổng lồ ra giữa sân và focus camera vào bomb.
        /// </summary>
        private IEnumerator ShootGiantBomb(BossAnimationBase anim)
        {
            anim?.PlaySpecialSkill();
            yield return new WaitForSeconds(0.5f);

            if (giantBombPrefab && bombSpawnPoint && bombCenterPoint)
            {
                var bombComp = PoolManager.Instance.ReuseComponent(
                    giantBombPrefab.gameObject,
                    bombSpawnPoint.position,
                    Quaternion.identity
                )?.GetComponent<BossBomb>();

                if (bombComp)
                {
                    bombComp.DisableFuseOnLand = true;
                    bombComp.LaunchToPosition(bombCenterPoint.position);
                }
            }

            // Focus camera vào bomb
            if (bombCam != null)
                yield return FocusCamera(bombCam);

            yield return new WaitForSeconds(1.5f); // chờ bomb bay đến giữa sân
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BOSS & SUPPORT DI CHUYỂN ✦

        /// <summary>
        /// Cho boss và quái phụ quay về trạm hồi năng lượng.
        /// </summary>
        private IEnumerator MoveToRechargeStation(SoldierRobot boss)
        {
            if (supportGunner != null)
                yield return supportGunner.ReturnToIdlePoint();

            if (rechargeTransition != null)
                yield return rechargeTransition.MoveBossToChargeStationOnly(boss);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ CHỜ PLAYER PHẢN CÔNG ✦

        /// <summary>
        /// Chờ player đánh bomb → focus camera → slow motion chiến thắng.
        /// </summary>
        private IEnumerator WaitForBombReflect(BossLinker linker)
        {
            GiantBombController bomb = FindFirstObjectByType<GiantBombController>();
            bool reflected = false;

            if (bomb)
            {
                bomb.onHitBoss += () => reflected = true;

                // Khi player đánh → focus camera vào bomb
                bomb.onHitBoss += () =>
                {
                    if (bombCam != null)
                        bombCam.Priority = 100;
                };
            }

            while (!reflected)
                yield return null;

            yield return PlaySlowMotionWin(linker);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ SLOW MOTION & THẮNG TRẬN ✦

        /// <summary>
        /// Hiệu ứng slow motion khi bomb phản công trúng boss → thắng trận.
        /// </summary>
        private IEnumerator PlaySlowMotionWin(BossLinker linker)
        {
            float orig = Time.timeScale;
            Time.timeScale = slowMoScale;
            yield return new WaitForSecondsRealtime(slowMoDuration);
            Time.timeScale = orig;

            // Boss nổ chết
            linker.PlayDeathAnim();

            // Có thể thêm UI chiến thắng ở đây
            // linker.bossUI.ShowWinScreen();
        }

        #endregion
    }
}
