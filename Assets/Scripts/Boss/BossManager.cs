using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Quản lý vòng đời trận Boss Fight:
    /// - Khởi động, kết thúc trận
    /// - Bắt sự kiện thay đổi giai đoạn, boss bị đánh bại
    /// - Phát tín hiệu event ra UI / gameplay khác
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Boss Manager")]
    public class BossManager : SingletonMonobehaviour<BossManager>
    {
        #region === Inspector Fields ===

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

        [Tooltip("Được gọi khi boss chuyển giai đoạn (phase change)")]
        [SerializeField] private UnityEvent<int> OnBossPhaseChanged;

        [Tooltip("Được gọi khi boss bị đánh bại hoàn toàn")]
        [SerializeField] private UnityEvent OnBossDefeated;

        #endregion

        #region === Runtime State ===

        private bool m_bossFightActive = false;   // Trận boss có đang diễn ra không
        private bool m_bossDefeated = false;      // Boss đã bị hạ gục hoàn toàn chưa

        #endregion

        #region === Properties ===

        /// <summary>
        /// Kiểm tra trận boss có đang diễn ra không
        /// </summary>
        public bool isBossFightActive => m_bossFightActive;

        /// <summary>
        /// Kiểm tra boss đã bị đánh bại chưa
        /// </summary>
        public bool isBossDefeated => m_bossDefeated;

        /// <summary>
        /// Trả về boss hiện tại
        /// </summary>
        public BaseBoss boss => currentBoss;

        #endregion

        #region === Unity Lifecycle ===

        private void Start()
        {
            // Nếu bật autoFindBoss và chưa gán, tự động tìm boss trong scene
            if (autoFindBoss && currentBoss == null)
                currentBoss = FindFirstObjectByType<BaseBoss>();

            // Nếu có boss → đăng ký sự kiện
            if (currentBoss != null)
                RegisterBossEvents(currentBoss);
        }

        private void OnDestroy()
        {
            // Gỡ đăng ký sự kiện khi Manager bị hủy
            if (currentBoss != null)
                UnregisterBossEvents(currentBoss);
        }

        #endregion

        #region === Event Registration ===

        /// <summary>
        /// Đăng ký các sự kiện từ Boss (phase change, defeated)
        /// </summary>
        private void RegisterBossEvents(BaseBoss boss)
        {
            boss.OnBossPhaseStartEvent.AddListener(OnPhaseChangedHandler);
            boss.bossHealth.OnBossDefeated.AddListener(OnBossDefeatedHandler);
        }

        /// <summary>
        /// Hủy đăng ký sự kiện từ Boss
        /// </summary>
        private void UnregisterBossEvents(BaseBoss boss)
        {
            boss.OnBossPhaseStartEvent.RemoveListener(OnPhaseChangedHandler);
            boss.bossHealth.OnBossDefeated.RemoveListener(OnBossDefeatedHandler);
        }

        #endregion

        #region === Boss Fight Control ===

        /// <summary>
        /// Bắt đầu trận boss fight (nếu có boss và chưa bắt đầu)
        /// </summary>
        public void StartBossFight()
        {
            if (currentBoss == null || m_bossFightActive) return;
            StartCoroutine(StartBossFightRoutine());
        }

        /// <summary>
        /// Coroutine thực hiện delay trước khi bắt đầu boss fight
        /// </summary>
        private System.Collections.IEnumerator StartBossFightRoutine()
        {
            yield return new WaitForSeconds(bossFightStartDelay);
            m_bossFightActive = true;
            OnBossFightStart?.Invoke();
        }

        /// <summary>
        /// Kết thúc boss fight
        /// </summary>
        public void EndBossFight()
        {
            if (!m_bossFightActive) return;
            m_bossFightActive = false;
            OnBossFightEnd?.Invoke();
        }

        #endregion

        #region === Event Handlers ===

        /// <summary>
        /// Khi boss chuyển sang giai đoạn mới
        /// </summary>
        private void OnPhaseChangedHandler(int newPhase)
        {
            OnBossPhaseChanged?.Invoke(newPhase);
        }

        /// <summary>
        /// Khi boss bị đánh bại hoàn toàn
        /// </summary>
        private void OnBossDefeatedHandler()
        {
            m_bossDefeated = true;
            EndBossFight();
            OnBossDefeated?.Invoke();
        }

        #endregion
    }
}
