using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Điều khiển hành vi của boss SoldierRobot:
    /// - Tấn công cận chiến khi ở gần.
    /// - Ném bom (tay trái/phải).
    /// - Bắn cầu lửa.
    /// - Di chuyển ngẫu nhiên sau mỗi chu kỳ tấn công.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoldierRobot : BossCore
    {
        //─────────────────────────────────────────────
        #region === CÀI ĐẶT BOM ===
        [Header("Bomb Settings")]
        [SerializeField] private BossBomb bombPrefab;
        [SerializeField] private Transform rightHandSpawnPoint;
        [SerializeField] private Transform leftHandSpawnPoint;
        #endregion

        //─────────────────────────────────────────────
        #region === CÀI ĐẶT CẦU LỬA ===
        [Header("Fireball Settings")]
        [SerializeField] private BossFireball fireballPrefab;
        [SerializeField] private Transform fireballSpawnPoint;
        #endregion

        //─────────────────────────────────────────────
        #region === CẬN CHIẾN ===
        [Header("Melee Attack Settings")]
        [SerializeField] private float meleeRange = 3f;
        [SerializeField] private float meleeCooldown = 2f;
        [SerializeField] private int meleeDamage = 10;
        #endregion

        //─────────────────────────────────────────────
        #region === DI CHUYỂN ===
        [Header("Movement Settings")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float movementRestTime = 2f;
        [SerializeField] private float movementSpeedMultiplier = 1.5f;

        [Header("Rotation Settings")]
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private float rotationThreshold = 5f;
        #endregion

        //─────────────────────────────────────────────
        #region === HIỆU ỨNG ===
        [Header("Effects")]
        [SerializeField] private GameObject[] flashBombEffects; // [0] = tay trái, [1] = tay phải
        [SerializeField] private GameObject flashFireballEffect;
        #endregion

        //─────────────────────────────────────────────
        #region === NGƯỜI CHƠI ===
        [Header("Player")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;
        #endregion

        //─────────────────────────────────────────────
        #region === BIẾN TRẠNG THÁI ===
        private NavMeshAgent agent;
        private BossAnimationBase anim;                
        private SoldierRobotAnimation soldierAnim;
        private bool m_isInAttackSequence;
        private bool m_isMoving;
        private float m_originalSpeed;
        private bool isMeleeAttacking;
        private float nextMeleeTime;
        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===
        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
            DisableAllEffects();
            StartAttackSequence();
        }

        protected override void Update()
        {
            base.Update();

            if (player == null) return;

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= meleeRange && Time.time >= nextMeleeTime && !isMeleeAttacking)
                StartCoroutine(PerformMeleeAttack());
        }
        #endregion

        //─────────────────────────────────────────────
        #region === KHỞI TẠO ===
        private void InitializeComponents()
        {
            anim = GetComponent<BossAnimationBase>();
            agent = GetComponent<NavMeshAgent>();
            soldierAnim = anim as SoldierRobotAnimation;

            if (agent != null)
                m_originalSpeed = agent.speed;
        }

        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<Player>();
        }

        private void DisableAllEffects()
        {
            if (flashBombEffects != null)
            {
                foreach (var fx in flashBombEffects)
                    if (fx != null) fx.SetActive(false);
            }

            if (flashFireballEffect != null)
                flashFireballEffect.SetActive(false);
        }
        #endregion

        //─────────────────────────────────────────────
        #region === CẬN CHIẾN ===
        private IEnumerator PerformMeleeAttack()
        {
            isMeleeAttacking = true;
            nextMeleeTime = Time.time + meleeCooldown;

            if (agent != null)
                agent.isStopped = true;

            yield return RotateTowardsPlayer(() => anim?.PlayMeleeAttack());
            yield return new WaitForSeconds(1f);

            if (agent != null)
                agent.isStopped = false;

            isMeleeAttacking = false;
        }

        public void ApplyMeleeDamageToPlayer()
        {
            if (player == null) return;

            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= meleeRange)
            {
                player.ApplyDamage(meleeDamage, transform.position);
                Debug.Log($"💥 Melee hit player for {meleeDamage} damage!");
            }
        }
        #endregion

        //─────────────────────────────────────────────
        #region === TRÌNH TỰ TẤN CÔNG ===
        private void StartAttackSequence()
        {
            if (m_isInAttackSequence) return;
            m_isInAttackSequence = true;
            StartCoroutine(ExecuteAttackSequence());
        }

        private IEnumerator ExecuteAttackSequence()
        {
            while (true)
            {
                if (!isMeleeAttacking)
                {
                    ShootBomb(true);
                    yield return new WaitForSeconds(1f);

                    ShootBomb(false);
                    yield return new WaitForSeconds(3f);

                    int fireballCount = Random.Range(1, 3);
                    for (int i = 0; i < fireballCount; i++)
                    {
                        ShootFireball();
                        if (i < fireballCount - 1)
                            yield return new WaitForSeconds(2f);
                    }
                }

                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(MoveToNewPosition());
            }
        }
        #endregion

        //─────────────────────────────────────────────
        #region === TẦM XA ===
        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving || isMeleeAttacking) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() =>
            {
                soldierAnim?.PlayShootBomb(useRightHand);
            }));
        }

        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null || bombPrefab == null) return;

            int index = useRightHand ? 1 : 0;
            if (flashBombEffects.Length > index && flashBombEffects[index] != null)
                flashBombEffects[index].SetActive(true);

            BossBomb bomb = PoolManager.Instance.ReuseComponent(
                bombPrefab.gameObject, spawnPoint.position, bombPrefab.transform.rotation)?.GetComponent<BossBomb>();

            if (bomb != null && player != null)
                bomb.SetupFromPool(player, this);
        }

        private void ShootFireball()
        {
            if (m_isMoving || isMeleeAttacking) return;
            StartCoroutine(RotateTowardsPlayer(() => soldierAnim?.PlayFireballShoot()));
        }

        public void CreateFireballFromAnimation()
        {
            if (m_isMoving || fireballPrefab == null || fireballSpawnPoint == null) return;

            flashFireballEffect.SetActive(true);
            DOVirtual.DelayedCall(0.15f, () =>
            {
                if (flashFireballEffect != null)
                    flashFireballEffect.SetActive(false);
            });

            BossFireball fireball = PoolManager.Instance.ReuseComponent(
                fireballPrefab.gameObject, fireballSpawnPoint.position, fireballSpawnPoint.rotation)
                ?.GetComponent<BossFireball>();

            if (fireball != null && player != null)
                fireball.SetupFromPool(player);
        }
        #endregion

        //─────────────────────────────────────────────
        #region === DI CHUYỂN ===
        private IEnumerator MoveToNewPosition()
        {
            if (m_isMoving) yield break;
            m_isMoving = true;

            if (agent != null)
                agent.speed = m_originalSpeed * movementSpeedMultiplier;

            Vector3 newPosition = GetNewPosition();
            agent.SetDestination(newPosition);
            anim?.SetMoving(true);

            while (agent.pathPending || agent.remainingDistance > 0.2f)
                yield return null;

            anim?.SetMoving(false);
            if (agent != null)
                agent.speed = m_originalSpeed;

            yield return new WaitForSeconds(movementRestTime);
            m_isMoving = false;
        }

        private Vector3 GetNewPosition()
        {
            Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + center;

            return NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, NavMesh.AllAreas)
                ? hit.position
                : transform.position;
        }

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
        #endregion

        //─────────────────────────────────────────────
        protected override void UpdateBossBehavior() { }
    }
}
