using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Factory pattern quản lý việc spawn và setup Boss
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Factory")]
    public class BossFactory : SingletonMonobehaviour<BossFactory>
    {
        [Header("Boss Prefabs")]
        [SerializeField] private GameObject meleeBossPrefab;
        [SerializeField] private GameObject rangedBossPrefab;
        [SerializeField] private GameObject magicBossPrefab;

        [Header("Spawn Settings")]
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private bool autoSpawnOnStart = true;
        [SerializeField] private BossType defaultBossType = BossType.Melee;

        private BaseBoss m_currentBoss;
        public BaseBoss currentBoss => m_currentBoss;

        // Dictionary để ánh xạ loại Boss → prefab
        private Dictionary<BossType, GameObject> prefabMap;

        private void Start()
        {
            // Khởi tạo mapping BossType → Prefab
            prefabMap = new Dictionary<BossType, GameObject>
            {
                { BossType.Melee, meleeBossPrefab },
                { BossType.Ranged, rangedBossPrefab },
                { BossType.Magic, magicBossPrefab }
            };

            if (autoSpawnOnStart)
                SpawnBoss(defaultBossType);
        }

        /// <summary>
        /// Spawn một Boss theo loại đã chỉ định
        /// </summary>
        public BaseBoss SpawnBoss(BossType type)
        {
            // Hủy boss cũ nếu có
            if (m_currentBoss != null)
                Destroy(m_currentBoss.gameObject);

            if (!prefabMap.TryGetValue(type, out var prefab) || prefab == null)
            {
                Debug.LogError($"Không tìm thấy prefab cho BossType {type}");
                return null;
            }

            // Tạo boss mới
            var bossGO = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

            m_currentBoss = bossGO.GetComponent<BaseBoss>();

            if (m_currentBoss == null)
            {
                Debug.LogError("Prefab không có component BaseBoss!");
                return null;
            }

            // Setup phases cho boss
            SetupBossPhases(m_currentBoss);

            return m_currentBoss;
        }

        /// <summary>
        /// Thiết lập các phase cho Boss (tùy chỉnh cho từng loại)
        /// </summary>
        private void SetupBossPhases(BaseBoss boss)
        {
            if (boss == null) return;

            // Ví dụ: mỗi boss có 3 giai đoạn
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
                    canUseSpecialAbility = (i == boss.phases.Length - 1), // chỉ phase cuối có special
                    specialAbilityName = (i == boss.phases.Length - 1) ? "Final Burst" : "",
                    specialAbilityCooldown = (i == boss.phases.Length - 1) ? 5f : 0f,
                    phaseColor = (i == boss.phases.Length - 1) ? Color.red : Color.white
                };
            }
        }
    }
}
