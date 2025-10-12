using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Factory chịu trách nhiệm spawn SoldierRobot boss
    /// và thiết lập các phase chiến đấu cho nó.
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Soldier Robot Factory")]
    public class SoldierRobotFactory : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Prefab & Spawn Settings
        [Header("Boss Prefabs")]
        [SerializeField] private SoldierRobot soldierRobotPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool autoSpawnOnStart = true;

        // ─────────────────────────────────────────────
        // Runtime
        private SoldierRobot currentBoss;

        /// <summary>
        /// Boss hiện tại trong scene (runtime).
        /// </summary>
        public SoldierRobot CurrentBoss => currentBoss;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        private void Start()
        {
            if (autoSpawnOnStart)
                SpawnSoldierRobot();
        }

        // ─────────────────────────────────────────────
        // Public Methods
        /// <summary>
        /// Spawn SoldierRobot mới tại spawnPoint.
        /// Nếu đã có boss cũ → huỷ trước.
        /// </summary>
        public SoldierRobot SpawnSoldierRobot()
        {
            // Huỷ boss cũ
            if (currentBoss != null)
                Destroy(currentBoss.gameObject);

            if (soldierRobotPrefab == null || spawnPoint == null)
            {
                Debug.LogError("❌ SoldierRobotFactory: Prefab hoặc spawnPoint chưa gán!");
                return null;
            }

            // Tạo boss mới
            SoldierRobot bossInstance = Instantiate(soldierRobotPrefab, spawnPoint.position, spawnPoint.rotation);
            currentBoss = bossInstance;

            // Thiết lập phase cho boss
            SetupBossPhases(bossInstance);

            Debug.Log("✅ SoldierRobotFactory: Spawn boss thành công!");
            return bossInstance;
        }

        // ─────────────────────────────────────────────
        // Private Helpers
        /// <summary>
        /// Thiết lập các phase chiến đấu cho SoldierRobot.
        /// Ở mỗi phase có thể thay đổi máu, tốc độ, skill…
        /// </summary>
        private void SetupBossPhases(SoldierRobot boss)
        {
            if (boss == null)
            {
                Debug.LogError("❌ SetupBossPhases: boss null");
                return;
            }

            // Ví dụ: SoldierRobot có 3 phase
            boss.phases = new BossPhase[3];

            for (int i = 0; i < boss.phases.Length; i++)
            {
                boss.phases[i] = new BossPhase
                {
                    phaseName = $"Phase {i + 1}",
                    maxHealth = boss.bossHealth.initialHealth / (i + 1),
                    moveSpeed = 2f + i,
                    attackSpeed = 1f + (0.5f * i),
                    damage = 10 * (i + 1),
                    sightRange = 5f + i * 2,
                    scale = Vector3.one * (1f + 0.2f * i),
                    canUseSpecialAbility = (i == boss.phases.Length - 1), // chỉ phase cuối
                    specialAbilityName = (i == boss.phases.Length - 1) ? "Rage Mode" : "",
                    specialAbilityCooldown = (i == boss.phases.Length - 1) ? 5f : 0f,

                    // Phase cuối chuyển sang màu đỏ
                    phaseColor = (i == boss.phases.Length - 1) ? Color.red : Color.white
                };
            }

            Debug.Log("⚙️ SoldierRobotFactory: Setup phases hoàn tất.");
        }
    }
}
