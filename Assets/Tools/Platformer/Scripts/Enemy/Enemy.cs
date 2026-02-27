using UnityEngine;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    #region ===== DATA TYPES =====

    /// <summary>
    /// Config animation attack theo type + mode.
    /// (Odin chỉ dùng để show/hide field theo type, không can thiệp logic).
    /// </summary>
    [System.Serializable]
    public class EnemyAttackAnimConfig
    {
        [HorizontalGroup("Row", Width = 0.55f)]
        [HideLabel]
        public EnemyAttackType type = EnemyAttackType.NormalHit;

        [HorizontalGroup("Row")]
        [HideLabel]
        [InfoBox("NormalHit thường để Mode = Trigger (bắn Trigger Attack).", InfoMessageType.Info, "@type == EnemyAttackType.NormalHit")]
        [InfoBox("RollAttack nên để Mode = Bool (bật/tắt Bool Attack).", InfoMessageType.Warning, "@type == EnemyAttackType.RollAttack")]
        [InfoBox("RangedShot nên để Mode = Trigger (vẫn dùng chung Trigger Attack).", InfoMessageType.Info, "@type == EnemyAttackType.RangedShot")]
        public AttackAnimMode mode = AttackAnimMode.Trigger;

        // ===================== RANGED (PROJECTILE) =====================
        // Chỉ hiện khi chọn RangedShot
        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        public Transform shootPoint;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        public GameObject projectilePrefab;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        public GameObject muzzleEffect;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        [Min(0.1f)]
        public float rangedAttackRange = 6.0f;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        [Min(0f)]
        public float rangedAttackCooldown = 1.2f;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        [Min(0.1f)]
        public float projectileSpeed = 12f;

        [ShowIf("@type == EnemyAttackType.RangedShot")]
        [BoxGroup("RangedShot Settings")]
        public bool rangedUseContactDamage = true;

        [ShowIf("@type == EnemyAttackType.RangedShot && rangedUseContactDamage == false")]
        [BoxGroup("RangedShot Settings")]
        public int rangedOverrideDamage = 1;
    }

    #endregion

    [RequireComponent(typeof(EnemyStatsManager))]
    [RequireComponent(typeof(EnemyStateManager))]
    [RequireComponent(typeof(WaypointManager))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Enemy/Enemy")]
    public class Enemy : Entity<Enemy>
    {
        #region ===== INSPECTOR =====

        [Header("Enemy Settings")]
        public EnemyEvents enemyEvents;

        [Header("Attack Animation Configs")]
        [TableList(ShowIndexLabels = true, AlwaysExpanded = true)]
        public EnemyAttackAnimConfig[] attackAnimConfigs;

        [Header("Extra Attack (Optional)")]
        public ExtraAttackMode extraAttackMode = ExtraAttackMode.None;

        [ShowIf("@extraAttackMode == ExtraAttackMode.Animated")]
        [Min(0.1f)] public float extraAttackRange = 1.4f;

        [ShowIf("@extraAttackMode == ExtraAttackMode.Animated")]
        [Min(0f)] public float extraAttackCooldown = 1.0f;

        [ShowIf("@extraAttackMode == ExtraAttackMode.Animated")]
        public bool extraUseContactDamage = true;

        [ShowIf("@extraAttackMode == ExtraAttackMode.Animated && extraUseContactDamage == false")]
        public int extraOverrideDamage = 1;

        [Header("Roll Attack Settings")]
        public bool enableRollAttack = true;

        [ShowIf("@enableRollAttack")]
        [Min(0.1f)] public float rollAttackRange = 4.0f;

        [ShowIf("@enableRollAttack")]
        [Min(0.1f)] public float rollTopSpeed = 8.0f;

        [ShowIf("@enableRollAttack")]
        [Min(0.1f)] public float rollAcceleration = 40.0f;

        [ShowIf("@enableRollAttack")]
        [Min(0.05f)] public float rollStopDistance = 0.4f;

        [ShowIf("@enableRollAttack")]
        [Min(0f)] public float rollCooldown = 2.0f;

        #endregion

        #region ===== REFERENCES / PROPERTIES =====

        /// <summary>Player đang bị enemy phát hiện / rượt.</summary>
        public Player player { get; protected set; }

        /// <summary>Stats của enemy.</summary>
        public EnemyStatsManager stats { get; protected set; }

        /// <summary>Waypoint manager.</summary>
        public WaypointManager waypoints { get; protected set; }

        /// <summary>Máu của enemy.</summary>
        public Health health { get; protected set; }

        private EnemyAnimator m_enemyAnimator;

        #endregion

        #region ===== RUNTIME CACHE =====

        private readonly Collider[] m_sightOverlaps = new Collider[1024];

        // Extra attack runtime
        private bool m_extraAttacking;
        private float m_nextExtraAttackTime;

        // Ranged runtime
        private bool m_rangedAttacking;
        private float m_nextRangedAttackTime;

        // Roll runtime
        private bool m_rollAttacking;
        private Vector3 m_rollTargetPos;
        private float m_nextRollTime;

        // Bool "Attack" dùng cho RollAttack mode Bool
        private static readonly int s_attackBoolHash = Animator.StringToHash("Attack");

        #endregion

        #region ===== UNITY LIFECYCLE =====

        /// <summary>Awake: cache component cần thiết.</summary>
        protected override void Awake()
        {
            base.Awake();

            InitializeTag();
            InitializeStatsManager();
            InitializeWaypointsManager();
            InitializeHealth();
            InitializeEnemyAnimator();
        }

        /// <summary>Update vòng đời của Entity: chỉ xử lý phát hiện player.</summary>
        protected override void OnUpdate()
        {
            HandleSight();
        }

        #endregion

        #region ===== INITIALIZE =====

        /// <summary>Set tag theo GameTags.</summary>
        protected virtual void InitializeTag() => tag = GameTags.Enemy;

        /// <summary>Cache EnemyStatsManager.</summary>
        protected virtual void InitializeStatsManager() => stats = GetComponent<EnemyStatsManager>();

        /// <summary>Cache WaypointManager.</summary>
        protected virtual void InitializeWaypointsManager() => waypoints = GetComponent<WaypointManager>();

        /// <summary>Cache Health.</summary>
        protected virtual void InitializeHealth() => health = GetComponent<Health>();

        /// <summary>Cache EnemyAnimator.</summary>
        protected virtual void InitializeEnemyAnimator() => m_enemyAnimator = GetComponent<EnemyAnimator>();

        #endregion

        #region ===== DAMAGE / REVIVE =====

        /// <summary>Nhận damage: trừ máu, chết thì tắt controller.</summary>
        public override void ApplyDamage(int amount, Vector3 origin)
        {
            if (health == null) return;
            if (health.isEmpty || health.recovering) return;

            health.Damage(amount);
            enemyEvents?.OnDamage?.Invoke();

            if (health.isEmpty)
            {
                controller.enabled = false;
                enemyEvents?.OnDie?.Invoke();
            }
        }

        /// <summary>Hồi sinh: reset máu và bật lại controller.</summary>
        public virtual void Revive()
        {
            if (health == null) return;
            if (!health.isEmpty) return;

            health.ResetHealth();
            controller.enabled = true;
            enemyEvents?.OnRevive?.Invoke();
        }

        #endregion

        #region ===== MOVEMENT HELPERS (STATS-BASED) =====

        /// <summary>Tăng tốc theo hướng, dùng turningDrag/acceleration/topSpeed.</summary>
        public virtual void Accelerate(Vector3 direction, float acceleration, float topSpeed)
        {
            if (stats?.current == null) return;
            Accelerate(direction, stats.current.turningDrag, acceleration, topSpeed);
        }

        /// <summary>Giảm tốc theo deceleration.</summary>
        public virtual void Decelerate()
        {
            if (stats?.current == null) return;
            Decelerate(stats.current.deceleration);
        }

        /// <summary>Ma sát theo friction.</summary>
        public virtual void Friction()
        {
            if (stats?.current == null) return;
            Decelerate(stats.current.friction);
        }

        /// <summary>Trọng lực theo gravity.</summary>
        public virtual void Gravity()
        {
            if (stats?.current == null) return;
            Gravity(stats.current.gravity);
        }

        /// <summary>Dính đất theo snapForce.</summary>
        public virtual void SnapToGround()
        {
            if (stats?.current == null) return;
            SnapToGround(stats.current.snapForce);
        }

        /// <summary>Xoay mặt theo hướng với rotationSpeed.</summary>
        public virtual void FaceDirectionSmooth(Vector3 direction)
        {
            if (stats?.current == null) return;
            FaceDirection(direction, stats.current.rotationSpeed);
        }

        #endregion

        #region ===== CONTACT ATTACK (CHẠM LÀ TRỪ MÁU) =====

        /// <summary>Chạm player thì trừ máu (cơ chế cũ).</summary>
        public virtual void ContactAttack(Collider other)
        {
            if (!other.CompareTag(GameTags.Player)) return;
            if (!other.TryGetComponent(out Player p)) return;
            if (stats?.current == null) return;

            var stepping = controller.bounds.max + Vector3.down * stats.current.contactSteppingTolerance;

            if (p.isGrounded || !BoundsHelper.IsBellowPoint(controller.collider, stepping))
            {
                if (stats.current.contactPushback)
                    lateralVelocity = -localForward * stats.current.contactPushBackForce;

                p.ApplyDamage(stats.current.contactDamage, transform.position);
                enemyEvents?.OnPlayerContact?.Invoke();
            }
        }

        /// <summary>Unity trigger: gọi contact attack.</summary>
        protected virtual void OnTriggerEnter(Collider other)
        {
            ContactAttack(other);
        }

        #endregion

        #region ===== ATTACK CONFIG / DISPATCH =====

        /// <summary>Tìm config theo type.</summary>
        private EnemyAttackAnimConfig GetAttackConfig(EnemyAttackType type)
        {
            if (attackAnimConfigs == null) return null;

            for (int i = 0; i < attackAnimConfigs.Length; i++)
            {
                var cfg = attackAnimConfigs[i];
                if (cfg != null && cfg.type == type) return cfg;
            }

            return null;
        }

        /// <summary>
        /// Gọi animation theo rule:
        /// - NormalHit + Trigger -> TriggerAttack()
        /// - RangedShot + Trigger -> TriggerAttack() (dùng chung)
        /// - RollAttack + Bool -> Bool "Attack"
        /// </summary>
        public void PlayAttack(EnemyAttackType type, bool active)
        {
            if (m_enemyAnimator == null || m_enemyAnimator.animator == null) return;

            var cfg = GetAttackConfig(type);
            if (cfg == null) return;

            // Trigger (dùng chung cho NormalHit và RangedShot)
            if (cfg.mode == AttackAnimMode.Trigger &&
                (cfg.type == EnemyAttackType.NormalHit || cfg.type == EnemyAttackType.RangedShot))
            {
                if (!active) return;
                m_enemyAnimator.TriggerAttack();
                return;
            }

            // RollAttack + Bool => Bool "Attack"
            if (cfg.type == EnemyAttackType.RollAttack && cfg.mode == AttackAnimMode.Bool)
            {
                m_enemyAnimator.animator.SetBool(s_attackBoolHash, active);
                return;
            }
        }

        #endregion

        #region ===== EXTRA ATTACK (ANIMATION) =====

        /// <summary>Đang extra attack hay không.</summary>
        public bool IsExtraAttacking() => m_extraAttacking;

        /// <summary>Thử bắt đầu extra attack (gọi từ FollowEnemyState).</summary>
        public void TryStartExtraAttack()
        {
            if (extraAttackMode != ExtraAttackMode.Animated) return;
            if (m_extraAttacking) return;
            if (Time.time < m_nextExtraAttackTime) return;
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > extraAttackRange) return;

            m_extraAttacking = true;
            m_nextExtraAttackTime = Time.time + extraAttackCooldown;

            PlayAttack(EnemyAttackType.NormalHit, true);
        }

        /// <summary>Animation Event: frame trúng đòn để trừ máu player.</summary>
        public void ExtraAttackHit_AnimationEvent()
        {
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            if (dist > extraAttackRange + 0.2f) return;

            int dmg = (extraUseContactDamage && stats?.current != null)
                ? stats.current.contactDamage
                : extraOverrideDamage;

            player.ApplyDamage(dmg, transform.position);
        }

        /// <summary>Animation Event: cuối clip để kết thúc extra attack.</summary>
        public void ExtraAttackEnd_AnimationEvent()
        {
            m_extraAttacking = false;
        }

        #endregion

        #region ===== RANGED ATTACK (PROJECTILE) =====

        /// <summary>Đang bắn hay không.</summary>
        public bool IsRangedAttacking() => m_rangedAttacking;

        /// <summary>Check điều kiện bắn (range/cooldown/prefab).</summary>
        public bool CanStartRangedAttack()
        {
            if (player == null) return false;
            if (m_rangedAttacking) return false;
            if (Time.time < m_nextRangedAttackTime) return false;

            var cfg = GetAttackConfig(EnemyAttackType.RangedShot);
            if (cfg == null) return false;
            if (cfg.mode != AttackAnimMode.Trigger) return false;
            if (cfg.projectilePrefab == null) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= cfg.rangedAttackRange;
        }

        /// <summary>Bắt đầu bắn: set state + trigger Attack.</summary>
        public void TryStartRangedAttack()
        {
            if (!CanStartRangedAttack()) return;

            var cfg = GetAttackConfig(EnemyAttackType.RangedShot);
            if (cfg == null) return;

            m_rangedAttacking = true;
            m_nextRangedAttackTime = Time.time + cfg.rangedAttackCooldown;

            PlayAttack(EnemyAttackType.RangedShot, true);
        }

        /// <summary>Animation Event: frame bắn để spawn projectile.</summary>
        public void RangedFire_AnimationEvent()
        {
            if (player == null) return;

            var cfg = GetAttackConfig(EnemyAttackType.RangedShot);
            if (cfg == null) return;
            if (cfg.projectilePrefab == null) return;

            Transform sp = cfg.shootPoint != null ? cfg.shootPoint : transform;

            Vector3 dir = player.transform.position - sp.position;
            dir.y = 0f;

            if (dir.sqrMagnitude < 0.0001f) dir = localForward;
            dir.Normalize();

            Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

            // 1) Bật muzzle effect
            if (cfg.muzzleEffect != null)
            {
                cfg.muzzleEffect.SetActive(true);

                // Nếu là ParticleSystem thì Play lại cho chắc (tuỳ bạn)
                if (cfg.muzzleEffect.TryGetComponent<ParticleSystem>(out var ps))
                {
                    ps.Clear(true);
                    ps.Play(true);
                }
            }

            // 2) Spawn projectile (Pool -> fallback Instantiate)
            GameObject go = null;

            if (PoolManager.Instance != null)
            {
                Component pooled = PoolManager.Instance.ReuseComponent(cfg.projectilePrefab, sp.position, rot);
                if (pooled != null) go = pooled.gameObject;
            }

            if (go == null)
                go = Instantiate(cfg.projectilePrefab, sp.position, rot);

            // 3) Set vận tốc bay
            if (go.TryGetComponent<Rigidbody>(out var rb))
                rb.linearVelocity = dir * cfg.projectileSpeed;

            // 4) Damage
            int dmg = (cfg.rangedUseContactDamage && stats?.current != null)
                ? stats.current.contactDamage
                : cfg.rangedOverrideDamage;

            // 5) Init projectile (ưu tiên gọi component)
            if (go.TryGetComponent<EnemyProjectile>(out var proj))
                proj.Init(dmg);
        }

        /// <summary>Animation Event: cuối clip để kết thúc bắn.</summary>
        public void RangedAttackEnd_AnimationEvent()
        {
            m_rangedAttacking = false;
        }

        #endregion

        #region ===== ROLL ATTACK =====

        /// <summary>Đang roll hay không.</summary>
        public bool IsRollAttacking() => m_rollAttacking;

        /// <summary>Check điều kiện bắt đầu roll.</summary>
        public bool CanStartRollAttack()
        {
            if (!enableRollAttack) return false;
            if (player == null) return false;
            if (m_rollAttacking) return false;
            if (Time.time < m_nextRollTime) return false;

            var cfg = GetAttackConfig(EnemyAttackType.RollAttack);
            if (cfg == null || cfg.mode != AttackAnimMode.Bool) return false;

            float dist = Vector3.Distance(transform.position, player.transform.position);
            return dist <= rollAttackRange;
        }

        /// <summary>Bắt đầu roll: lock vị trí player và bật Bool Attack.</summary>
        public void StartRollAttack()
        {
            if (!CanStartRollAttack()) return;

            m_rollTargetPos = player.transform.position;

            m_rollAttacking = true;
            m_nextRollTime = Time.time + rollCooldown;

            PlayAttack(EnemyAttackType.RollAttack, true);
        }

        /// <summary>Step roll: lao tới điểm đã lock, tới gần thì kết thúc.</summary>
        public void StepRollAttack()
        {
            if (!m_rollAttacking) return;

            Vector3 toTarget = m_rollTargetPos - transform.position;
            toTarget.y = 0f;

            if (toTarget.sqrMagnitude <= rollStopDistance * rollStopDistance)
            {
                EndRollAttack();
                return;
            }

            Vector3 dir = toTarget.normalized;

            Accelerate(dir, rollAcceleration, rollTopSpeed);
            FaceDirectionSmooth(dir);
        }

        /// <summary>Kết thúc roll: tắt Bool Attack và clamp lại tốc độ chase.</summary>
        public void EndRollAttack()
        {
            if (!m_rollAttacking) return;

            m_rollAttacking = false;

            PlayAttack(EnemyAttackType.RollAttack, false);

            if (stats != null && stats.current != null)
            {
                float followTop = stats.current.followTopSpeed;
                lateralVelocity = Vector3.ClampMagnitude(lateralVelocity, followTop);
            }
        }

        #endregion

        #region ===== SIGHT / DETECTION =====

        /// <summary>Phát hiện player (spot) và mất mục tiêu (escape).</summary>
        protected virtual void HandleSight()
        {
            if (!player)
            {
                if (stats?.current == null) return;

                int overlaps = Physics.OverlapSphereNonAlloc(transform.position, stats.current.spotRange, m_sightOverlaps);
                for (int i = 0; i < overlaps; i++)
                {
                    var col = m_sightOverlaps[i];
                    if (col == null) continue;

                    if (col.CompareTag(GameTags.Player) && col.TryGetComponent<Player>(out var spotted))
                    {
                        player = spotted;
                        OnPlayerSpotted();
                        enemyEvents?.OnPlayerSpotted?.Invoke();
                        return;
                    }
                }
            }
            else
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);

                bool playerDead = player.health != null && player.health.current == 0;
                bool tooFar = stats?.current != null && distance > stats.current.viewRange;

                if (playerDead || tooFar)
                {
                    player = null;
                    enemyEvents?.OnPlayerScaped?.Invoke();
                }
            }
        }

        /// <summary>Khi thấy player: nếu bật followTargetOnSight thì chuyển qua FollowEnemyState.</summary>
        protected virtual void OnPlayerSpotted()
        {
            if (stats?.current == null) return;
            if (!stats.current.followTargetOnSight) return;
            if (states == null) return;

            states.Change<FollowEnemyState>();
        }

        #endregion
    }
}