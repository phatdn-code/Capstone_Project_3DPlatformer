using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Factory để tạo Soldier Robot Boss
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Soldier Robot Factory")]
    public class SoldierRobotFactory : SingletonMonobehaviour<SoldierRobotFactory>
    {
        [Header("Soldier Robot Prefabs")]
        [Tooltip("Prefab Soldier Robot")]
        [SerializeField] private GameObject soldierRobotPrefab;
        
        [Tooltip("Prefab bom")]
        [SerializeField] private GameObject bombPrefab;
        
        [Tooltip("Prefab cầu lửa")]
        [SerializeField] private GameObject fireballPrefab;
        
        [Tooltip("Prefab hiệu ứng nổ bom")]
        [SerializeField] private GameObject bombExplosionEffect;
        
        [Tooltip("Prefab hiệu ứng cầu lửa")]
        [SerializeField] private GameObject fireballEffect;

        [Header("Spawn Settings")]
        [Tooltip("Vị trí spawn boss")]
        [SerializeField] private Transform spawnPoint;
        
        [Tooltip("Tự động spawn boss khi start")]
        [SerializeField] private bool autoSpawnOnStart = true;

        [Header("Animation Settings")]
        [Tooltip("Animator Controller cho Skin")]
        [SerializeField] private RuntimeAnimatorController skinAnimatorController;

        private SoldierRobot m_currentSoldierRobot;

        /// <summary>
        /// Soldier Robot hiện tại
        /// </summary>
        public SoldierRobot currentSoldierRobot => m_currentSoldierRobot;

        protected override void Awake()
        {
            base.Awake();
            if (autoSpawnOnStart)
            {
                SpawnSoldierRobot();
            }
        }

        /// <summary>
        /// Spawn Soldier Robot
        /// </summary>
        /// <returns>Soldier Robot được tạo</returns>
        public SoldierRobot SpawnSoldierRobot()
        {
            // Destroy boss cũ nếu có
            if (m_currentSoldierRobot != null)
            {
                Destroy(m_currentSoldierRobot.gameObject);
            }

            if (soldierRobotPrefab == null)
            {
                Debug.LogError("Soldier Robot Prefab chưa được gán!");
                return null;
            }

            // Tạo Soldier Robot
            Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : transform.position;
            GameObject soldierRobotObject = Instantiate(soldierRobotPrefab, spawnPosition, Quaternion.identity);
            
            // Lấy component SoldierRobot
            m_currentSoldierRobot = soldierRobotObject.GetComponent<SoldierRobot>();
            
            if (m_currentSoldierRobot == null)
            {
                Debug.LogError("Prefab không có component SoldierRobot!");
                Destroy(soldierRobotObject);
                return null;
            }

            // Setup Soldier Robot
            SetupSoldierRobot(m_currentSoldierRobot);

            Debug.Log("Đã spawn Soldier Robot Boss!");
            return m_currentSoldierRobot;
        }

        /// <summary>
        /// Setup Soldier Robot sau khi spawn
        /// </summary>
        private void SetupSoldierRobot(SoldierRobot soldierRobot)
        {
            // Setup phases
            SetupSoldierRobotPhases(soldierRobot);
            
            // Setup prefabs
            SetupSoldierRobotPrefabs(soldierRobot);
            
            // Setup spawn points
            SetupSoldierRobotSpawnPoints(soldierRobot);
            
            // Setup animation events
            SetupSoldierRobotAnimationEvents(soldierRobot);
            
            // Setup Skin Animator
            SetupSoldierRobotSkinAnimator(soldierRobot);
        }

        /// <summary>
        /// Setup phases cho Soldier Robot
        /// </summary>
        private void SetupSoldierRobotPhases(SoldierRobot soldierRobot)
        {
            soldierRobot.phases = new BossPhase[3];

            // Giai đoạn 1: Bom và Cầu lửa
            soldierRobot.phases[0] = new BossPhase
            {
                phaseName = "Soldier Robot - Phase 1",
                maxHealth = 100,
                moveSpeed = 3f,
                attackSpeed = 1f,
                damage = 20,
                sightRange = 15f,
                phaseColor = Color.red,
                scale = Vector3.one,
                canUseSpecialAbility = false
            };

            // Giai đoạn 2: Bom nhanh hơn + Kỹ năng mới
            soldierRobot.phases[1] = new BossPhase
            {
                phaseName = "Soldier Robot - Phase 2",
                maxHealth = 100,
                moveSpeed = 4f,
                attackSpeed = 0.8f,
                damage = 25,
                sightRange = 18f,
                phaseColor = new Color(1f, 0.5f, 0f), // Orange color
                scale = Vector3.one * 1.1f,
                canUseSpecialAbility = true,
                specialAbilityName = "Rapid Fire",
                specialAbilityCooldown = 8f
            };

            // Giai đoạn 3: Tất cả kỹ năng + Kỹ năng mạnh nhất
            soldierRobot.phases[2] = new BossPhase
            {
                phaseName = "Soldier Robot - Phase 3",
                maxHealth = 100,
                moveSpeed = 5f,
                attackSpeed = 0.6f,
                damage = 30,
                sightRange = 20f,
                phaseColor = Color.magenta,
                scale = Vector3.one * 1.2f,
                canUseSpecialAbility = true,
                specialAbilityName = "Ultimate Assault",
                specialAbilityCooldown = 6f
            };
        }

        /// <summary>
        /// Setup prefabs cho Soldier Robot
        /// </summary>
        private void SetupSoldierRobotPrefabs(SoldierRobot soldierRobot)
        {
            if (bombPrefab != null)
                soldierRobot.SetBombPrefab(bombPrefab);
            
            if (fireballPrefab != null)
                soldierRobot.SetFireballPrefab(fireballPrefab);
            
            if (bombExplosionEffect != null)
                soldierRobot.SetBombExplosionEffect(bombExplosionEffect);
            
            if (fireballEffect != null)
                soldierRobot.SetFireballEffect(fireballEffect);
        }

        /// <summary>
        /// Setup spawn points cho Soldier Robot
        /// </summary>
        private void SetupSoldierRobotSpawnPoints(SoldierRobot soldierRobot)
        {
            // Tìm hoặc tạo spawn points
            Transform rightHand = FindChildTransform(soldierRobot.transform, "RightHandSpawn");
            Transform leftHand = FindChildTransform(soldierRobot.transform, "LeftHandSpawn");
            Transform fireballSpawn = FindChildTransform(soldierRobot.transform, "FireballSpawn");

            if (rightHand != null)
                soldierRobot.SetRightHandSpawnPoint(rightHand);
            
            if (leftHand != null)
                soldierRobot.SetLeftHandSpawnPoint(leftHand);
            
            if (fireballSpawn != null)
                soldierRobot.SetFireballSpawnPoint(fireballSpawn);
        }

        /// <summary>
        /// Setup animation events cho Soldier Robot
        /// </summary>
        private void SetupSoldierRobotAnimationEvents(SoldierRobot soldierRobot)
        {
            // Thêm component animation events nếu chưa có
            var animationEvents = soldierRobot.GetComponent<SoldierRobotAnimationEvents>();
            if (animationEvents == null)
            {
                animationEvents = soldierRobot.gameObject.AddComponent<SoldierRobotAnimationEvents>();
            }

            // Kết nối với SoldierRobot
            animationEvents.SetSoldierRobot(soldierRobot);
        }

        /// <summary>
        /// Setup Skin Animator cho Soldier Robot
        /// </summary>
        private void SetupSoldierRobotSkinAnimator(SoldierRobot soldierRobot)
        {
            // Tìm Animator trong Skin/Soldier Robot
            Transform skinTransform = FindChildTransform(soldierRobot.transform, "Skin");
            if (skinTransform != null)
            {
                Transform soldierRobotTransform = FindChildTransform(skinTransform, "Soldier Robot");
                if (soldierRobotTransform != null)
                {
                    Animator skinAnimator = soldierRobotTransform.GetComponent<Animator>();
                    if (skinAnimator == null)
                    {
                        skinAnimator = soldierRobotTransform.gameObject.AddComponent<Animator>();
                    }
                    
                    // Gán Animator Controller
                    if (skinAnimatorController != null)
                    {
                        skinAnimator.runtimeAnimatorController = skinAnimatorController;
                        soldierRobot.SetSkinAnimator(skinAnimator);
                        soldierRobot.SetAnimatorController(skinAnimatorController);
                    }
                }
            }
        }

        /// <summary>
        /// Tìm child transform theo tên
        /// </summary>
        private Transform FindChildTransform(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                
                Transform found = FindChildTransform(child, name);
                if (found != null)
                    return found;
            }
            return null;
        }

        /// <summary>
        /// Destroy Soldier Robot hiện tại
        /// </summary>
        public void DestroyCurrentSoldierRobot()
        {
            if (m_currentSoldierRobot != null)
            {
                Destroy(m_currentSoldierRobot.gameObject);
                m_currentSoldierRobot = null;
            }
        }

        /// <summary>
        /// Reset Soldier Robot về trạng thái ban đầu
        /// </summary>
        public void ResetSoldierRobot()
        {
            if (m_currentSoldierRobot != null)
            {
                m_currentSoldierRobot.ResetBoss();
            }
        }

        /// <summary>
        /// Lấy thông tin Soldier Robot hiện tại
        /// </summary>
        public string GetSoldierRobotInfo()
        {
            if (m_currentSoldierRobot == null)
                return "Không có Soldier Robot nào đang hoạt động";

            return $"Soldier Robot - Phase: {m_currentSoldierRobot.bossHealth.currentPhase + 1}, " +
                   $"Health: {m_currentSoldierRobot.bossHealth.currentHealth}/{m_currentSoldierRobot.bossHealth.initialHealth}, " +
                   $"Attack State: {m_currentSoldierRobot.GetAttackStateInfo()}";
        }
    }
}
