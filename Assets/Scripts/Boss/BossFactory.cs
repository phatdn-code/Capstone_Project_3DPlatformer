using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Data structure cho boss phase setup
    /// </summary>
    [System.Serializable]
    public struct BossPhaseData
    {
        public string phaseName;
        public int maxHealth;
        public float moveSpeed;
        public float attackSpeed;
        public int damage;
        public float sightRange;
        public Color phaseColor;
        public Vector3 scale;
        public bool canUseSpecialAbility;
        public string specialAbilityName;
        public float specialAbilityCooldown;

        public BossPhaseData(string name, int health, float move, float attack, int dmg, float sight, 
                           Color color, Vector3 scl, bool special, string ability, float cooldown)
        {
            phaseName = name;
            maxHealth = health;
            moveSpeed = move;
            attackSpeed = attack;
            damage = dmg;
            sightRange = sight;
            phaseColor = color;
            scale = scl;
            canUseSpecialAbility = special;
            specialAbilityName = ability;
            specialAbilityCooldown = cooldown;
        }
    }
    /// <summary>
    /// Factory pattern để tạo các loại boss khác nhau
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Factory")]
    public class BossFactory : SingletonMonobehaviour<BossFactory>
    {
        [Header("Boss Prefabs")]
        [Tooltip("Prefab Melee Boss")]
        [SerializeField] private GameObject meleeBossPrefab;
        
        [Tooltip("Prefab Ranged Boss")]
        [SerializeField] private GameObject rangedBossPrefab;
        
        [Tooltip("Prefab Magic Boss")]
        [SerializeField] private GameObject magicBossPrefab;

        [Header("Spawn Settings")]
        [Tooltip("Vị trí spawn boss")]
        [SerializeField] private Transform spawnPoint;
        
        [Tooltip("Tự động spawn boss khi start")]
        [SerializeField] private bool autoSpawnOnStart = true;
        
        [Tooltip("Loại boss mặc định")]
        [SerializeField] private BossType defaultBossType = BossType.Melee;

        private BaseBoss m_currentBoss;

        /// <summary>
        /// Boss hiện tại
        /// </summary>
        public BaseBoss currentBoss => m_currentBoss;

        protected override void Awake()
        {
            base.Awake();

            if (autoSpawnOnStart)
                SpawnBoss(defaultBossType);
        }

        /// <summary>
        /// Spawn boss theo loại
        /// </summary>
        /// <param name="bossType">Loại boss</param>
        /// <returns>Boss được tạo</returns>
        public BaseBoss SpawnBoss(BossType bossType)
        {
            // Destroy boss cũ nếu có
            if (m_currentBoss != null)
                Destroy(m_currentBoss.gameObject);

            GameObject bossPrefab = GetBossPrefab(bossType);
            if (bossPrefab == null)
            {
                Debug.LogError($"Không tìm thấy prefab cho boss type: {bossType}");
                return null;
            }

            // Tạo boss
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject bossObject = Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
            
            // Lấy component BaseBoss
            m_currentBoss = bossObject.GetComponent<BaseBoss>();
            
            if (m_currentBoss == null)
            {
                Debug.LogError($"Prefab {bossPrefab.name} không có component BaseBoss!");
                Destroy(bossObject);
                return null;
            }

            // Setup boss
            SetupBoss(m_currentBoss, bossType);

            Debug.Log($"Đã spawn {bossType} boss!");
            return m_currentBoss;
        }

        /// <summary>
        /// Lấy prefab theo loại boss
        /// </summary>
        private GameObject GetBossPrefab(BossType bossType)
        {
            switch (bossType)
            {
                case BossType.Melee:
                    return meleeBossPrefab;
                case BossType.Ranged:
                    return rangedBossPrefab;
                case BossType.Magic:
                    return magicBossPrefab;
                default:
                    return meleeBossPrefab;
            }
        }

        /// <summary>
        /// Setup boss sau khi spawn
        /// </summary>
        private void SetupBoss(BaseBoss boss, BossType bossType)
        {
            // Setup phases dựa trên loại boss
            SetupBossPhases(boss, bossType);
            
            // Setup stats dựa trên loại boss
            SetupBossStats(boss, bossType);
            
            // Setup visual dựa trên loại boss
            SetupBossVisual(boss, bossType);
        }
        
        /// <summary>
        /// Setup boss phases với data array (shared method)
        /// </summary>
        private void SetupBossPhases(BaseBoss boss, string bossTypeName, BossPhaseData[] phaseData)
        {
            for (int i = 0; i < phaseData.Length && i < boss.phases.Length; i++)
            {
                var data = phaseData[i];
                boss.phases[i] = new BossPhase
                {
                    phaseName = $"{bossTypeName} - {data.phaseName}",
                    maxHealth = data.maxHealth,
                    moveSpeed = data.moveSpeed,
                    attackSpeed = data.attackSpeed,
                    damage = data.damage,
                    sightRange = data.sightRange,
                    phaseColor = data.phaseColor,
                    scale = data.scale,
                    canUseSpecialAbility = data.canUseSpecialAbility,
                    specialAbilityName = data.specialAbilityName,
                    specialAbilityCooldown = data.specialAbilityCooldown
                };
            }
        }

        /// <summary>
        /// Setup phases cho boss
        /// </summary>
        private void SetupBossPhases(BaseBoss boss, BossType bossType)
        {
            boss.phases = new BossPhase[3];

            switch (bossType)
            {
                case BossType.Melee:
                    SetupMeleeBossPhases(boss);
                    break;
                case BossType.Ranged:
                    SetupRangedBossPhases(boss);
                    break;
                case BossType.Magic:
                    SetupMagicBossPhases(boss);
                    break;
            }
        }

        /// <summary>
        /// Setup phases cho Melee Boss
        /// </summary>
        private void SetupMeleeBossPhases(BaseBoss boss)
        {
            SetupBossPhases(boss, "Melee", new BossPhaseData[]
            {
                new BossPhaseData("Giai đoạn 1", 100, 6f, 1.2f, 15, 12f, Color.red, Vector3.one, false, "", 0f),
                new BossPhaseData("Giai đoạn 2", 100, 8f, 1f, 20, 15f, new Color(1f, 0.5f, 0f), Vector3.one * 1.1f, true, "Rush Attack", 6f),
                new BossPhaseData("Giai đoạn 3", 100, 10f, 0.8f, 30, 18f, Color.magenta, Vector3.one * 1.2f, true, "Berserker Rage", 4f)
            });
        }

        /// <summary>
        /// Setup phases cho Ranged Boss
        /// </summary>
        private void SetupRangedBossPhases(BaseBoss boss)
        {
            SetupBossPhases(boss, "Ranged", new BossPhaseData[]
            {
                new BossPhaseData("Giai đoạn 1", 100, 4f, 1.5f, 12, 15f, Color.blue, Vector3.one, false, "", 0f),
                new BossPhaseData("Giai đoạn 2", 100, 5f, 1.2f, 18, 18f, Color.cyan, Vector3.one * 1.1f, true, "Triple Shot", 8f),
                new BossPhaseData("Giai đoạn 3", 100, 6f, 1f, 25, 20f, Color.yellow, Vector3.one * 1.2f, true, "Burst Fire", 6f)
            });
        }

        /// <summary>
        /// Setup phases cho Magic Boss
        /// </summary>
        private void SetupMagicBossPhases(BaseBoss boss)
        {
            SetupBossPhases(boss, "Magic", new BossPhaseData[]
            {
                new BossPhaseData("Giai đoạn 1", 100, 3f, 2f, 10, 20f, new Color(0.5f, 0f, 0.5f), Vector3.one, false, "", 0f),
                new BossPhaseData("Giai đoạn 2", 100, 4f, 1.5f, 15, 25f, Color.magenta, Vector3.one * 1.1f, true, "Multi Spell", 10f),
                new BossPhaseData("Giai đoạn 3", 100, 5f, 1.2f, 20, 30f, Color.white, Vector3.one * 1.2f, true, "Ultimate Spell", 8f)
            });
        }

        /// <summary>
        /// Setup stats cho boss
        /// </summary>
        private void SetupBossStats(BaseBoss boss, BossType bossType)
        {
            // Có thể thêm logic setup stats khác nhau cho từng loại boss
            // Ví dụ: Melee boss có nhiều máu hơn, Ranged boss có tầm xa hơn, etc.
        }

        /// <summary>
        /// Setup visual cho boss
        /// </summary>
        private void SetupBossVisual(BaseBoss boss, BossType bossType)
        {
            // Có thể thêm logic setup visual khác nhau
            // Ví dụ: Thêm particle effects, thay đổi materials, etc.
        }

        /// <summary>
        /// Destroy boss hiện tại
        /// </summary>
        public void DestroyCurrentBoss()
        {
            if (m_currentBoss != null)
            {
                Destroy(m_currentBoss.gameObject);
                m_currentBoss = null;
            }
        }

        /// <summary>
        /// Reset boss về trạng thái ban đầu
        /// </summary>
        public void ResetBoss()
        {
            if (m_currentBoss != null)
                m_currentBoss.ResetBoss();
        }
    }

    /// <summary>
    /// Enum các loại boss
    /// </summary>
    public enum BossType
    {
        Melee,   // Boss cận chiến
        Ranged,  // Boss tầm xa
        Magic    // Boss phép thuật
    }
}
