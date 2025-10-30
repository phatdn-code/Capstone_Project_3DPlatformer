using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Controls the behavior of the SoldierRobot boss:
    /// - Melee attacks when close to the player.
    /// - Throws bombs (left/right hand).
    /// - Shoots fireballs.
    /// - Wanders around between attack sequences.
    /// </summary>
    [DisallowMultipleComponent]
    public class SoldierRobot : BossCore
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Player Reference")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Bomb Settings")]
        [SerializeField] private BossBomb bombPrefab;
        [SerializeField] private Transform rightHandSpawnPoint;
        [SerializeField] private Transform leftHandSpawnPoint;

        [Header("Fireball Settings")]
        [SerializeField] private BossFireball fireballPrefab;
        [SerializeField] private Transform fireballSpawnPoint;

        [Header("Melee Attack Settings")]
        [SerializeField] private float meleeRange = 3f;
        [SerializeField] private float meleeCooldown = 2f;
        [SerializeField] private int meleeDamage = 10;

        [Header("Movement Settings")]
        [SerializeField] private Transform centerPoint;
        [SerializeField] private float wanderRadius = 10f;
        [SerializeField] private float movementRestTime = 2f;
        [SerializeField] private float movementSpeedMultiplier = 1.5f;

        [Header("Rotation Settings")]
        [SerializeField] private Transform model;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject[] flashBombEffects; // [0] = left, [1] = right
        [SerializeField] private GameObject flashFireballEffect;
        [SerializeField] private GameObject specialSkillEffect;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME VARIABLES ===

        private NavMeshAgent agent;
        private SoldierRobotAnimation soldierAnim;
        private Coroutine speedRoutine;

        private bool isPaused;
        private bool isMeleeAttacking;
        private bool m_isInAttackSequence;
        private bool m_isMoving;

        private bool unlockedPhase2Attack;
        private bool hasUsedPhase2Once;

        private float nextMeleeTime;
        private float m_originalSpeed;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        // SoldierRobot.cs
        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
            DisableAllEffects();

            OnBossPhaseStartEvent.AddListener(OnPhaseChanged);
        }

        protected override void OnBattleStarted()
        {
            // Được gọi từ BossCore.StartBattle()
            StartAttackSequence();
        }

        protected override void Update()
        {
            base.Update();
            if (isPaused || player == null) return;

            RotateTowardsMovementDirection();

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= meleeRange && Time.time >= nextMeleeTime && !isMeleeAttacking)
                StartCoroutine(PerformMeleeAttack());
        }

        #endregion

        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            soldierAnim = base.bossAnim as SoldierRobotAnimation;

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

        private void OnPhaseChanged(int phaseIndex)
        {
            if (phaseIndex == 1) unlockedPhase2Attack = true;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === PAUSE CONTROL ===

        /// <summary>Pause or resume all boss activity (used during phase transitions).</summary>
        public void SetPaused(bool pause)
        {
            isPaused = pause;

            if (agent != null)
                agent.isStopped = pause;

            if (bossAnim != null)
                bossAnim.SetMoving(false);
        }

        public bool IsPaused => isPaused;

        #endregion

        //─────────────────────────────────────────────
        #region === MELEE ATTACK ===

        private IEnumerator PerformMeleeAttack()
        {
            isMeleeAttacking = true;
            nextMeleeTime = Time.time + meleeCooldown;

            if (agent != null)
                agent.isStopped = true;

            yield return RotateTowardsPlayer(() => bossAnim?.PlayMeleeAttack());
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
        #region === ATTACK SEQUENCE ===

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
                if (isPaused)
                {
                    yield return null;
                    continue;
                }

                if (!isMeleeAttacking)
                {
                    // 🔹 Bước 1: ném bomb như cũ
                    ShootBomb(true);
                    yield return new WaitForSeconds(1f);

                    ShootBomb(false);
                    yield return new WaitForSeconds(3f);

                    // 🔹 Bước 2: nếu vừa sang phase 2 và chưa dùng chiêu mới lần nào → dùng ngay
                    if (unlockedPhase2Attack && !hasUsedPhase2Once)
                    {
                        yield return PerformMultipleSpecialSkills();
                        hasUsedPhase2Once = true;
                    }

                    else
                    {
                        // 🔹 Bước 3: nếu đã từng dùng, 60% khả năng dùng lại chiêu mới
                        bool usedSpecialSkill = false;

                        if (unlockedPhase2Attack && Random.value < 0.6f)
                        {
                            yield return PerformMultipleSpecialSkills();
                            usedSpecialSkill = true;
                        }

                        // 🔹 Bước 4: chỉ bắn fireball nếu KHÔNG dùng chiêu mới
                        if (!usedSpecialSkill)
                        {
                            int fireballCount = Random.Range(1, 3);
                            for (int i = 0; i < fireballCount; i++)
                            {
                                ShootFireball();
                                if (i < fireballCount - 1)
                                    yield return new WaitForSeconds(2f);
                            }
                        }
                    }
                }

                // 🔹 Bước 5: nghỉ và di chuyển
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(MoveToNewPosition());
            }
        }

        #endregion

        //─────────────────────────────────────────────
        #region === RANGE ATTACKS ===

        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving || isMeleeAttacking || isPaused) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() =>
            {
                soldierAnim?.PlayShootBomb(useRightHand);
            }));
        }

        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving || isPaused) return;

            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null || bombPrefab == null) return;

            int index = useRightHand ? 1 : 0;
            if (flashBombEffects.Length > index && flashBombEffects[index] != null)
                flashBombEffects[index].SetActive(true);

            BossBomb bomb = PoolManager.Instance.ReuseComponent(
                bombPrefab.gameObject, spawnPoint.position, bombPrefab.transform.rotation)
                ?.GetComponent<BossBomb>();

            if (bomb != null && player != null)
                bomb.SetupFromPool(player, this);
        }

        private void ShootFireball()
        {
            if (m_isMoving || isMeleeAttacking || isPaused) return;
            StartCoroutine(RotateTowardsPlayer(() => soldierAnim?.PlayFireballShoot()));
        }

        public void CreateFireballFromAnimation()
        {
            if (m_isMoving || fireballPrefab == null || fireballSpawnPoint == null || isPaused) return;

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
                fireball.SetupFromPool(player, this);
        }

        private IEnumerator PerformMultipleSpecialSkills()
        {
            int specialSkillCount = Random.Range(1, 3);

            for (int i = 0; i < specialSkillCount; i++)
                yield return PerformSpecialSkill();

            yield return new WaitForSeconds(2f);
        }

        private IEnumerator PerformSpecialSkill()
        {
            Debug.Log("🔥 Boss performs new Phase 2 attack!");
            if (soldierAnim != null)
                soldierAnim.PlaySpecialSkill();
            yield return new WaitForSeconds(1f);
        }

        public void CreateSpecialEffectFromAnimation()
        {
            if (specialSkillEffect != null) specialSkillEffect.SetActive(true);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === ANIMATION CONTROL ===

        /// <summary>
        /// Bật/tắt animation sạc năng lượng (SetBool "IsRecharging").
        /// </summary>
        public void PlayRechargeAnimation(bool isRecharging)
        {
            soldierAnim?.SetHealing(isRecharging);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MOVEMENT ===

        public Coroutine MoveToCombatPoint(Transform combatPoint)
        {
            if (combatPoint == null) return null;
            return StartCoroutine(MoveAndRotateToPosition(combatPoint.position));
        }

        private IEnumerator MoveAndRotateToPosition(Vector3 destination, bool restoreSpeed = true)
        {
            if (agent == null) yield break;

            m_isMoving = true;
            bossAnim?.SetMoving(true);
            agent.isStopped = false;
            agent.SetDestination(destination);

            while (agent.pathPending || agent.remainingDistance > 0.3f)
            {
                if (agent.desiredVelocity.sqrMagnitude > 0.1f)
                    yield return RotateTowards(agent.desiredVelocity);

                yield return null;
            }

            bossAnim?.SetMoving(false);
            agent.isStopped = true;
            if (restoreSpeed) agent.speed = m_originalSpeed;
            m_isMoving = false;

            yield return RotateTowards(destination - model.position);
        }

        public IEnumerator MoveToTarget(Transform target)
        {
            if (target == null) yield break;
            yield return MoveAndRotateToPosition(target.position);
        }

        private IEnumerator MoveToNewPosition()
        {
            if (m_isMoving || isPaused) yield break;

            if (agent != null)
                agent.speed = m_originalSpeed * movementSpeedMultiplier;

            Vector3 newPosition = GetNewPosition();
            yield return MoveAndRotateToPosition(newPosition);
            yield return new WaitForSeconds(movementRestTime);
        }

        private Vector3 GetNewPosition()
        {
            Vector3 center = centerPoint != null ? centerPoint.position : transform.position;
            Vector3 randomDirection = Random.insideUnitSphere * wanderRadius + center;

            return NavMesh.SamplePosition(randomDirection, out var hit, wanderRadius, NavMesh.AllAreas)
                ? hit.position
                : transform.position;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === SPEED CONTROL ===

        public void SetSpeedMultiplier(float multiplier, float duration = 0f)
        {
            if (agent == null) return;

            if (speedRoutine != null)
                StopCoroutine(speedRoutine);

            speedRoutine = StartCoroutine(SpeedModifierRoutine(multiplier, duration));
        }

        private IEnumerator SpeedModifierRoutine(float multiplier, float duration)
        {
            float originalSpeed = agent.speed;
            agent.speed = m_originalSpeed * multiplier;

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
                agent.speed = originalSpeed;
            }

            speedRoutine = null;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === ROTATION (REFACTORED) ===

        /// <summary>
        /// Xoay model về hướng chỉ định (đã loại bỏ Y).
        /// </summary>
        private IEnumerator RotateTowards(Vector3 direction)
        {
            if (model == null) yield break;

            direction.y = 0;
            if (direction.sqrMagnitude < 0.001f)
                yield break;

            Quaternion startRot = model.rotation;
            Quaternion targetRot = Quaternion.LookRotation(direction.normalized);

            float elapsed = 0f;
            float duration = 0.25f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime * rotationSpeed;
                model.rotation = Quaternion.Slerp(startRot, targetRot, elapsed / duration);
                yield return null;
            }

            model.rotation = targetRot;
        }

        public IEnumerator RotateTowardsTarget(Transform target)
        {
            if (target == null) yield break;
            Vector3 direction = target.forward;
            yield return RotateTowards(direction);
        }

        public IEnumerator RotateTowardsPlayer(System.Action onComplete = null)
        {
            if (player == null)
            {
                onComplete?.Invoke();
                yield break;
            }

            yield return RotateTowards(player.transform.position - model.position);
            onComplete?.Invoke();
        }

        private void RotateTowardsMovementDirection()
        {
            if (agent == null || !agent.hasPath) return;
            if (agent.desiredVelocity.sqrMagnitude < 0.1f) return;

            StartCoroutine(RotateTowards(agent.desiredVelocity));
        }

        #endregion

        //─────────────────────────────────────────────
        #region === OVERRIDE ===

        protected override void UpdateBossBehavior() { }

        #endregion

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPoint.position, wanderRadius);
        }
    }
}
