using Unity.Cinemachine;
using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// ✦ Xử lý quá trình Boss SoldierRobot hồi năng lượng và chuyển phase.
    /// Gồm di chuyển về trạm nạp, hiệu ứng hồi máu, camera focus, slow motion, v.v.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoldierRobotRechargeSequence : BossPhaseTransitionBase
    {
        //─────────────────────────────────────────────
        #region ✦ THAM CHIẾU INSPECTOR ✦

        [Header("Tham chiếu vị trí")]
        [SerializeField] private Transform chargeStationTarget;
        [SerializeField] private Transform returnPoint;
        [SerializeField] private GameObject rechargeEffectPrefab;

        [Header("Hiệu ứng hồi năng lượng & Quái phụ Phase 2")]
        [SerializeField] private BossEnergyPumpEffect[] energyPumps;
        [SerializeField] private SupportGunnerAI supportGunner;

        [Header("Thiết lập hồi năng lượng")]
        [SerializeField] private float rechargeDuration = 10f;

        [Header("Thiết lập tốc độ di chuyển")]
        [SerializeField] private float speedBoostMultiplier = 2f;
        [SerializeField] private float speedRestoreDelay = 0.3f;

        #endregion


        //─────────────────────────────────────────────
        #region ✦ BIẾN RUNTIME ✦

        private BossCore boss;
        private SoldierRobot soldierBoss;

        #endregion


        //─────────────────────────────────────────────
        #region ✦ UNITY LIFECYCLE ✦

        /// <summary>
        /// Lấy reference đến BossCore khi khởi tạo.
        /// </summary>
        private void Start()
        {
            boss = GetComponent<BossCore>();

            soldierBoss = boss as SoldierRobot;
        }

        #endregion


        //─────────────────────────────────────────────
        #region ✦ LUỒNG CHÍNH: CHUYỂN PHA ✦

        /// <summary>
        /// Toàn bộ quá trình hồi năng lượng & chuyển sang phase kế tiếp.
        /// </summary>
        public override IEnumerator ExecuteTransition(int nextPhase)
        {
            boss.IsInCutscene = true;

            // Khóa điều khiển người chơi
            soldierBoss.SetPaused(true);
            PlayerHub.Instance.LockPlayer(true);

            // Focus camera vào boss
            yield return CameraCutsceneController.instance.FocusTo(BossCamType.Boss);

            // Boss chạy đến trạm nạp
            yield return MoveBossWithSpeedBoost(chargeStationTarget);
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);

            // Bắt đầu chuỗi hồi năng lượng
            yield return StartRechargeSequence(nextPhase);

            // Chuyển sang phase mới
            ApplyNextPhase(nextPhase);

            // Boss quay về vị trí cũ
            yield return MoveBossWithSpeedBoost(returnPoint);

            // Trả camera lại cho player & mở khóa điều khiển
            yield return CameraCutsceneController.instance.ReleaseToPlayer();
            PlayerHub.Instance.LockPlayer(false);
            soldierBoss.SetPaused(false);

            boss.IsInCutscene = false;
        }

        #endregion


        //─────────────────────────────────────────────
        #region ✦ CUTSCENE MODE (FINAL PHASE) ✦

        /// <summary>
        /// Chỉ phát cutscene hồi năng lượng, không đổi phase.
        /// </summary>
        public IEnumerator PlayRechargeCutsceneOnly()
        {
            if (soldierBoss == null) yield break;

            yield return MoveBossWithSpeedBoost(chargeStationTarget);
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);

            soldierBoss.PlayRechargeAnimation(true);
            StartPumpAndRechargeEffect();
            yield return new WaitForSeconds(3f);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BƯỚC 2: DI CHUYỂN VỚI TĂNG TỐC ✦

        /// <summary>
        /// Cho boss di chuyển nhanh đến vị trí chỉ định.
        /// </summary>
        private IEnumerator MoveBossWithSpeedBoost(Transform target)
        {
            if (soldierBoss == null || target == null) yield break;

            soldierBoss.SetSpeedMultiplier(speedBoostMultiplier);
            yield return soldierBoss.MoveToTarget(target);
            soldierBoss.SetSpeedMultiplier(1f, speedRestoreDelay);
        }

        #endregion


        //─────────────────────────────────────────────
        #region ✦ BƯỚC 3: HỒI NĂNG LƯỢNG ✦

        /// <summary>
        /// Gọi animation + hiệu ứng hồi năng lượng theo thời gian.
        /// </summary>
        private IEnumerator StartRechargeSequence(int nextPhase)
        {
            soldierBoss.PlayRechargeAnimation(true);
            StartPumpAndRechargeEffect();
            yield return RechargeBossOverTime(nextPhase);
            EndPumpAndRechargeEffect();
            soldierBoss.PlayRechargeAnimation(false);
        }

        /// <summary>
        /// Tăng máu dần theo thời gian trong quá trình hồi.
        /// </summary>
        private IEnumerator RechargeBossOverTime(int nextPhase)
        {
            float elapsed = 0f;
            float startHealth = soldierBoss.BossHealth.CurrentHealth;
            float targetHealth = soldierBoss.Phases[nextPhase].maxHealth;

            float current = startHealth;
            float velocity = 0f;

            while (elapsed < rechargeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / rechargeDuration);

                float target = Mathf.Lerp(startHealth, targetHealth, t);
                current = Mathf.SmoothDamp(current, target, ref velocity, 0.15f);

                soldierBoss.BossHealth.SetHealth(current);

                foreach (var morph in energyPumps)
                    morph?.UpdateMorphProgress(t);

                yield return null;
            }

            // Đảm bảo đầy máu cuối cùng
            soldierBoss.BossHealth.SetHealth(targetHealth);
        }

        /// <summary>
        /// Bắt đầu hiệu ứng morph & particle hồi năng lượng.
        /// </summary>
        private void StartPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.PlayMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(true);
        }

        /// <summary>
        /// Kết thúc hiệu ứng hồi năng lượng.
        /// </summary>
        public void EndPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.RevertMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(false);
        }

        #endregion


        //─────────────────────────────────────────────
        #region ✦ BƯỚC 4: ÁP DỤNG PHA MỚI ✦

        /// <summary>
        /// Áp dụng dữ liệu phase mới và kích hoạt SupportGunner nếu cần.
        /// </summary>
        private void ApplyNextPhase(int nextPhase)
        {
            soldierBoss.BossHealth.InitializePhase(nextPhase, soldierBoss.Phases[nextPhase].maxHealth);
            soldierBoss.ApplyPhaseVisual(nextPhase, instant: false);
            soldierBoss.OnBossPhaseStartEvent.Invoke(nextPhase);

            if (nextPhase == 1 && supportGunner != null)
            {
                Vector3 center = MovementBoundaryZone.Instance.transform.position;
                float radius = MovementBoundaryZone.Instance.GetBoundaryRadius();
                supportGunner.ActivateGunner(center, radius);
            }
        }

        #endregion
    }
}
