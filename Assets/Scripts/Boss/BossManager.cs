using UnityEngine;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Manager")]
    public class BossManager : SingletonMonobehaviour<BossManager>
    {
        [Header("Boss Settings")]
        [SerializeField] private BossCore currentBoss;
        [SerializeField] private bool autoFindBoss = true;

        private BossUI bossUI;

        //─────────────────────────────────────────────
        // UNITY LIFECYCLE
        //─────────────────────────────────────────────
        private void Start()
        {
            StartCoroutine(InitializeBoss());
        }

        //─────────────────────────────────────────────
        // INITIALIZATION
        //─────────────────────────────────────────────
        private IEnumerator InitializeBoss()
        {
            yield return null;

            // Tự tìm boss nếu chưa gán
            if (autoFindBoss && currentBoss == null)
                currentBoss = FindFirstObjectByType<BossCore>();

            // Nếu có boss → tự tìm BossUI trong children của boss
            if (currentBoss != null)
                bossUI = currentBoss.GetComponent<BossUI>();

            // Gắn UI với boss
            if (bossUI != null)
                bossUI.Bind(currentBoss);
        }

        //─────────────────────────────────────────────
        // PUBLIC ACCESS
        //─────────────────────────────────────────────
        public void SetActiveBoss(BossCore boss)
        {
            currentBoss = boss;
            bossUI = boss != null ? boss.GetComponentInChildren<BossUI>() : null;

            if (bossUI != null)
                bossUI.Bind(currentBoss);
        }

        public BossCore GetActiveBoss() => currentBoss;
        public BossUI GetBossUI() => bossUI;
    }
}
