using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// SoldierRobot Boss Controller:
    /// - Melee attack, Bomb throw, Fireball, Phase 2/3 AOE skills
    /// - Movement, rotation, and attack sequence handling
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
        [SerializeField] private float movementRestTime = 2f;
        [SerializeField] private float movementSpeedMultiplier = 1.5f;

        [Header("Rotation Settings")]
        [SerializeField] private Transform model;
        [SerializeField] private float rotationSpeed = 5f;

        [Header("Visual Effects")]
        [SerializeField] private GameObject[] flashBombEffects; // [0] = left, [1] = right
        [SerializeField] private GameObject flashFireballEffect;
        [SerializeField] private GameObject electricSmashEffect;

        [Header("AOE Skill (Phase 2)")]
        [SerializeField] private GameObject aoeWarningPrefab;   // Prefab vòng cảnh báo (AOEWarningUnified)
        [SerializeField] private int aoeCount = 5;
        [SerializeField] private float aoeRadiusRange = 8f;
        [SerializeField] private float aoeSpawnDelay = 0.15f;

        [Header("AOE Skill (Phase 3)")]
        [SerializeField] private int innerRingCount = 4;
        [SerializeField] private int outerRingCount = 6;
        [SerializeField] private float innerRadius = 4f;
        [SerializeField] private float outerRadius = 8f;
        [SerializeField] private float aoeDelayBetweenRings = 1.2f;
        [SerializeField] private float innerWarnDuration = 1.0f;
        [SerializeField] private float outerWarnDuration = 1.8f;
        [SerializeField] private int innerDamage = 20;
        [SerializeField] private int outerDamage = 26;

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
        private bool unlockedPhase3Attack;

        private float wanderRadius;
        private float nextMeleeTime;
        private float m_originalSpeed;

        #endregion
        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
            DisableAllEffects();
            OnBossPhaseStartEvent.AddListener(OnPhaseChanged);
            wanderRadius = MovementBoundaryZone.Instance.GetBoundaryRadius();
        }

        protected override void OnBattleStarted()
        {
            StartAttackSequence(); // Bắt đầu vòng tấn công
        }

        protected override void Update()
        {
            base.Update();
            if (isPaused || player == null) return;

            RotateTowardsMovementDirection(); // Xoay theo hướng di chuyển

            float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distanceToPlayer <= meleeRange && Time.time >= nextMeleeTime && !isMeleeAttacking)
                StartCoroutine(PerformMeleeAttack()); // Tấn công cận chiến nếu gần
        }

        #endregion
        //─────────────────────────────────────────────
        #region === INITIALIZATION ===

        private void InitializeComponents()
        {
            agent = GetComponent<NavMeshAgent>();
            soldierAnim = base.bossAnim as SoldierRobotAnimation;
            if (agent != null) m_originalSpeed = agent.speed;
        }

        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = FindFirstObjectByType<Player>();
        }

        private void DisableAllEffects()
        {
            if (flashBombEffects != null)
                foreach (var fx in flashBombEffects)
                    if (fx != null) fx.SetActive(false);

            if (flashFireballEffect != null)
                flashFireballEffect.SetActive(false);
        }

        private void OnPhaseChanged(int phaseIndex)
        {
            if (phaseIndex == 1) unlockedPhase2Attack = true;
            if (phaseIndex == 2) unlockedPhase3Attack = true;
        }

        #endregion
        //─────────────────────────────────────────────
        #region === PAUSE CONTROL ===

        public void SetPaused(bool pause)
        {
            isPaused = pause;
            if (agent != null) agent.isStopped = pause;
            if (bossAnim != null) bossAnim.SetMoving(false);
        }

        public bool IsPaused => isPaused;

        #endregion
        //─────────────────────────────────────────────
        #region === MELEE ATTACK ===

        private IEnumerator PerformMeleeAttack()
        {
            isMeleeAttacking = true;
            nextMeleeTime = Time.time + meleeCooldown;

            if (agent != null) agent.isStopped = true;

            yield return RotateTowardsPlayer(() => bossAnim?.PlayMeleeAttack());
            yield return new WaitForSeconds(1f);

            if (agent != null) agent.isStopped = false;
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
                    // 🔹 Bước 1: Ném bomb
                    ShootBomb(true);
                    yield return new WaitForSeconds(1f);
                    ShootBomb(false);
                    yield return new WaitForSeconds(3f);

                    // 🔹 Bước 2: Skill đặc biệt Phase 2 / Phase 3
                    bool usedSpecialSkill = false;

                    if (unlockedPhase3Attack && Random.value < 0.7f)
                    {
                        yield return PerformAdvancedAOESkill(); // Phase 3 AOE
                        usedSpecialSkill = true;
                    }
                    else if (unlockedPhase2Attack && Random.value < 0.6f)
                    {
                        yield return PerformSpecialSkill(); // Phase 2 AOE
                        usedSpecialSkill = true;
                    }

                    if (!usedSpecialSkill)
                        yield return FireballSequence();
                }

                // 🔹 Bước 3: Nghỉ và di chuyển
                yield return new WaitForSeconds(5f);
                yield return StartCoroutine(MoveToNewPosition());
            }
        }

        #endregion
        //─────────────────────────────────────────────
        #region === RANGE ATTACKS (Bomb, Fireball, AOE) ===

        private IEnumerator FireballSequence()
        {
            int fireballCount = Random.Range(1, 3);
            for (int i = 0; i < fireballCount; i++)
            {
                ShootFireball();
                if (i < fireballCount - 1)
                    yield return new WaitForSeconds(2f);
            }
        }

        private void ShootBomb(bool useRightHand)
        {
            if (m_isMoving || isMeleeAttacking || isPaused) return;
            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() => soldierAnim?.PlayShootBomb(useRightHand)));
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

        private IEnumerator PerformSpecialSkill()
        {
            Debug.Log("🔥 Boss performs Phase 2 AOE skill!");
            soldierAnim?.PlaySpecialSkill();

            yield return new WaitForSeconds(0.5f);
            electricSmashEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);

            yield return StartCoroutine(SpawnAOEWarnings());
        }

        private IEnumerator PerformAdvancedAOESkill()
        {
            Debug.Log("💢 Boss performs Phase 3 dual-ring AOE!");
            soldierAnim?.PlaySpecialSkill();

            yield return new WaitForSeconds(0.5f);
            electricSmashEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(SpawnRingAOE(innerRingCount, innerRadius, innerWarnDuration, innerDamage, AOEMode.Phase3_Inner));
            yield return new WaitForSeconds(aoeDelayBetweenRings);
            yield return StartCoroutine(SpawnRingAOE(outerRingCount, outerRadius, outerWarnDuration, outerDamage, AOEMode.Phase3_Outer));
        }

        private IEnumerator SpawnRingAOE(int count, float radius, float warnDuration, int dmg, AOEMode mode)
        {
            if (aoeWarningPrefab == null)
            {
                Debug.LogWarning("⚠️ Missing aoeWarningPrefab (Phase 3)");
                yield break;
            }

            float step = 360f / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float angle = step * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 spawnPos = transform.position + offset + Vector3.up * 0.1f;

                var aoe = PoolManager.Instance.ReuseComponent(
                    aoeWarningPrefab, spawnPos, Quaternion.identity)
                    ?.GetComponent<AOEWarningUnified>();

                if (aoe != null)
                    aoe.Configure(mode, radius, warnDuration, dmg);

                yield return new WaitForSeconds(0.06f);
            }
        }

        private IEnumerator SpawnAOEWarnings()
        {
            if (aoeWarningPrefab == null)
            {
                Debug.LogWarning("⚠️ Missing aoeWarningPrefab (Phase 2)");
                yield break;
            }

            for (int i = 0; i < aoeCount; i++)
            {
                Vector3 offset = new Vector3(
                    Random.Range(-aoeRadiusRange, aoeRadiusRange),
                    0f,
                    Random.Range(-aoeRadiusRange, aoeRadiusRange)
                );

                Vector3 spawnPos = centerPoint.position + offset + Vector3.up * 0.1f;

                var aoe = PoolManager.Instance.ReuseComponent(
                    aoeWarningPrefab, spawnPos, Quaternion.identity)
                    ?.GetComponent<AOEWarningUnified>();

                if (aoe != null)
                    aoe.Configure(AOEMode.Phase2, 3f, 1.5f, 20);

                yield return new WaitForSeconds(aoeSpawnDelay);
            }
        }

        #endregion
        //─────────────────────────────────────────────
        #region === ANIMATION CONTROL ===

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
        //─────────────────────────────────────────────

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(centerPoint.position, wanderRadius);
        }
    }
}
