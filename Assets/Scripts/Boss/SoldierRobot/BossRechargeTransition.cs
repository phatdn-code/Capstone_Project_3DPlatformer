using Unity.Cinemachine;
using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    public class BossRechargeTransition : BossPhaseTransitionBase
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("References")]
        [SerializeField] private Transform chargeStationTarget;
        [SerializeField] private Transform returnPoint;
        [SerializeField] private GameObject rechargeEffectPrefab;

        [Tooltip("Các vật thể trong nhà sẽ bị méo khi boss hồi máu.")]
        [SerializeField] private BossEnergyPumpEffect[] energyPumps;

        [Header("Cinemachine Settings")]
        [SerializeField] private CinemachineCamera bossCam;
        [SerializeField] private float cameraFocusTime = 1.5f;
        [SerializeField] private int bossCamPriority = 100;
        [SerializeField] private int normalCamPriority = 0;

        [Header("Heal Settings")]
        [SerializeField] private float rechargeDuration = 10f;

        [Header("Speed Boost Settings")]
        [SerializeField] private float speedBoostMultiplier = 2f;   // Boss chạy nhanh hơn 40%
        [SerializeField] private float speedRestoreDelay = 0.3f;      // Trễ 1 chút trước khi trả về tốc độ bình thường

        #endregion

        //─────────────────────────────────────────────
        #region === MAIN TRANSITION FLOW ===

        public override IEnumerator ExecuteTransition(BossCore boss, int nextPhase)
        {
            // 🧠 Lấy boss đúng kiểu
            SoldierRobot soldierBoss = boss as SoldierRobot;
            if (soldierBoss == null)
                yield break;

            // ⏸️ Tạm dừng hành vi boss
            soldierBoss.SetPaused(true);

            // 🔒 Khóa player
            PlayerLockController.Instance.LockPlayer(true);

            // 🎥 Focus camera sang boss
            yield return FocusCameraOnBoss(true);

            // 🚀 Boss chạy nhanh đến nhà
            yield return MoveBossWithSpeedBoost(soldierBoss, chargeStationTarget);

            // 🔄 Xoay boss hướng về nhà
            yield return soldierBoss.RotateTowardsTarget(chargeStationTarget);

            // 💚 Hồi máu
            yield return StartRechargeSequence(nextPhase, soldierBoss);

            // ⚙️ Chuyển phase mới
            ApplyNextPhase(nextPhase, soldierBoss);

            // 🚀 Quay lại vị trí chiến đấu (cũng chạy nhanh)
            yield return MoveBossWithSpeedBoost(soldierBoss, returnPoint);

            // 🎥 Trả camera về player
            yield return FocusCameraOnBoss(false);

            // 🔓 Mở lại player và boss
            PlayerLockController.Instance.LockPlayer(false);
            soldierBoss.SetPaused(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === STEP 1: CAMERA FOCUS ===

        private IEnumerator FocusCameraOnBoss(bool enable)
        {
            if (bossCam == null)
                yield break;

            bossCam.Priority = enable ? bossCamPriority : normalCamPriority;
            yield return new WaitForSeconds(cameraFocusTime);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === STEP 2: MOVE WITH SPEED BOOST ===

        private IEnumerator MoveBossWithSpeedBoost(SoldierRobot soldierBoss, Transform target)
        {
            if (soldierBoss == null || target == null)
                yield break;

            // 🚀 Tăng tốc
            soldierBoss.SetSpeedMultiplier(speedBoostMultiplier);

            // Di chuyển
            yield return soldierBoss.MoveToTarget(target);

            // ⏳ Khôi phục tốc độ sau khi đến nơi
            soldierBoss.SetSpeedMultiplier(1f, speedRestoreDelay);
        }


        #endregion

        //─────────────────────────────────────────────
        #region === STEP 3: HEAL SEQUENCE ===

        private IEnumerator StartRechargeSequence(int nextPhase, SoldierRobot soldierBoss)
        {
            soldierBoss.PlayRechargeAnimation(true);

            StartPumpAndRechargeEffect();
            yield return RechargeBossOverTime(nextPhase, soldierBoss);
            EndPumpAndRechargeEffect();

            soldierBoss.PlayRechargeAnimation(false);
        }

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

        private void StartPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.PlayMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(true);
        }

        private void EndPumpAndRechargeEffect()
        {
            foreach (var morph in energyPumps)
                morph?.RevertMorph();

            if (rechargeEffectPrefab != null)
                rechargeEffectPrefab.SetActive(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === STEP 4: APPLY NEXT PHASE ===

        private void ApplyNextPhase(int nextPhase, SoldierRobot soldierBoss)
        {
            soldierBoss.bossHealth.InitializePhase(nextPhase, soldierBoss.phases[nextPhase].maxHealth);
            soldierBoss.ApplyPhaseVisual(nextPhase, instant: false);
            soldierBoss.OnBossPhaseStartEvent.Invoke(nextPhase);
        }

        #endregion
    }
}
