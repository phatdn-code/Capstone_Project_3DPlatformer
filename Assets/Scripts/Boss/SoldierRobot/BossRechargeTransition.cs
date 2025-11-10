using Unity.Cinemachine;
using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    public class BossRechargeTransition : BossPhaseTransitionBase
    {
        //─────────────────────────────────────────────
        #region ✦ THAM CHIẾU INSPECTOR ✦

        [Header("Tham chiếu đối tượng")]
        [SerializeField] private Transform chargeStationTarget;
        [SerializeField] private Transform returnPoint;
        [SerializeField] private GameObject rechargeEffectPrefab;

        [Header("Hiệu ứng hồi năng lượng & Quái phụ Phase 2")]
        [SerializeField] private BossEnergyPumpEffect[] energyPumps;
        [SerializeField] private SupportGunnerAI supportGunner;

        [Header("Cinemachine Settings")]
        [SerializeField] private CinemachineCamera bossCam;
        [SerializeField] private float cameraFocusTime = 1.5f;
        [SerializeField] private int bossCamPriority = 100;
        [SerializeField] private int normalCamPriority = 0;

        [Header("Thiết lập hồi máu")]
        [SerializeField] private float rechargeDuration = 10f;

        [Header("Thiết lập tốc độ di chuyển")]
        [SerializeField] private float speedBoostMultiplier = 2f;   // Boss chạy nhanh hơn
        [SerializeField] private float speedRestoreDelay = 0.3f;    // Trễ trước khi trả lại tốc độ bình thường

        #endregion

        //─────────────────────────────────────────────
        #region ✦ LUỒNG CHUYỂN PHA CHÍNH ✦

        /// <summary>Toàn bộ quá trình hồi năng lượng & chuyển phase.</summary>
        public override IEnumerator ExecuteTransition(BossCore boss, int nextPhase)
        {
            SoldierRobot soldierBoss = boss as SoldierRobot;
            if (soldierBoss == null) yield break;

            soldierBoss.SetPaused(true);
            PlayerHub.Instance.LockPlayer(true);

            yield return FocusCameraOnBoss(true);
            yield return MoveBossWithSpeedBoost(soldierBoss, chargeStationTarget);
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);

            yield return StartRechargeSequence(nextPhase, soldierBoss);
            ApplyNextPhase(nextPhase, soldierBoss);

            yield return MoveBossWithSpeedBoost(soldierBoss, returnPoint);
            yield return FocusCameraOnBoss(false);

            PlayerHub.Instance.LockPlayer(false);
            soldierBoss.SetPaused(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ CHẾ ĐỘ CUTSCENE (FINAL PHASE) ✦

        /// <summary>Chỉ hiển thị hiệu ứng hồi năng lượng, không đổi phase.</summary>
        public IEnumerator PlayRechargeCutsceneOnly(SoldierRobot soldierBoss)
        {
            if (soldierBoss == null) yield break;

            soldierBoss.SetPaused(true);
            PlayerHub.Instance.LockPlayer(true);

            yield return FocusCameraOnBoss(true);
            yield return MoveBossWithSpeedBoost(soldierBoss, chargeStationTarget);
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);

            soldierBoss.PlayRechargeAnimation(true);
            StartPumpAndRechargeEffect();

            yield return new WaitForSeconds(3f);

            soldierBoss.PlayRechargeAnimation(false);
            EndPumpAndRechargeEffect();

            yield return FocusCameraOnBoss(false);
            PlayerHub.Instance.LockPlayer(false);
        }

        /// <summary>Boss chỉ chạy đến trạm nạp năng lượng (không hồi).</summary>
        public IEnumerator MoveBossToChargeStationOnly(SoldierRobot soldierBoss)
        {
            if (soldierBoss == null) yield break;

            yield return FocusCameraOnBoss(true);
            yield return MoveBossWithSpeedBoost(soldierBoss, chargeStationTarget);
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);
            yield return FocusCameraOnBoss(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BƯỚC 1: CAMERA FOCUS ✦

        /// <summary>Focus camera vào boss hoặc trả lại player.</summary>
        private IEnumerator FocusCameraOnBoss(bool enable)
        {
            if (bossCam == null) yield break;

            bossCam.Priority = enable ? bossCamPriority : normalCamPriority;
            yield return new WaitForSeconds(cameraFocusTime);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BƯỚC 2: DI CHUYỂN VỚI TĂNG TỐC ✦

        /// <summary>Cho boss di chuyển nhanh đến mục tiêu.</summary>
        private IEnumerator MoveBossWithSpeedBoost(SoldierRobot soldierBoss, Transform target)
        {
            if (soldierBoss == null || target == null) yield break;

            soldierBoss.SetSpeedMultiplier(speedBoostMultiplier);
            yield return soldierBoss.MoveToTarget(target);
            soldierBoss.SetSpeedMultiplier(1f, speedRestoreDelay);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BƯỚC 3: QUÁ TRÌNH HỒI NĂNG LƯỢNG ✦

        /// <summary>Animation + hiệu ứng hồi máu.</summary>
        private IEnumerator StartRechargeSequence(int nextPhase, SoldierRobot soldierBoss)
        {
            soldierBoss.PlayRechargeAnimation(true);
            StartPumpAndRechargeEffect();
            yield return RechargeBossOverTime(nextPhase, soldierBoss);
            EndPumpAndRechargeEffect();
            soldierBoss.PlayRechargeAnimation(false);
        }

        /// <summary>Tăng máu dần theo thời gian.</summary>
        private IEnumerator RechargeBossOverTime(int nextPhase, SoldierRobot soldierBoss)
        {
            float elapsed = 0f;
            float startHealth = soldierBoss.bossHealth.CurrentHealth;
            float targetHealth = soldierBoss.phases[nextPhase].maxHealth;

            while (elapsed < rechargeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / rechargeDuration);

                soldierBoss.bossHealth.SetHealth(Mathf.Lerp(startHealth, targetHealth, t));

                foreach (var morph in energyPumps)
                    morph?.UpdateMorphProgress(t);

                yield return null;
            }
        }

        /// <summary>Bắt đầu hiệu ứng hồi năng lượng.</summary>
        private void StartPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.PlayMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(true);
        }

        /// <summary>Kết thúc hiệu ứng hồi năng lượng.</summary>
        private void EndPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.RevertMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region ✦ BƯỚC 4: CHUYỂN PHA ✦

        /// <summary>Áp dụng thông số cho phase mới & kích hoạt quái phụ.</summary>
        private void ApplyNextPhase(int nextPhase, SoldierRobot soldierBoss)
        {
            soldierBoss.bossHealth.InitializePhase(nextPhase, soldierBoss.phases[nextPhase].maxHealth);
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
