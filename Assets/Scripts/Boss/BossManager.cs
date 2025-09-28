using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Manager")]
    public class BossManager : SingletonMonobehaviour<BossManager>
    {
        [Header("Boss Settings")]
        [Tooltip("Boss hiện tại trong scene")]
        [SerializeField] private BaseBoss currentBoss;
        
        [Tooltip("Có tự động tìm boss trong scene không")]
        [SerializeField] private bool autoFindBoss = true;
        
        [Tooltip("Thời gian delay trước khi bắt đầu boss fight")]
        [SerializeField] private float bossFightStartDelay = 2f;

        [Header("Boss Events")]
        [Tooltip("Được gọi khi boss fight bắt đầu")]
        [SerializeField] private UnityEvent OnBossFightStart;
        
        [Tooltip("Được gọi khi boss fight kết thúc")]
        [SerializeField] private UnityEvent OnBossFightEnd;
        
        [Tooltip("Được gọi khi boss chuyển giai đoạn")]
        [SerializeField] private UnityEvent<int> OnBossPhaseChanged;
        
        [Tooltip("Được gọi khi boss bị đánh bại hoàn toàn")]
        [SerializeField] private UnityEvent OnBossDefeated;

        private bool m_bossFightActive = false;
        private bool m_bossDefeated = false;

        /// <summary>
        /// Boss fight có đang diễn ra không
        /// </summary>
        public bool isBossFightActive => m_bossFightActive;

        /// <summary>
        /// Boss đã bị đánh bại chưa
        /// </summary>
        public bool isBossDefeated => m_bossDefeated;

        protected override void Awake()
        {
            base.Awake();
            InitializeBoss();
        }

        private void Start()
        {
            if (autoFindBoss && currentBoss == null)
            {
                FindBossInScene();
            }

            if (currentBoss != null)
            {
                SetupBossEvents();
                StartBossFight();
            }
        }

        /// <summary>
        /// Khởi tạo boss
        /// </summary>
        private void InitializeBoss()
        {
            if (currentBoss == null && autoFindBoss)
            {
                FindBossInScene();
            }
        }

        /// <summary>
        /// Tìm boss trong scene
        /// </summary>
        public void FindBossInScene()
        {
            currentBoss = BossUtils.FindBoss();
            
            if (currentBoss == null)
            {
                Debug.LogWarning("BossManager: Không tìm thấy Boss trong scene!");
            }
            else
            {
                Debug.Log("BossManager: Đã tìm thấy Boss trong scene!");
            }
        }

        /// <summary>
        /// Thiết lập các sự kiện boss
        /// </summary>
        private void SetupBossEvents()
        {
            BossUtils.SetupBossEvents(currentBoss, 
                onPhaseStart: OnBossPhaseStart,
                onPhaseChanged: OnBossPhaseChangedHandler,
                onBossDefeated: OnBossDefeatedHandler);
        }

        /// <summary>
        /// Bắt đầu boss fight
        /// </summary>
        public void StartBossFight()
        {
            if (currentBoss == null)
            {
                Debug.LogWarning("BossManager: Không có boss để bắt đầu fight!");
                return;
            }

            if (m_bossFightActive) return;

            Debug.Log("BossManager: Bắt đầu Boss Fight!");
            
            Invoke(nameof(ActivateBossFight), bossFightStartDelay);
        }

        /// <summary>
        /// Kích hoạt boss fight
        /// </summary>
        private void ActivateBossFight()
        {
            m_bossFightActive = true;
            m_bossDefeated = false;
            
            OnBossFightStart?.Invoke();
            
            Debug.Log("BossManager: Boss Fight đã được kích hoạt!");
        }

        /// <summary>
        /// Kết thúc boss fight
        /// </summary>
        public void EndBossFight()
        {
            if (!m_bossFightActive) return;

            m_bossFightActive = false;
            OnBossFightEnd?.Invoke();
            
            Debug.Log("BossManager: Boss Fight đã kết thúc!");
        }

        /// <summary>
        /// Reset boss về trạng thái ban đầu
        /// </summary>
        public void ResetBoss()
        {
            if (currentBoss == null) return;

            currentBoss.ResetBoss();
            m_bossFightActive = false;
            m_bossDefeated = false;
            
            Debug.Log("BossManager: Boss đã được reset!");
        }

        /// <summary>
        /// Lấy thông tin boss hiện tại
        /// </summary>
        public BossInfo GetBossInfo()
        {
            if (currentBoss == null) return null;

            return new BossInfo
            {
                currentHealth = currentBoss.bossHealth.currentHealth,
                maxHealth = currentBoss.bossHealth.initialHealth,
                currentPhase = currentBoss.bossHealth.currentPhase,
                phaseName = currentBoss.currentPhase?.phaseName ?? "Unknown",
                bossType = currentBoss.GetType().Name,
                isDead = currentBoss.bossHealth.isDead,
                isTransitioning = currentBoss.bossHealth.isTransitioning
            };
        }

        // Event Handlers
        private void OnBossPhaseStart(int phase)
        {
            Debug.Log($"BossManager: Boss bắt đầu giai đoạn {phase + 1}");
            OnBossPhaseChanged?.Invoke(phase);
        }

        private void OnBossPhaseChangedHandler(int newPhase)
        {
            Debug.Log($"BossManager: Boss chuyển giai đoạn {newPhase + 1}");
            OnBossPhaseChanged?.Invoke(newPhase);
        }

        private void OnBossDefeatedHandler()
        {
            m_bossDefeated = true;
            m_bossFightActive = false;
            
            OnBossDefeated?.Invoke();
            OnBossFightEnd?.Invoke();
            
            Debug.Log("BossManager: Boss đã bị đánh bại hoàn toàn!");
        }

        private void OnDestroy()
        {
            // Cleanup events
            if (currentBoss != null)
            {
                currentBoss.OnBossPhaseStartEvent.RemoveListener(OnBossPhaseStart);
                currentBoss.bossHealth.OnPhaseChanged.RemoveListener(OnBossPhaseChangedHandler);
                currentBoss.bossHealth.OnBossDefeated.RemoveListener(OnBossDefeatedHandler);
            }
        }
    }

    /// <summary>
    /// Cấu trúc thông tin boss
    /// </summary>
    [System.Serializable]
    public class BossInfo
    {
        public int currentHealth;
        public int maxHealth;
        public int currentPhase;
        public string phaseName;
        public string bossType;
        public bool isDead;
        public bool isTransitioning;
    }
}
