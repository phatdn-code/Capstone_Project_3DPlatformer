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
    public class SoldierRobot : BaseBoss
    {
        //─────────────────────────────────────────────
        #region === CÀI ĐẶT BOM ===
        [field: Header("Bomb Settings")]
        [field: SerializeField] public BossBomb BombPrefab { get; private set; }
        [field: SerializeField] public Transform RightHandSpawnPoint { get; private set; }
        [field: SerializeField] public Transform LeftHandSpawnPoint { get; private set; }
        #endregion

        //─────────────────────────────────────────────
        #region === CÀI ĐẶT CẦU LỬA ===
        [field: Header("Fireball Settings")]
        [field: SerializeField] public BossFireball FireballPrefab { get; private set; }
        [field: SerializeField] public Transform FireballSpawnPoint { get; private set; }
        #endregion

        //─────────────────────────────────────────────
        #region === CÀI ĐẶT CẬN CHIẾN ===
        [Header("Melee Attack Settings")]
        [SerializeField] private float meleeRange = 3f;       // Phạm vi tấn công cận chiến
        [SerializeField] private float meleeCooldown = 2f;    // Thời gian hồi chiêu
        [SerializeField] private int meleeDamage = 10;        // Sát thương gây ra
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
        #region === ANIMATION & HIỆU ỨNG ===
        [field: Header("Animation")]
        [field: SerializeField] public Animator SkinAnimator { get; private set; }

        [field: Header("Effects")]
        [field: SerializeField] public GameObject[] flashBombEffects { get; private set; } // [0] = tay trái, [1] = tay phải
        [field: SerializeField] public GameObject flashFireballEffect { get; private set; }
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
        private bool m_isInAttackSequence;
        private bool m_isMoving;
        private float m_originalSpeed;
        private bool isMeleeAttacking;
        private float nextMeleeTime;
        #endregion

        //─────────────────────────────────────────────
        #region === VÒNG ĐỜI UNITY ===

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

        /// <summary>Khởi tạo NavMeshAgent và lưu tốc độ ban đầu.</summary>
        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            if (agent != null)
                m_originalSpeed = agent.speed;
        }

        /// <summary>Tìm Player trong scene nếu chưa được gán.</summary>
        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<Player>();
        }

        /// <summary>Tắt tất cả hiệu ứng flash ngay khi bắt đầu game.</summary>
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

        /// <summary>Thực hiện chuỗi hành động tấn công cận chiến.</summary>
        private IEnumerator PerformMeleeAttack()
        {
            isMeleeAttacking = true;
            nextMeleeTime = Time.time + meleeCooldown;

            if (agent != null)
                agent.isStopped = true;

            yield return RotateTowardsPlayer(() =>
            {
                SkinAnimator?.SetTrigger("MeleeAttack");
            });

            yield return new WaitForSeconds(1f);

            if (agent != null)
                agent.isStopped = false;

            isMeleeAttacking = false;
        }

        /// <summary>Gây sát thương khi animation đánh trúng player.</summary>
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
        #region === TRÌNH TỰ TẤN CÔNG (AI LOOP) ===

        /// <summary>Bắt đầu vòng tấn công (nếu chưa bắt đầu).</summary>
        private void StartAttackSequence()
        {
            if (m_isInAttackSequence) return;

            m_isInAttackSequence = true;
            StartCoroutine(ExecuteAttackSequence());
        }

        /// <summary>Chuỗi tấn công: ném bom → bắn cầu lửa → di chuyển.</summary>
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
        #region === TẤN CÔNG TẦM XA ===

        /// <summary>Chuẩn bị hướng ném bom (tay trái hoặc tay phải).</summary>
        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving || isMeleeAttacking) return;

            Transform spawnPoint = useRightHand ? RightHandSpawnPoint : LeftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() =>
            {
                SkinAnimator?.SetTrigger(useRightHand ? "RightHandShoot" : "LeftHandShoot");
            }));
        }

        /// <summary>Được gọi từ Animation Event để tạo quả bom thật.</summary>
        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving) return;

            Transform spawnPoint = useRightHand ? RightHandSpawnPoint : LeftHandSpawnPoint;
            if (spawnPoint == null || BombPrefab == null) return;

            int index = useRightHand ? 1 : 0;
            if (flashBombEffects.Length > index && flashBombEffects[index] != null)
                flashBombEffects[index].SetActive(true);

            BossBomb bomb = PoolManager.Instance.ReuseComponent(
                BombPrefab.gameObject, spawnPoint.position, BombPrefab.transform.rotation)?.GetComponent<BossBomb>();

            if (bomb != null && player != null)
                bomb.SetupFromPool(player);
        }

        /// <summary>Chuẩn bị hướng bắn cầu lửa.</summary>
        private void ShootFireball()
        {
            if (m_isMoving || isMeleeAttacking) return;

            StartCoroutine(RotateTowardsPlayer(() => SkinAnimator?.SetTrigger("FireballShoot")));
        }

        /// <summary>Được gọi từ Animation Event để tạo cầu lửa thật.</summary>
        public void CreateFireballFromAnimation()
        {
            if (m_isMoving || FireballPrefab == null || FireballSpawnPoint == null) return;

            flashFireballEffect.SetActive(true);

            DOVirtual.DelayedCall(0.15f, () =>
            {
                if (flashFireballEffect != null)
                    flashFireballEffect.SetActive(false);
            });

            BossFireball fireball = PoolManager.Instance.ReuseComponent(
                FireballPrefab.gameObject, FireballSpawnPoint.position, FireballSpawnPoint.rotation)
                ?.GetComponent<BossFireball>();

            if (fireball != null && player != null)
                fireball.SetupFromPool(player);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === DI CHUYỂN ===

        /// <summary>Boss di chuyển đến vị trí ngẫu nhiên trong phạm vi cho phép.</summary>
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
            if (agent != null)
                agent.speed = m_originalSpeed;

            yield return new WaitForSeconds(movementRestTime);
            m_isMoving = false;
        }

        /// <summary>Lấy một vị trí hợp lệ ngẫu nhiên trong bán kính di chuyển.</summary>
        private Vector3 GetNewPosition()
        {
            Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + center;

            return NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, NavMesh.AllAreas)
                ? hit.position
                : transform.position;
        }

        /// <summary>Xoay boss hướng về phía player trước khi hành động.</summary>
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
    }
}
