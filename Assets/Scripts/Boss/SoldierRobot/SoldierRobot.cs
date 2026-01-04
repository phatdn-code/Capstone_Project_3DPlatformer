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
        [SerializeField] private int meleeDamage = 1;

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

        [Header("Danger Zone Settings - Common")]
        [SerializeField] private GameObject dangerZonePrefab;
        [SerializeField] private float phaseRadius = 3f;            // bán kính nổ của từng DangerZone
        [SerializeField] private float phaseWarningDuration = 2f;   // thời gian chờ trước khi nổ

        [Header("Phase 1 DangerZone Settings (Random)")]
        [SerializeField] private int aoeCount = 5;
        [SerializeField] private float aoeRadiusRange = 8f;
        [SerializeField] private float aoeSpawnDelay = 0.15f;
        [SerializeField] private int phaseDamage = 20;

        [Header("Phase 2 DangerZone Settings (Dual Ring)")]
        [SerializeField] private int innerRingCount = 4;
        [SerializeField] private float innerRadius = 4f;
        [SerializeField] private int innerDamage = 20;

        [SerializeField] private int outerRingCount = 6;
        [SerializeField] private float outerRadius = 8f;
        [SerializeField] private int outerDamage = 26;
        [SerializeField] private float aoeDelayBetweenRings = 1.2f;

        #endregion
        //─────────────────────────────────────────────
        #region === RUNTIME VARIABLES ===

        private NavMeshAgent agent;
        private SoldierRobotAnimation soldierAnim;
        private Coroutine speedRoutine;
        private Coroutine attackRoutine;

        private bool isPaused;
        private bool isMeleeAttacking;
        private bool m_isInAttackSequence;
        private bool m_isMoving;

        private bool isPhase2Active;

        private float wanderRadius;
        private float nextMeleeTime;
        private float m_originalSpeed;

        public bool IsPaused => isPaused;

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
            soldierAnim = BossAnim as SoldierRobotAnimation;
            if (agent != null) m_originalSpeed = agent.speed;
        }

        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = PlayerHub.Instance.Player;
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
            isPhase2Active = (phaseIndex >= 2);
        }

        #endregion
        //─────────────────────────────────────────────
        #region === PAUSE CONTROL ===

        public void SetPaused(bool pause)
        {
            isPaused = pause;
            if (agent != null) agent.isStopped = pause;
            if (BossAnim != null) BossAnim.SetMoving(false);
        }

        #endregion
        //─────────────────────────────────────────────
        #region === MELEE ATTACK ===

        private IEnumerator PerformMeleeAttack()
        {
            isMeleeAttacking = true;
            nextMeleeTime = Time.time + meleeCooldown;

            if (agent != null) agent.isStopped = true;

            yield return RotateTowardsPlayer(() => BossAnim?.PlayMeleeAttack());
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
            attackRoutine = StartCoroutine(ExecuteAttackSequence());
        }

        public void StopAttackSequence()
        {
            m_isInAttackSequence = false;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }

        private IEnumerator ExecuteAttackSequence()
        {
            while (true)
            {
                if (isPaused || IsInCutscene)
                {
                    yield return null;
                    continue;
                }

                if (!isMeleeAttacking)
                {
                    // ────────────────────────────────
                    // 🔹 BƯỚC 1: NÉM BOMB (luôn thực hiện)
                    // ────────────────────────────────
                    ShootBomb(true);
                    yield return new WaitForSeconds(1f);
                    ShootBomb(false);
                    yield return new WaitForSeconds(2f);

                    // ────────────────────────────────
                    // 🔹 BƯỚC 2: Random chọn skill phụ
                    // ────────────────────────────────
                    yield return PerformRandomSecondarySkill();
                }

                // ────────────────────────────────
                // 🔹 BƯỚC 3: Nghỉ và di chuyển vị trí mới
                // ────────────────────────────────
                yield return new WaitForSeconds(3f);
                yield return StartCoroutine(MoveToNewPosition());
            }
        }

        /// <summary>
        /// 🎲 Random chọn skill phụ (Fireball hoặc AOE tuỳ theo phase)
        /// </summary>
        private IEnumerator PerformRandomSecondarySkill()
        {
            float roll = Random.value;

            // Tỉ lệ base
            float fireballChance = isPhase2Active ? 0.4f : 0.5f;
            float aoeChance = isPhase2Active ? 0.7f : 0.8f;

            // 🔥 Fireball
            if (roll < fireballChance)
                yield return FireballSequence();

            else if (roll < aoeChance)
            {
                // 💥 AOE Skill (tuỳ phase)
                if (isPhase2Active) yield return PerformAdvancedAOESkill();
                else yield return PerformSpecialSkill();
            }

            // 😴 Không làm gì
            else yield return new WaitForSeconds(1f);
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
            if (m_isMoving || isMeleeAttacking || isPaused || IsInCutscene) return;
            Transform spawnPoint = useRightHand ? rightHandSpawnPoint : leftHandSpawnPoint;
            if (spawnPoint == null) return;

            StartCoroutine(RotateTowardsPlayer(() => soldierAnim?.PlayShootBomb(useRightHand)));
        }

        public void ShootBombFromAnimation(bool useRightHand)
        {
            if (m_isMoving || isPaused || IsInCutscene) return;
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
            if (m_isMoving || isMeleeAttacking || isPaused || IsInCutscene) return;
            StartCoroutine(RotateTowardsPlayer(() => soldierAnim?.PlayFireballShoot()));
        }

        public void CreateFireballFromAnimation()
        {
            if (m_isMoving || isPaused || IsInCutscene) return;

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
                fireball.SetupFromPool(player.transform, this);
        }

        private IEnumerator PerformSpecialSkill()
        {
            Debug.Log("🔥 Boss performs Phase 1 Danger Zone skill!");
            soldierAnim?.PlaySpecialSkill();

            yield return new WaitForSeconds(0.5f);
            electricSmashEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);

            yield return StartCoroutine(SpawnRandomDangerZones());
        }

        private IEnumerator PerformAdvancedAOESkill()
        {
            Debug.Log("💢 Boss performs Phase 2 dual-ring Danger Zone!");
            soldierAnim?.PlaySpecialSkill();

            yield return new WaitForSeconds(0.5f);
            electricSmashEffect.SetActive(true);
            yield return new WaitForSeconds(0.3f);

            // Vòng trong
            yield return StartCoroutine(
                SpawnRingDangerZones(
                    innerRingCount,
                    innerRadius,
                    innerDamage
                )
            );

            // Delay giữa 2 vòng
            yield return new WaitForSeconds(aoeDelayBetweenRings);

            // Vòng ngoài
            yield return StartCoroutine(
                SpawnRingDangerZones(
                    outerRingCount,
                    outerRadius,
                    outerDamage
                )
            );
        }


        /// <summary>
        /// Phase 2: Spawn DangerZone theo vòng tròn quanh boss.
        /// </summary>
        private IEnumerator SpawnRingDangerZones(int count, float ringRadius, int ringDamage)
        {
            if (dangerZonePrefab == null)
            {
                Debug.LogWarning("⚠️ Missing dangerZonePrefab (Phase 2)");
                yield break;
            }

            float step = 360f / Mathf.Max(1, count);

            for (int i = 0; i < count; i++)
            {
                float angle = step * i * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ringRadius;
                Vector3 spawnPos = transform.position + offset + Vector3.up * 0.1f;

                var pooled = PoolManager.Instance.ReuseComponent(
                    dangerZonePrefab, spawnPos, Quaternion.identity
                );

                if (pooled != null)
                {
                    var dz = pooled as DangerZone ?? pooled.GetComponent<DangerZone>();
                    if (dz != null)
                    {
                        // Phase 2: bán kính hit vẫn là phaseRadius,
                        // thời gian chờ vẫn phaseWarningDuration,
                        // damage = ringDamage (innerDamage hoặc outerDamage)
                        dz.Configure(
                            phaseRadius,
                            phaseWarningDuration,
                            ringDamage
                        );
                    }
                }

                yield return new WaitForSeconds(0.06f);
            }
        }

        /// <summary>
        /// Phase 1: Spawn DangerZone ngẫu nhiên xung quanh centerPoint.
        /// </summary>
        private IEnumerator SpawnRandomDangerZones()
        {
            if (dangerZonePrefab == null)
            {
                Debug.LogWarning("⚠️ Missing dangerZonePrefab (Phase 1)");
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

                var pooled = PoolManager.Instance.ReuseComponent(
                    dangerZonePrefab, spawnPos, Quaternion.identity
                );

                if (pooled != null)
                {
                    var dz = pooled as DangerZone ?? pooled.GetComponent<DangerZone>();
                    if (dz != null)
                    {
                        // Phase 1: dùng phaseRadius + phaseWarningDuration + phaseDamage
                        dz.Configure(
                            phaseRadius,
                            phaseWarningDuration,
                            phaseDamage
                        );
                    }
                }

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
            BossAnim?.SetMoving(true);
            agent.isStopped = false;
            agent.SetDestination(destination);

            while (agent.pathPending || agent.remainingDistance > 0.3f)
            {
                if (agent.desiredVelocity.sqrMagnitude > 0.1f)
                    yield return RotateTowards(agent.desiredVelocity);
                yield return null;
            }

            BossAnim?.SetMoving(false);
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

        public Coroutine MoveToPoint(Vector3 position)
        {
            GameObject temp = new GameObject("Temp_Move_Point");
            temp.transform.position = position;
            return StartCoroutine(MoveToTarget(temp.transform));
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

        public IEnumerator RotateTowardsPoint(Vector3 point)
        {
            Vector3 direction = point - model.position;
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
