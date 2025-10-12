using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Boss SoldierRobot: điều khiển logic di chuyển, tấn công (bom, cầu lửa),
    /// và trình tự hành vi (attack sequence).
    /// </summary>
    public class SoldierRobot : BaseBoss
    {
        // ─────────────────────────────────────────────
        // Bomb Settings
        [field: Header("Bomb Settings")]
        [field: SerializeField] public BossBomb BombPrefab { get; private set; }
        [field: SerializeField] public Transform RightHandSpawnPoint { get; private set; }
        [field: SerializeField] public Transform LeftHandSpawnPoint { get; private set; }
        [field: SerializeField] public float BombThrowForce { get; private set; } = 10f;
        [field: SerializeField] public float BombDamage { get; private set; } = 50f;
        [field: SerializeField] public float BombFuseTime { get; private set; } = 3f;
        [field: SerializeField] public float BombExplosionRadius { get; private set; } = 5f;
        [field: SerializeField] public float BombExplosionForce { get; private set; } = 15f;

        // ─────────────────────────────────────────────
        // Fireball Settings
        [field: Header("Fireball Settings")]
        [field: SerializeField] public BossFireball FireballPrefab { get; private set; }
        [field: SerializeField] public Transform FireballSpawnPoint { get; private set; }
        [field: SerializeField] public float FireballDamage { get; private set; } = 30f;
        [field: SerializeField] public float FireballSpeed { get; private set; } = 8f;
        [field: SerializeField] public float FireballLifetime { get; private set; } = 5f;


        // ─────────────────────────────────────────────
        // Movement Settings
        [Header("Movement Settings")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float movementRestTime = 2f;
        [SerializeField] private float movementSpeedMultiplier = 1.5f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float rotationThreshold = 5f;

        // ─────────────────────────────────────────────
        // Animation & Effects
        [field: Header("Animation")]
        [field: SerializeField] public Animator SkinAnimator { get; private set; }

        [field: Header("Effects")]
        [field: SerializeField] public GameObject BombExplosionEffect { get; private set; }
        [field: SerializeField] public GameObject FireballEffect { get; private set; }

        // ─────────────────────────────────────────────
        // Player
        [Header("Player")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        // ─────────────────────────────────────────────
        // Components & State
        private NavMeshAgent agent;
        private bool m_isInAttackSequence;
        private int m_currentStep;
        private bool m_isMoving;
        private float m_originalSpeed;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        /// <summary>
        /// Unity lifecycle: khởi tạo boss khi scene bắt đầu.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
            StartAttackSequence();
        }

        /// <summary>
        /// Khởi tạo các component quan trọng (NavMeshAgent, tốc độ gốc).
        /// </summary>
        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();

            if (agent == null)
                Debug.LogError("❌ NavMeshAgent not found!");

            else m_originalSpeed = agent.speed;
        }

        /// <summary>
        /// Tìm player trong scene (nếu chưa gán thủ công).
        /// </summary>
        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<Player>();
        }

        // ─────────────────────────────────────────────
        // Attack Sequence
        /// <summary>
        /// Bắt đầu trình tự tấn công (chạy liên tục).
        /// </summary>
        private void StartAttackSequence()
        {
            if (m_isInAttackSequence) return;

            m_isInAttackSequence = true;
            m_currentStep = 0;
            StartCoroutine(ExecuteAttackSequence());
        }

        /// <summary>
        /// Trình tự hành vi: bắn bom phải → bom trái → cầu lửa → di chuyển.
        /// </summary>
        private IEnumerator ExecuteAttackSequence()
        {
            // Step 1: Bomb right
            ShootBomb(true);
            yield return new WaitForSeconds(1f);

            // Step 2: Bomb left
            ShootBomb(false);
            yield return new WaitForSeconds(3f);

            // Step 3: Fireball (1–2 lần)
            int fireballCount = Random.Range(1, 3);

            for (int i = 0; i < fireballCount; i++)
            {
                ShootFireball();
                if (i < fireballCount - 1)
                    yield return new WaitForSeconds(2f);
            }

            yield return new WaitForSeconds(5f);

            // Step 4: Move
            yield return StartCoroutine(MoveToNewPosition());

            m_isInAttackSequence = false;
            StartAttackSequence();
        }

        // ─────────────────────────────────────────────
        // Combat
        /// <summary>
        /// Gọi animation ném bom (tay phải hoặc trái).
        /// </summary>
        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving) return;
            Transform spawnPoint = useRightHand ? RightHandSpawnPoint : LeftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() =>
            {
                SkinAnimator?.SetTrigger(useRightHand ? "RightHandShoot" : "LeftHandShoot");
            }));
        }

        /// <summary>
        /// Gọi animation bắn cầu lửa.
        /// </summary>
        private void ShootFireball()
        {
            if (m_isMoving) return;
            StartCoroutine(RotateTowardsPlayer(() => SkinAnimator?.SetTrigger("FireballShoot")));
        }

        /// <summary>
        /// Được gọi từ animation event để tạo bomb thực sự.
        /// </summary>
        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving) return;
            Transform spawnPoint = useRightHand ? RightHandSpawnPoint : LeftHandSpawnPoint;
            if (spawnPoint == null || BombPrefab == null) return;

            BossBomb bomb = PoolManager.Instance.ReuseComponent(
                BombPrefab.gameObject, spawnPoint.position, BombPrefab.transform.rotation)?.GetComponent<BossBomb>();

            if (bomb != null)
                SetupBomb(bomb);

            SetupBomb(bomb);
        }

        /// <summary>
        /// Được gọi từ animation event để tạo fireball thực sự.
        /// </summary>
        public void CreateFireballFromAnimation()
        {
            if (m_isMoving || FireballPrefab == null || FireballSpawnPoint == null) return;

            BossFireball fireball = PoolManager.Instance.ReuseComponent(
                FireballPrefab.gameObject, FireballSpawnPoint.position, FireballSpawnPoint.rotation)
                ?.GetComponent<BossFireball>();

            SetupFireball(fireball);
        }

        /// <summary>
        /// Setup thông số cho bomb vừa spawn.
        /// </summary>
        private void SetupBomb(BossBomb bomb)
        {
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<Player>();

            if (player != null)
                bomb.SetupFromPool(player, BombThrowForce, (int)BombDamage,
                                   BombFuseTime, BombExplosionRadius, BombExplosionForce);
        }

        /// <summary>
        /// Setup thông số cho fireball vừa spawn.
        /// </summary>
        private void SetupFireball(BossFireball fireball)
        {
            fireball.SetupFromPool((int)FireballDamage, FireballSpeed, FireballLifetime);
        }

        // ─────────────────────────────────────────────
        // Movement & Rotation
        /// <summary>
        /// Di chuyển boss đến vị trí ngẫu nhiên trong bán kính wanderRadius.
        /// </summary>
        private IEnumerator MoveToNewPosition()
        {
            if (m_isMoving) yield break;
            m_isMoving = true;

            if (agent != null)
                agent.speed = m_originalSpeed * movementSpeedMultiplier;

            Vector3 newPosition = GetNewPosition();
            agent.SetDestination(newPosition);

            SkinAnimator?.SetBool("isMoving", true);

            while (agent.pathPending || agent.remainingDistance > 0.2f)
                yield return null;

            SkinAnimator?.SetBool("isMoving", false);
            if (agent != null) agent.speed = m_originalSpeed;

            yield return new WaitForSeconds(movementRestTime);
            m_isMoving = false;
        }

        /// <summary>
        /// Lấy vị trí ngẫu nhiên trong bán kính wanderRadius (dựa trên NavMesh).
        /// </summary>
        private Vector3 GetNewPosition()
        {
            Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + center;

            return NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, NavMesh.AllAreas)
                ? hit.position
                : transform.position;
        }

        /// <summary>
        /// Xoay boss hướng về phía player, sau đó gọi onComplete.
        /// </summary>
        private IEnumerator RotateTowardsPlayer(System.Action onComplete = null)
        {
            if (player == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            Vector3 direction = (player.transform.position - transform.position).normalized;
            direction.y = 0;

            if (direction.magnitude < 0.1f)
            {
                onComplete?.Invoke();
                yield break;
            }

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            float angleDifference = Quaternion.Angle(transform.rotation, targetRotation);

            if (angleDifference > rotationThreshold)
            {
                float rotationTime = angleDifference / (rotationSpeed * 90f);
                Quaternion startRotation = transform.rotation;
                float elapsedTime = 0f;

                while (elapsedTime < rotationTime)
                {
                    elapsedTime += Time.deltaTime;
                    float progress = elapsedTime / rotationTime;
                    transform.rotation = Quaternion.Slerp(startRotation, targetRotation, progress);
                    yield return null;
                }
                transform.rotation = targetRotation;
            }

            onComplete?.Invoke();
        }

        // ─────────────────────────────────────────────
        // Animation Helper
        /// <summary>
        /// Trigger animation theo tên (chỉ khi không di chuyển).
        /// </summary>
        public void TriggerAnimation(string triggerName)
        {
            if (!m_isMoving) SkinAnimator?.SetTrigger(triggerName);
        }

        // ─────────────────────────────────────────────
        // Debug
        /// <summary>
        /// Trả về trạng thái hiện tại của attack sequence.
        /// </summary>
        public string GetAttackStateInfo()
        {
            return $"AttackSequence: {m_isInAttackSequence}, Step: {m_currentStep}, Moving: {m_isMoving}";
        }
    }
}
