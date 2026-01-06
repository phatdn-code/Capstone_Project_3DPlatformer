using DG.Tweening;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// DragonRobot: di chuyển, xoay mặt và điều khiển các skill tấn công (Flame / Blast).
    /// </summary>
    public class DragonRobot : BossCore
    {
        //─────────────────────────────────────────────────────────────
        #region === INSPECTOR: REFERENCES ===

        [Header("Player Reference")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Visual / Facing Settings")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool useYRotation = true;
        [SerializeField] private float turnDuration = 0.2f;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: MOVEMENT ===

        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 6f;
        [SerializeField] private float distanceSpeedFactor = 0.6f;
        [SerializeField] private float minUnitsPerSecond = 5f;
        [SerializeField] private float maxUnitsPerSecond = 25f;
        [SerializeField] private Ease moveEase = Ease.InOutSine;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: ANIMATION ===

        [Header("Animation Logic")]
        [SerializeField] private float horizontalAnimThreshold = 0.1f;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: COMBAT FLOW ===

        [Header("Attack Settings")]
        [SerializeField] private int totalSkillCount = 3;
        [SerializeField] private float attackStartDelay = 1f;

        [Header("Attack Stop Condition")]
        [SerializeField] private int stopAttackAfterDamage = 20;

        [Header("Attack Time Limit By Portal")]
        [SerializeField] private float attackDurationCorrectPortal = 90f;
        [SerializeField] private float attackDurationWrongPortal = 60f;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: SKILL - FLAME THROWER ===

        [Header("Flame Thrower")]
        [SerializeField] private float flameAttackDuration = 2f;
        [SerializeField] private float flameMoveDuration = 1.2f;
        [SerializeField] private GameObject flameEffectObject;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: SKILL - BLAST ===

        [Header("Blast Attack")]
        [SerializeField] private BossFireball fireballPrefab;
        [SerializeField] private Transform fireballSpawnPoint;
        [SerializeField] private GameObject blastFlashEffect;
        [SerializeField] private int blastFireballCount = 3;
        [SerializeField] private float blastAimDuration = 0.5f;
        [SerializeField] private float blastShotAnimDuration = 0.6f;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: SKILL - METEOR ===

        [Header("Meteor Attack")]
        [SerializeField] private float meteorMoveDuration = 0.8f;
        [SerializeField] private float meteorAttackRadius = 30f;
        [SerializeField] private float meteorStrikeAnimDuration = 1.0f;
        [SerializeField] private float meteorBetweenPointsDelay = 1f;
        [SerializeField] private GameObject meteorEffectObject;
        [SerializeField] private GameObject meteorWarningEffect;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === INSPECTOR: SKILL - METEOR RAIN ===

        [Header("Meteor Rain Attack")]
        [SerializeField] private float meteorRainMoveDuration = 0.8f;
        [SerializeField] private float meteorRainAttackRadius = 30f;
        [SerializeField] private float meteorRainStrikeAnimDuration = 4.0f;
        [SerializeField] private float meteorRainBetweenPointsDelay = 1f;
        [SerializeField] private GameObject meteorRainEffectObject;
        [SerializeField] private GameObject meteorRainWarningEffect;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === RUNTIME: CACHED REFERENCES ===

        private PortalZoneManager zoneManager;
        private DragonRobotAnimation dragonAnim;
        private BossShieldController shieldControl;
        private BossTelegraphGrowFromGround bossTelegraph;
        private Coroutine attackRoutine;
        private Coroutine attackTimeLimitRoutine;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === RUNTIME: ATTACK SELECTION (ANTI-REPEAT) ===

        private readonly List<int> _skillBag = new();
        private int _skillBagCount = -1;
        private int _lastSkillIndex = -1;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === RUNTIME: STOP ATTACK CONDITION ===

        private int _damageTakenWhileAttacking;
        private int _lastHpSnapshot;

        private bool _stopAttackingRequested;
        private bool _isDamageImmuneThisRound;
        private bool _pendingRetreatAfterTakeDamage;
        private bool _isShieldRecharging;
        private bool _isRetreating;

        public bool IsDamageImmuneThisRound => _isDamageImmuneThisRound;

        #endregion
        //────────────────────────────────────────────────────────────-

        //────────────────────────────────────────────────────────────-
        #region === RUNTIME: BLAST STATE ===

        private bool isBlastSequenceActive = false;
        private bool isBlastRotLocked = false;
        private int blastShotsDone = 0;

        #endregion
        //────────────────────────────────────────────────────────────-



        //─────────────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Khởi tạo ban đầu: cache component và tìm Player nếu cần.</summary>
        protected override void Start()
        {
            base.Start();

            InitializeComponents();
            InitializePlayer();
            DisableAllVisualEffects();

            // Cache HP snapshot để tính damage theo delta HP
            if (BossHealth != null)
            {
                _lastHpSnapshot = BossHealth.CurrentHealth;
                BossHealth.OnHealthChanged += HandleBossHealthChanged;
            }
        }

        private void OnDestroy()
        {
            if (BossHealth != null)
                BossHealth.OnHealthChanged -= HandleBossHealthChanged;
        }

        /// <summary>Override behavior boss (hiện chưa dùng).</summary>
        protected override void UpdateBossBehavior() { }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === INITIALIZATION ===

        /// <summary>Tự động gán Player từ PlayerHub nếu chưa set.</summary>
        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = PlayerHub.Instance.Player;
        }

        /// <summary>Cache DragonRobotAnimation và set visualRoot mặc định nếu trống.</summary>
        private void InitializeComponents()
        {
            dragonAnim = BossAnim as DragonRobotAnimation;
            zoneManager = PortalZoneManager.Instance;
            shieldControl = GetComponentInChildren<BossShieldController>(true);
            bossTelegraph = GetComponent<BossTelegraphGrowFromGround>();

            if (visualRoot == null)
                visualRoot = transform;
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === HEALTH / DAMAGE TRACKING ===

        /// <summary>Tính damage dựa trên thay đổi HP của boss.</summary>
        private void HandleBossHealthChanged(float hpPercent)
        {
            if (BossHealth == null) return;

            // Đã stop rồi thì không xử lý nữa
            if (_stopAttackingRequested)
            {
                _lastHpSnapshot = BossHealth.CurrentHealth;
                return;
            }

            if (_isDamageImmuneThisRound)
            {
                _lastHpSnapshot = BossHealth.CurrentHealth;
                return;
            }

            int currentHp = BossHealth.CurrentHealth;

            if (currentHp < _lastHpSnapshot)
            {
                int delta = _lastHpSnapshot - currentHp;
                _damageTakenWhileAttacking += delta;

                if (_damageTakenWhileAttacking >= stopAttackAfterDamage)
                    TriggerStaggerAndRetreat();
            }

            _lastHpSnapshot = currentHp;
        }


        /// <summary>Dừng toàn bộ chuỗi tấn công khi đạt ngưỡng damage.</summary>
        private void RequestStopAttacking()
        {
            if (_stopAttackingRequested) return;
            _stopAttackingRequested = true;

            if (attackTimeLimitRoutine != null)
            {
                StopCoroutine(attackTimeLimitRoutine);
                attackTimeLimitRoutine = null;
            }

            // Stop attack coroutine
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            ResetBlastState();
            DisableAllVisualEffects();

            dragonAnim?.SetFlameThrower(false);
            dragonAnim?.SetMeteorRain(false);
        }

        /// <summary>
        /// Khi đủ ngưỡng damage: dừng attack + play take damage, retreat sẽ do Animation Event gọi.
        /// </summary>
        private void TriggerStaggerAndRetreat()
        {
            if (_stopAttackingRequested) return;

            _isDamageImmuneThisRound = true;
            _pendingRetreatAfterTakeDamage = true;

            RequestStopAttacking();

            dragonAnim?.PlayTakeDamage();
        }

        /// <summary>
        /// Force the same stagger flow as "take 20 damage" — used when Phase 1 HP reaches 0.
        /// </summary>
        public void ForceStaggerAndRetreatForPhaseBreak()
        {
            if (_stopAttackingRequested) return;
            if (_isRetreating || _isShieldRecharging) return;

            TriggerStaggerAndRetreat();
        }


        /// <summary>Animation Event: TakeDamage xong thì boss retreat về entry point.</summary>
        public void OnTakeDamageRetreatFromAnimation()
        {
            if (!_pendingRetreatAfterTakeDamage) return;

            _pendingRetreatAfterTakeDamage = false;
            RetreatToCurrentZoneEntryPoint();
        }


        /// <summary>
        /// Bay về bossEntryPoint của current zone (KHÔNG tự restart attack).
        /// </summary>
        private void RetreatToCurrentZoneEntryPoint()
        {
            if (_isRetreating) return;
            _isRetreating = true;

            if (zoneManager == null) { _isRetreating = false; return; }

            Transform entry = zoneManager.GetCurrentZoneBossEntryPoint();
            if (entry == null) return;

            transform.DOKill();
            if (visualRoot != null) visualRoot.DOKill();

            Vector3 start = transform.position;
            Vector3 end = entry.position;

            float distance = Vector3.Distance(start, end);
            if (distance <= 0.001f) return;

            dragonAnim?.SetMoving(true);

            float unitsPerSecond = baseMoveSpeed + distanceSpeedFactor * distance;
            unitsPerSecond = Mathf.Clamp(unitsPerSecond, minUnitsPerSecond, maxUnitsPerSecond);

            transform.DOMove(end, unitsPerSecond)
                     .SetSpeedBased(true)
                     .SetEase(moveEase)
                     .OnComplete(() =>
                     {
                         dragonAnim?.SetMoving(false);

                         Vector3 lookPoint = zoneManager.GetCurrentZoneFacingPoint();
                         FaceTowards(lookPoint);

                         bool force = false;

                         if (zoneManager.IsCurrentZoneCorrectZone())
                             force = true;

                         BeginShieldRechargeAfterRetreat(force);
                         _isRetreating = false;
                     });
        }

        /// <summary>
        /// Về tới entry point xong: bật anim shield=true, gọi shield hồi đầy.
        /// Đầy shield thì anim shield=false.
        /// </summary>
        private void BeginShieldRechargeAfterRetreat(bool forceRechargeToFull)
        {
            if (shieldControl == null) return;
            if (_isShieldRecharging) return;

            bool needRecharge = forceRechargeToFull
                ? !shieldControl.IsFull
                : !shieldControl.IsActive;

            if (!needRecharge)
            {
                dragonAnim?.SetShield(false);
                _isDamageImmuneThisRound = false;

                if (zoneManager != null)
                    zoneManager.RunZoneTransition();

                return;
            }

            _isShieldRecharging = true;
            _isDamageImmuneThisRound = true;
            dragonAnim?.SetShield(true);

            shieldControl.StartRechargeToFull(2f, () =>
            {
                if (!shieldControl.IsActive)
                    shieldControl.Enable(false);

                dragonAnim?.SetShield(false);
                _isDamageImmuneThisRound = false;

                _isShieldRecharging = false;

                if (zoneManager != null)
                    zoneManager.RunZoneTransition();
            });
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === MOVEMENT / AUTO TURN ===

        /// <summary>
        /// Di chuyển đến target:
        /// - Xa thì đi nhanh hơn, gần thì chậm lại (clamp min/max).
        /// - Bật anim di chuyển nếu chủ yếu đi ngang.
        /// - Khi tới nơi → xoay mặt về currentZone → nếu KHÔNG phải return zone thì mới tấn công.
        /// </summary>
        public void MoveToEntryPoint(Transform target)
        {
            if (target == null) return;

            Vector3 start = transform.position;
            Vector3 end = target.position;

            float distance = Vector3.Distance(start, end);
            if (distance <= 0.001f) return;

            float deltaX = end.x - start.x;
            float deltaY = end.y - start.y;

            bool hasHorizontalMove = Mathf.Abs(deltaX) > horizontalAnimThreshold;
            bool horizontalDominant = hasHorizontalMove && Mathf.Abs(deltaX) >= Mathf.Abs(deltaY);

            // Trong lúc bay: nhìn về điểm đến
            FaceTowards(end);

            // Bật / tắt anim di chuyển theo hướng chính
            if (horizontalDominant)
                dragonAnim?.SetMoving(true);
            else
                dragonAnim?.SetMoving(false);

            // Tính tốc độ theo khoảng cách (clamp trong min/max)
            float unitsPerSecond = baseMoveSpeed + distanceSpeedFactor * distance;
            unitsPerSecond = Mathf.Clamp(unitsPerSecond, minUnitsPerSecond, maxUnitsPerSecond);

            // Tween di chuyển speed-based
            transform.DOMove(end, unitsPerSecond)
                     .SetSpeedBased(true)
                     .SetEase(moveEase)
                     .OnComplete(() =>
                     {
                         // Dừng anim di chuyển
                         dragonAnim?.SetMoving(false);

                         // Nếu đang ở return zone → không tấn công
                         bool shouldAttack = true;
                         if (zoneManager != null && zoneManager.IsCurrentZoneReturnZone())
                             shouldAttack = false;

                         if (zoneManager != null)
                         {
                             // Xoay về hướng currentZone
                             Vector3 lookPoint = zoneManager.GetCurrentZoneFacingPoint();
                             Tween rotateTween = FaceTowards(lookPoint);

                             // Chỉ gắn attack nếu được phép tấn công
                             if (shouldAttack && !_stopAttackingRequested)
                             {
                                 if (rotateTween != null)
                                     rotateTween.OnComplete(StartRandomAttack);
                                 else
                                     StartRandomAttack();
                             }
                         }

                         else
                         {
                             // Không có zoneManager → xem như zone thường
                             if (shouldAttack)
                                 StartRandomAttack();
                         }
                     });
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === FACING / LOOK AT TARGET ===

        /// <summary>
        /// Xoay visualRoot để nhìn về target:
        /// - Dùng xoay 3D theo Y hoặc flip X.
        /// - Trả về Tween xoay để bắt OnComplete nếu cần.
        /// </summary>
        private Tween FaceTowards(Vector3 targetPos, bool keepUpright = true)
        {
            if (visualRoot == null) return null;

            Vector3 dir = targetPos - visualRoot.position;

            if (useYRotation)
            {
                if (keepUpright)
                    dir.y = 0f;

                if (dir.sqrMagnitude < 0.0001f) return null;

                dir.Normalize();

                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

                visualRoot.DOKill();
                return visualRoot.DORotateQuaternion(targetRot, turnDuration);
            }
            else
            {
                float dirX = dir.x;
                if (Mathf.Approximately(dirX, 0f)) return null;

                Vector3 s = visualRoot.localScale;
                s.x = dirX > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                visualRoot.localScale = s;

                return null;
            }
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === ATTACK LOGIC (CHUNG) ===

        /// <summary>Bật/tắt GameObject effect phun lửa (VFX).</summary>
        private void SetFlameEffectActive(bool isActive)
        {
            if (flameEffectObject == null) return;
            flameEffectObject.SetActive(isActive);
        }

        /// <summary>
        /// Bật/tắt GameObject effect Meteor (VFX trên trời rơi xuống).
        /// </summary>
        private void SetMeteorEffectActive(bool isActive)
        {
            if (meteorEffectObject == null) return;
            meteorEffectObject.SetActive(isActive);
        }


        /// <summary>
        /// Bật/tắt GameObject effect Meteor Rain.
        /// </summary>
        private void SetMeteorRainEffectActive(bool isActive)
        {
            if (meteorRainEffectObject == null) return;
            meteorRainEffectObject.SetActive(isActive);
        }

        /// <summary>
        /// Bật/tắt effect cảnh báo vùng rơi Meteor, đồng thời set vị trí (nếu truyền vào).
        /// </summary>
        private void SetMeteorWarningActive(bool isActive, Vector3? worldPos = null)
        {
            if (meteorWarningEffect == null) return;

            if (worldPos.HasValue)
                meteorWarningEffect.transform.position = worldPos.Value;

            meteorWarningEffect.SetActive(isActive);
        }

        /// <summary>
        /// Bật/tắt effect cảnh báo vùng rơi Meteor Rain,
        /// đồng thời set vị trí (nếu truyền vào).
        /// </summary>
        private void SetMeteorRainWarningActive(bool isActive, Vector3? worldPos = null)
        {
            if (meteorRainWarningEffect == null) return;

            if (worldPos.HasValue)
                meteorRainWarningEffect.transform.position = worldPos.Value;

            meteorRainWarningEffect.SetActive(isActive);
        }

        /// <summary>
        /// Tắt toàn bộ effect VFX của Dragon khi mới vào game / reset.
        /// </summary>
        private void DisableAllVisualEffects()
        {
            // Flame
            SetFlameEffectActive(false);

            // Blast (flash)
            if (blastFlashEffect != null)
                blastFlashEffect.SetActive(false);

            // Meteor
            SetMeteorEffectActive(false);
            SetMeteorWarningActive(false);

            // Meteor Rain
            SetMeteorRainEffectActive(false);
            SetMeteorRainWarningActive(false);
        }

        public void PrepareForNewZoneAttackCycle()
        {
            // Stop timer/attack cũ nếu còn
            if (attackTimeLimitRoutine != null)
            {
                StopCoroutine(attackTimeLimitRoutine);
                attackTimeLimitRoutine = null;
            }

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            // Reset các cờ gây kẹt
            _stopAttackingRequested = false;
            _pendingRetreatAfterTakeDamage = false;
            _isShieldRecharging = false;
            _isRetreating = false;

            // Reset state combat
            _damageTakenWhileAttacking = 0;

            if (BossHealth != null)
                _lastHpSnapshot = BossHealth.CurrentHealth;

            ResetBlastState();
            DisableAllVisualEffects();

            dragonAnim?.SetFlameThrower(false);
            dragonAnim?.SetMeteorRain(false);
        }


        /// <summary>
        /// Bắt đầu chuỗi tấn công: dừng attack cũ (nếu đang chạy) rồi chạy RandomAttackRoutine.
        /// </summary>
        private void StartRandomAttack()
        {
            // Nếu đang retreat / đang hồi shield thì không bắt đầu đánh
            if (_isRetreating || _isShieldRecharging) return;

            // Cho phép bắt đầu vòng mới
            _stopAttackingRequested = false;

            // Reset attack selection bag each new cycle (avoid repeats)
            _skillBag.Clear();
            _skillBagCount = -1;
            _lastSkillIndex = -1;

            _isShieldRecharging = false;
            _isRetreating = false;

            _damageTakenWhileAttacking = 0;
            _isDamageImmuneThisRound = false;

            if (BossHealth != null)
                _lastHpSnapshot = BossHealth.CurrentHealth;

            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            attackRoutine = StartCoroutine(AttackLoopRoutine());
            StartAttackTimeLimitByPortal();
        }



        private void StartAttackTimeLimitByPortal()
        {
            // Stop timer cũ nếu có
            if (attackTimeLimitRoutine != null)
            {
                StopCoroutine(attackTimeLimitRoutine);
                attackTimeLimitRoutine = null;
            }

            float duration = attackDurationWrongPortal;

            if (zoneManager != null && zoneManager.IsCurrentZoneCorrectPortal())
                duration = attackDurationCorrectPortal;

            duration = Mathf.Max(0f, duration);
            if (duration <= 0f) return;

            attackTimeLimitRoutine = StartCoroutine(AttackTimeLimitRoutine(duration));
        }


        private IEnumerator AttackTimeLimitRoutine(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            // Nếu đã dừng vì damage trước đó thì thôi
            if (_stopAttackingRequested) yield break;

            // Dừng tấn công + retreat giống logic take-damage (nhưng không play take damage)
            RequestStopAttacking();
            RetreatToCurrentZoneEntryPoint();
        }


        private int GetNextSkillIndex(int skillCount)
        {
            if (skillCount <= 1) return 0;

            if (_skillBagCount != skillCount || _skillBag.Count == 0)
            {
                _skillBag.Clear();
                for (int i = 0; i < skillCount; i++)
                    _skillBag.Add(i);

                for (int i = 0; i < _skillBag.Count; i++)
                {
                    int j = Random.Range(i, _skillBag.Count);
                    (_skillBag[i], _skillBag[j]) = (_skillBag[j], _skillBag[i]);
                }

                if (_skillBag.Count > 1 && _skillBag[0] == _lastSkillIndex)
                {
                    int swapIndex = Random.Range(1, _skillBag.Count);
                    (_skillBag[0], _skillBag[swapIndex]) = (_skillBag[swapIndex], _skillBag[0]);
                }

                _skillBagCount = skillCount;
            }

            int next = _skillBag[0];
            _skillBag.RemoveAt(0);
            _lastSkillIndex = next;
            return next;
        }


        /// <summary>
        /// Coroutine random skill:
        /// - Chờ attackStartDelay.
        /// - Random skill dựa trên totalSkillCount.
        /// - Thực thi routine skill tương ứng.
        /// </summary>
        /// <summary>Boss liên tục dùng skill theo vòng lặp cho đến khi player gây đủ damage.</summary>
        private IEnumerator AttackLoopRoutine()
        {
            yield return new WaitForSeconds(attackStartDelay);

            while (!_stopAttackingRequested)
            {
                // 1. Lấy số skill hợp lệ theo phase
                int skillCount = GetAllowedSkillCountByPhase();

                // 2. Random chiêu trong range cho phép
                int index = GetNextSkillIndex(skillCount);

                switch (index)
                {
                    case 0:
                        yield return StartCoroutine(FlameThrowerRoutine());
                        break;

                    case 1:
                        yield return StartCoroutine(BlastAttackRoutine());
                        break;

                    case 2:
                        yield return StartCoroutine(MeteorAttackRoutine());
                        break;

                    case 3:
                        yield return StartCoroutine(MeteorRainAttackRoutine());
                        break;
                }

                // 3. Nếu player gây đủ 20 damage trong lúc đang cast → dừng
                if (_stopAttackingRequested)
                    break;

                // nghỉ 1 frame cho ổn định
                yield return null;
            }

            attackRoutine = null;
        }


        /// <summary>Giới hạn số skill theo phase (Phase 1 không có MeteorRain).</summary>
        private int GetAllowedSkillCountByPhase()
        {
            // BossHealth.currentPhase là 0-based: 0 = Phase 1, 1 = Phase 2...
            int phaseIndex = (BossHealth != null) ? BossHealth.currentPhase : 0;

            // Phase 1 (index 0): tối đa 3 skill (Flame, Blast, Meteor)
            // Phase 2 trở đi: tối đa 4 skill (thêm Meteor Rain)
            int phaseCap = (phaseIndex >= 1) ? 4 : 3;

            return Mathf.Clamp(totalSkillCount, 1, phaseCap);
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === FLAME THROWER ROUTINE ===

        /// <summary>
        /// Routine Flame Thrower:
        /// - Lấy flameCastPoint + bossEntryPoint từ PortalZone hiện tại.
        /// - Di chuyển nhẹ tới flameCastPoint (nếu có), không xoay, không anim chạy.
        /// - Bật animation Flame, chờ flameAttackDuration.
        /// - Tắt Flame + VFX.
        /// - Di chuyển nhẹ nhàng về bossEntryPoint (nếu có).
        /// </summary>
        private IEnumerator FlameThrowerRoutine()
        {
            // Lấy point cast + point quay về từ zone hiện tại
            Transform flameCastPoint = null;
            Transform bossEntryPoint = null;

            if (zoneManager != null)
            {
                flameCastPoint = zoneManager.GetCurrentZoneFlameCastPoint();
                bossEntryPoint = zoneManager.GetCurrentZoneBossEntryPoint();
            }

            // 1. Di chuyển tới điểm cast flame (nếu có)
            if (flameCastPoint != null)
            {
                dragonAnim?.SetMoving(false); // Không anim chạy
                transform.DOKill();           // Ngắt tween cũ nếu có

                float duration = flameMoveDuration > 0f ? flameMoveDuration : 0.5f;

                Tween goTween = transform
                    .DOMove(flameCastPoint.position, duration)
                    .SetEase(Ease.InOutSine);

                // Không xoay, không FaceTowards ở đoạn này
                yield return goTween.WaitForCompletion();
            }

            // 2. Bật animation Flame
            if (dragonAnim != null)
                dragonAnim.SetFlameThrower(true);

            bossTelegraph.PlayTelegraph();

            // Thời gian duy trì chiêu (VFX bật bằng Animation Event)
            yield return new WaitForSeconds(flameAttackDuration);

            // 3. Tắt animation Flame + đảm bảo tắt VFX
            if (dragonAnim != null)
                dragonAnim.SetFlameThrower(false);

            SetFlameEffectActive(false);

            // 4. Di chuyển về lại BossEntryPoint (nếu có)
            if (bossEntryPoint != null)
            {
                transform.DOKill();

                float duration = flameMoveDuration > 0f ? flameMoveDuration : 0.5f;

                Tween backTween = transform
                    .DOMove(bossEntryPoint.position, duration)
                    .SetEase(Ease.InOutSine);

                yield return backTween.WaitForCompletion();
            }
        }

        /// <summary>
        /// Gọi từ Animation Event: bật VFX phun lửa đúng frame trong clip.
        /// </summary>
        public void StartFlameThrowerFromAnimation()
        {
            if (_stopAttackingRequested) return;

            SetFlameEffectActive(true);

            bossTelegraph.StopTelegraph();
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === BLAST ATTACK (BOSSFIREBALL) ===

        /// <summary>
        /// Routine Blast Attack:
        /// - Lấy blastCastPoint + bossEntryPoint từ PortalZone hiện tại.
        /// - Bay chậm tới blastCastPoint (không xoay, không anim chạy).
        /// - Ở điểm cast: chạy chuỗi 3 phát Blast (ngắm → anim → event bắn).
        /// - Bắn xong 3 quả → bay về lại bossEntryPoint.
        /// </summary>
        private IEnumerator BlastAttackRoutine()
        {
            // Thiếu player hoặc anim → fallback sang Flame
            if (player == null || dragonAnim == null)
            {
                yield return StartCoroutine(FlameThrowerRoutine());
                yield break;
            }

            // Lấy điểm cast + điểm quay về từ zone hiện tại
            Transform blastCastPoint = null;
            Transform bossEntryPoint = null;

            if (zoneManager != null)
            {
                blastCastPoint = zoneManager.GetCurrentZoneBlastCastPoint();
                bossEntryPoint = zoneManager.GetCurrentZoneBossEntryPoint();
            }

            // 1) Bay tới blastCastPoint (nếu có) – nhẹ, không xoay, không anim chạy
            dragonAnim.SetMoving(false);
            transform.DOKill();

            if (blastCastPoint != null)
            {
                float moveDuration = flameMoveDuration > 0f ? flameMoveDuration : 0.5f;

                Tween toCastTween = transform
                    .DOMove(blastCastPoint.position, moveDuration)
                    .SetEase(Ease.InOutSine);

                // Không FaceTowards ở đoạn này → không xoay khi bay tới điểm cast
                yield return toCastTween.WaitForCompletion();
            }

            // 2) Setup state chuỗi blast tại vị trí cast
            isBlastSequenceActive = true;
            isBlastRotLocked = false;
            blastShotsDone = 0;

            // Bắt đầu phát thứ nhất (BeginNextBlastShot lo ngắm + play anim)
            BeginNextBlastShot();

            // Chờ cho tới khi chuỗi blast kết thúc
            yield return new WaitUntil(() => !isBlastSequenceActive);

            // 3) Blast xong → bay về lại bossEntryPoint (nếu có)
            if (bossEntryPoint != null)
            {
                transform.DOKill();
                if (visualRoot != null) visualRoot.DOKill();

                float returnDuration = flameMoveDuration > 0f ? flameMoveDuration : 0.5f;

                // Vừa bay về, vừa xoay về hướng bossEntryPoint
                Tween backTween = transform
                    .DOMove(bossEntryPoint.position, returnDuration)
                    .SetEase(Ease.InOutSine);

                if (visualRoot != null)
                    visualRoot.DORotateQuaternion(bossEntryPoint.rotation, returnDuration);

                yield return backTween.WaitForCompletion();
            }
        }

        /// <summary>
        /// Bắt đầu 1 phát Blast mới:
        /// - Đủ số phát → kết thúc chuỗi.
        /// - Chưa đủ → chạy coroutine ngắm Player rồi play animation.
        /// </summary>
        private void BeginNextBlastShot()
        {
            if (!isBlastSequenceActive)
                return;

            if (blastShotsDone >= blastFireballCount)
            {
                // Đã đủ số phát → kết thúc chuỗi blast
                isBlastSequenceActive = false;
                return;
            }

            // Bắt đầu 1 vòng: ngắm → anim → event bắn
            StartCoroutine(BeginBlastShotAfterAim());
        }

        /// <summary>
        /// Pha "ngắm": xoay theo Player trong blastAimDuration,
        /// sau đó bắt đầu animation Blast và lock xoay.
        /// </summary>
        private IEnumerator BeginBlastShotAfterAim()
        {
            float timer = 0f;

            // Pha ngắm – cho phép xoay về Player
            while (timer < blastAimDuration && isBlastSequenceActive)
            {
                if (!isBlastRotLocked && player != null)
                    FaceTowards(player.transform.position);

                timer += Time.deltaTime;
                yield return null;
            }

            if (!isBlastSequenceActive)
                yield break;

            // Bắt đầu animation Blast cho phát này: từ đây không cho xoay nữa
            isBlastRotLocked = true;
            dragonAnim?.PlayBlastAttack();

            // Optional: chờ ước chừng thời gian animation 1 phát
            float animTimer = 0f;
            while (animTimer < blastShotAnimDuration && isBlastSequenceActive)
            {
                // Trong lúc anim: không xoay vì isBlastRotLocked = true
                animTimer += Time.deltaTime;
                yield return null;
            }
        }

        /// <summary>
        /// Animation Event: bắn ra 1 quả cầu lửa cho Blast,
        /// bật flash effect và tăng số phát đã bắn.
        /// </summary>
        public void ShootBlastFireballFromAnimation()
        {
            if (_stopAttackingRequested) return;

            if (blastFlashEffect != null)
                blastFlashEffect.SetActive(true);

            SpawnFireballAtPlayer();
            blastShotsDone++;
        }

        /// <summary>
        /// Animation Event ở cuối motion bắn:
        /// - Mở lại xoay về Player.
        /// - Chưa đủ 3 phát → chuẩn bị phát tiếp.
        /// - Đủ 3 phát → kết thúc chuỗi blast.
        /// </summary>
        public void OnBlastShotEndFromAnimation()
        {
            // Cho phép xoay lại về Player
            isBlastRotLocked = false;

            if (!isBlastSequenceActive)
                return;

            // Đã bắn đủ → kết thúc chuỗi blast
            if (blastShotsDone >= blastFireballCount)
                isBlastSequenceActive = false;

            // Chưa đủ → ngắm + bắn phát tiếp theo
            else BeginNextBlastShot();
        }

        /// <summary>
        /// Spawn 1 BossFireball bay theo hướng fireballSpawnPoint (forward).
        /// </summary>
        private void SpawnFireballAtPlayer()
        {
            if (fireballPrefab == null || fireballSpawnPoint == null)
                return;

            BossFireball fireball = null;

            // Ưu tiên lấy từ PoolManager
            var pooled = PoolManager.Instance.ReuseComponent(
                fireballPrefab.gameObject,
                fireballSpawnPoint.position,
                fireballSpawnPoint.rotation
            );
            fireball = pooled as BossFireball;

            // Nếu pool chưa có thì Instantiate mới
            if (fireball == null)
            {
                GameObject go = Instantiate(
                    fireballPrefab.gameObject,
                    fireballSpawnPoint.position,
                    fireballSpawnPoint.rotation
                );
                fireball = go.GetComponent<BossFireball>();
            }

            if (fireball != null)
            {
                // Setup target = fireballSpawnPoint (BossFireball sẽ dùng target.forward nếu bạn đã bật)
                fireball.SetupFromPool(fireballSpawnPoint, this);
            }
        }

        /// <summary>
        /// Reset toàn bộ state của Blast Attack (chống kẹt).
        /// </summary>
        private void ResetBlastState()
        {
            isBlastSequenceActive = false;
            isBlastRotLocked = false;
            blastShotsDone = 0;

            if (blastFlashEffect != null)
                blastFlashEffect.SetActive(false);
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === METEOR ATTACK ===

        /// <summary>
        /// Chuỗi xử lý skill Meteor:
        /// - Lấy danh sách meteorCastPoints từ PortalZone hiện tại.
        /// - Chọn ngẫu nhiên 3–4 điểm.
        /// - Bay lên cao, lượn qua từng điểm và cast Meteor.
        /// - Cuối cùng bay về vị trí/hướng ban đầu.
        /// </summary>
        private IEnumerator MeteorAttackRoutine()
        {
            Transform[] chosenPoints;
            float meteorHeight;
            Vector3 originalPos;
            Quaternion originalRot;

            // Chuẩn bị dữ liệu cần cho skill Meteor (zone, height, điểm random, vị trí gốc)
            if (!TryPrepareMeteorContext(out chosenPoints, out meteorHeight, out originalPos, out originalRot))
            {
                // Thiếu dữ liệu cần thiết → fallback sang Flame
                yield return StartCoroutine(FlameThrowerRoutine());
                yield break;
            }

            // 1) Bay lên cao: giữ nguyên XZ, đặt Y = meteorHeight
            yield return LiftToMeteorHeight(meteorHeight);

            // 2) Lần lượt bay tới từng điểm Meteor và tấn công
            foreach (var point in chosenPoints)
            {
                if (point == null) continue;
                yield return FlyAndStrikeMeteorAtPoint(point, meteorHeight);
            }

            // 3) Kết thúc Meteor → bay về vị trí/hướng ban đầu
            yield return ReturnFromMeteor(originalPos, originalRot);
        }

        /// <summary>
        /// Chuẩn bị context cho Meteor:
        /// - Lấy Zone hiện tại, meteorCastPoints, bossEntryPoint.
        /// - Lấy meteorHeightY.
        /// - Chọn 3–4 điểm random.
        /// - Lưu lại vị trí/hướng ban đầu.
        /// </summary>
        private bool TryPrepareMeteorContext(
            out Transform[] chosenPoints,
            out float meteorHeight,
            out Vector3 originalPos,
            out Quaternion originalRot)
        {
            chosenPoints = null;
            meteorHeight = 0f;
            originalPos = Vector3.zero;
            originalRot = Quaternion.identity;

            if (zoneManager == null)
                return false;

            // Lấy các điểm Meteor + bossEntryPoint
            Transform[] meteorPoints = zoneManager.GetCurrentZoneMeteorCastPoints();
            Transform bossEntryPoint = zoneManager.GetCurrentZoneBossEntryPoint();

            if (meteorPoints == null || meteorPoints.Length == 0)
                return false;

            // Lấy chiều cao bay từ Zone (meteorHeightY)
            meteorHeight = zoneManager.GetCurrentZoneMeteorHeightY();
            if (meteorHeight <= 0f)
            {
                // Nếu chưa set hoặc set sai → dùng Y hiện tại làm fallback
                meteorHeight = transform.position.y;
            }

            // Lưu vị trí gốc để quay về (ưu tiên bossEntryPoint)
            originalPos = bossEntryPoint != null ? bossEntryPoint.position : transform.position;
            originalRot = visualRoot != null ? visualRoot.rotation : transform.rotation;

            // Đảm bảo không còn tween cũ, tắt anim di chuyển
            dragonAnim?.SetMoving(false);
            transform.DOKill();
            if (visualRoot != null) visualRoot.DOKill();

            // Chọn ngẫu nhiên 3–4 điểm khác nhau
            int countToUse = Mathf.Clamp(Random.Range(3, 5), 1, meteorPoints.Length);
            chosenPoints = ShuffleAndTake(meteorPoints, countToUse);

            return true;
        }

        /// <summary>
        /// Bay thẳng lên tầm cao Meteor (chỉ đổi Y, giữ nguyên XZ).
        /// </summary>
        private IEnumerator LiftToMeteorHeight(float meteorHeight)
        {
            float duration = meteorMoveDuration > 0f ? meteorMoveDuration : 0.6f;

            Vector3 liftPos = new Vector3(
                transform.position.x,
                meteorHeight,
                transform.position.z
            );

            Tween liftTween = transform
                .DOMove(liftPos, duration)
                .SetEase(Ease.InOutSine);

            yield return liftTween.WaitForCompletion();
        }

        /// <summary>
        /// Bay tới một điểm Meteor, xoay mặt về target và play animation đánh Meteor.
        /// Dragon luôn đứng cách target đúng bằng meteorAttackRadius (trên mặt phẳng XZ),
        /// đồng thời bật effect cảnh báo tại vùng rơi meteor trong lúc cast.
        /// </summary>
        private IEnumerator FlyAndStrikeMeteorAtPoint(Transform targetPoint, float meteorHeight)
        {
            if (targetPoint == null)
                yield break;

            // Vị trí hiện tại và vị trí target (chiếu xuống mặt phẳng XZ)
            Vector3 fromPos = transform.position;

            Vector3 targetXZ = new Vector3(
                targetPoint.position.x,
                0f,
                targetPoint.position.z
            );

            Vector3 fromXZ = new Vector3(
                fromPos.x,
                0f,
                fromPos.z
            );

            // Hướng từ target ra Dragon (để đứng trên đường hiện tại nhưng đúng radius)
            Vector3 flatDir = fromXZ - targetXZ;

            // Nếu đang trùng/siêu gần target → chọn hướng fallback
            if (flatDir.sqrMagnitude < 0.0001f)
            {
                // Ưu tiên hướng nhìn hiện tại
                Vector3 fallback = visualRoot != null ? visualRoot.forward : transform.forward;
                fallback.y = 0f;

                if (fallback.sqrMagnitude < 0.0001f)
                    fallback = Vector3.forward; // fallback cuối

                flatDir = fallback;
            }

            flatDir.Normalize();

            // Vị trí đúng cách target một đoạn meteorAttackRadius trên mặt phẳng XZ
            Vector3 finalXZ = targetXZ + flatDir * meteorAttackRadius;

            Vector3 finalPos = new Vector3(
                finalXZ.x,
                meteorHeight,
                finalXZ.z
            );

            float moveDur = meteorMoveDuration > 0f ? meteorMoveDuration : 0.6f;

            // Cho xoay về hướng điểm đến trước khi bay
            FaceTowards(finalPos);

            Tween moveTween = transform
                .DOMove(finalPos, moveDur)
                .SetEase(Ease.InOutSine);

            yield return moveTween.WaitForCompletion();

            // ĐÃ ĐẾN VỊ TRÍ TẤN CÔNG → XOAY MẶT VỀ CHÍNH TARGET
            Vector3 facePoint = targetPoint.position;
            facePoint.y = meteorHeight; // nhìn ngang mặt phẳng bay
            FaceTowards(facePoint);

            // Bật effect cảnh báo tại vùng target (dùng vị trí targetPoint)
            // Nếu warning là vòng tròn dưới đất → giữ Y của targetPoint.
            SetMeteorWarningActive(
                true,
                new Vector3(
                    targetPoint.position.x,
                    targetPoint.position.y,
                    targetPoint.position.z
                )
            );

            // Cho xoay xong một chút để tạo cảm giác "ngắm"
            yield return new WaitForSeconds(1f);

            // Play animation Meteor Attack (Animation Events sẽ lo spawn meteor rơi)
            dragonAnim?.PlayMeteorAttack();

            // Chờ thời gian animation
            yield return new WaitForSeconds(meteorStrikeAnimDuration);
            yield return new WaitForSeconds(meteorBetweenPointsDelay);
        }


        /// <summary>
        /// Bay về vị trí và hướng ban đầu sau khi kết thúc toàn bộ skill Meteor.
        /// </summary>
        private IEnumerator ReturnFromMeteor(Vector3 originalPos, Quaternion originalRot)
        {
            transform.DOKill();
            if (visualRoot != null) visualRoot.DOKill();

            float returnDur = meteorMoveDuration > 0f ? meteorMoveDuration : 0.6f;

            Tween backTween = transform
                .DOMove(originalPos, returnDur)
                .SetEase(Ease.InOutSine);

            if (visualRoot != null)
                visualRoot.DORotateQuaternion(originalRot, returnDur);

            yield return backTween.WaitForCompletion();
        }

        /// <summary>
        /// Trộn ngẫu nhiên mảng points và lấy về tối đa count điểm đầu tiên.
        /// Dùng cho Meteor để chọn 3–4 điểm khác nhau.
        /// </summary>
        private Transform[] ShuffleAndTake(Transform[] source, int count)
        {
            // Không có dữ liệu thì trả mảng rỗng
            if (source == null || source.Length == 0)
                return System.Array.Empty<Transform>();

            // Số lượng thực tế cần lấy
            int takeCount = Mathf.Clamp(count, 1, source.Length);

            // Clone mảng để không phá mảng gốc
            Transform[] buffer = (Transform[])source.Clone();

            // Fisher–Yates shuffle một phần (chỉ cần shuffle takeCount phần tử đầu)
            for (int i = 0; i < takeCount; i++)
            {
                int swapIndex = Random.Range(i, buffer.Length);
                Transform tmp = buffer[i];
                buffer[i] = buffer[swapIndex];
                buffer[swapIndex] = tmp;
            }

            // Copy ra mảng kết quả với đúng số lượng cần
            Transform[] result = new Transform[takeCount];
            for (int i = 0; i < takeCount; i++)
                result[i] = buffer[i];

            return result;
        }

        /// <summary>
        /// Gọi từ Animation Event: bật VFX tạo meteor đúng frame trong clip.
        /// </summary>
        public void StartMeteorFromAnimation()
        {
            if (_stopAttackingRequested) return;

            // Tắt effect cảnh báo sau khi chiêu Meteor của điểm này kết thúc
            SetMeteorWarningActive(false, null);
            SetMeteorEffectActive(true);
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === METEOR RAIN ATTACK ===

        /// <summary>
        /// Chuỗi xử lý skill Meteor Rain:
        /// - Lấy danh sách meteorRainPoints từ PortalZone hiện tại.
        /// - Random tối đa 2 điểm.
        /// - Bay lên cao (meteorRainBossHeightY), lượn qua từng điểm và cast Meteor Rain.
        /// - Cuối cùng bay về vị trí/hướng ban đầu.
        /// </summary>
        private IEnumerator MeteorRainAttackRoutine()
        {
            Transform[] rainPoints;
            float rainHeight;
            Vector3 originalPos;
            Quaternion originalRot;

            // Chuẩn bị dữ liệu cần cho skill Meteor Rain
            if (!TryPrepareMeteorRainContext(out rainPoints, out rainHeight, out originalPos, out originalRot))
            {
                // Thiếu dữ liệu → fallback sang Flame
                yield return StartCoroutine(FlameThrowerRoutine());
                yield break;
            }

            // 1) Bay lên cao: giữ nguyên XZ, đặt Y = rainHeight
            yield return LiftToMeteorRainHeight(rainHeight);

            // 2) Lần lượt bay tới từng điểm Meteor Rain và tấn công (tối đa 2 lần)
            int hitsToUse = Mathf.Min(2, rainPoints.Length);
            for (int i = 0; i < hitsToUse; i++)
            {
                Transform point = rainPoints[i];
                if (point == null) continue;

                yield return FlyAndStrikeMeteorRainAtPoint(point, rainHeight);
            }

            // 3) Kết thúc Meteor Rain → bay về vị trí/hướng ban đầu
            yield return ReturnFromMeteor(originalPos, originalRot);
        }

        /// <summary>
        /// Chuẩn bị context cho Meteor Rain:
        /// - Lấy Zone hiện tại, meteorRainPoints, bossEntryPoint.
        /// - Lấy meteorRainBossHeightY.
        /// - Random tối đa 2 điểm từ danh sách.
        /// - Lưu lại vị trí/hướng ban đầu của Boss.
        /// </summary>
        private bool TryPrepareMeteorRainContext(
            out Transform[] chosenPoints,
            out float rainHeight,
            out Vector3 originalPos,
            out Quaternion originalRot)
        {
            chosenPoints = null;
            rainHeight = 0f;
            originalPos = Vector3.zero;
            originalRot = Quaternion.identity;

            if (zoneManager == null)
                return false;

            // Lấy các điểm Meteor Rain + bossEntryPoint
            Transform[] rainPoints = zoneManager.GetCurrentZoneMeteorRainPoints();
            Transform bossEntryPoint = zoneManager.GetCurrentZoneBossEntryPoint();

            if (rainPoints == null || rainPoints.Length == 0)
                return false;

            // Lấy chiều cao bay từ Zone (meteorRainBossHeightY)
            rainHeight = zoneManager.GetCurrentZoneMeteorRainBossHeightY();
            if (rainHeight < 0f)
            {
                // Nếu chưa set hoặc set sai → dùng Y hiện tại làm fallback
                rainHeight = transform.position.y;
            }

            // Lưu vị trí gốc để quay về (ưu tiên bossEntryPoint)
            originalPos = bossEntryPoint != null ? bossEntryPoint.position : transform.position;
            originalRot = visualRoot != null ? visualRoot.rotation : transform.rotation;

            // Đảm bảo không còn tween cũ, tắt anim di chuyển
            dragonAnim?.SetMoving(false);
            transform.DOKill();
            if (visualRoot != null) visualRoot.DOKill();

            // Chọn tối đa 2 điểm random từ danh sách
            int countToUse = Mathf.Clamp(2, 1, rainPoints.Length);
            chosenPoints = ShuffleAndTake(rainPoints, countToUse);

            return true;
        }

        /// <summary>
        /// Bay thẳng lên tầm cao Meteor Rain (chỉ đổi Y, giữ nguyên XZ).
        /// Dùng duration riêng của Meteor Rain.
        /// </summary>
        private IEnumerator LiftToMeteorRainHeight(float rainHeight)
        {
            float duration = meteorRainMoveDuration > 0f ? meteorRainMoveDuration : 0.6f;

            Vector3 liftPos = new Vector3(
                transform.position.x,
                rainHeight,
                transform.position.z
            );

            Tween liftTween = transform
                .DOMove(liftPos, duration)
                .SetEase(Ease.InOutSine);

            yield return liftTween.WaitForCompletion();
        }

        /// <summary>
        /// Bay tới một điểm Meteor Rain, bật warning và play animation Rain.
        /// Dragon đứng cách target đúng bằng meteorRainAttackRadius (trên mặt phẳng XZ).
        /// </summary>
        private IEnumerator FlyAndStrikeMeteorRainAtPoint(Transform targetPoint, float rainHeight)
        {
            if (targetPoint == null)
                yield break;

            // Vị trí hiện tại và target (chiếu xuống mặt phẳng XZ)
            Vector3 fromPos = transform.position;

            Vector3 targetXZ = new Vector3(
                targetPoint.position.x,
                0f,
                targetPoint.position.z
            );

            Vector3 fromXZ = new Vector3(
                fromPos.x,
                0f,
                fromPos.z
            );

            // Hướng từ target ra Dragon (để đứng trên đường hiện tại nhưng đúng radius)
            Vector3 flatDir = fromXZ - targetXZ;

            // Nếu đang trùng/siêu gần target → chọn hướng fallback
            if (flatDir.sqrMagnitude < 0.0001f)
            {
                // Ưu tiên hướng nhìn hiện tại
                Vector3 fallback = visualRoot != null ? visualRoot.forward : transform.forward;
                fallback.y = 0f;

                if (fallback.sqrMagnitude < 0.0001f)
                    fallback = Vector3.forward; // fallback cuối

                flatDir = fallback;
            }

            flatDir.Normalize();

            // Vị trí đúng cách target một đoạn meteorRainAttackRadius trên mặt phẳng XZ
            Vector3 finalXZ = targetXZ + flatDir * meteorRainAttackRadius;

            Vector3 finalPos = new Vector3(
                finalXZ.x,
                rainHeight,
                finalXZ.z
            );

            float moveDur = meteorRainMoveDuration > 0f ? meteorRainMoveDuration : 0.6f;

            // Cho xoay về hướng điểm đến trước khi bay
            FaceTowards(finalPos);

            Tween moveTween = transform
                .DOMove(finalPos, moveDur)
                .SetEase(Ease.InOutSine);

            yield return moveTween.WaitForCompletion();

            // ĐÃ ĐẾN VỊ TRÍ TẤN CÔNG → XOAY MẶT VỀ CHÍNH TARGET
            Vector3 facePoint = targetPoint.position;
            facePoint.y = rainHeight; // nhìn ngang mặt phẳng bay
            FaceTowards(facePoint);

            // Bật effect cảnh báo mưa meteor tại vùng target
            if (meteorRainWarningEffect != null)
            {
                meteorRainWarningEffect.transform.position = new Vector3(
                    targetPoint.position.x,
                    targetPoint.position.y,
                    targetPoint.position.z
                );
                meteorRainWarningEffect.SetActive(true);
            }

            // Cho xoay/nhắm một chút trước khi cast
            yield return new WaitForSeconds(1f);

            // Bật trạng thái animation Meteor Rain (bool trên Animator)
            dragonAnim?.SetMeteorRain(true);

            SetMeteorRainWarningActive(false, null);
            SetMeteorRainEffectActive(true);

            // Thời gian animation mưa meteor
            yield return new WaitForSeconds(meteorRainStrikeAnimDuration);

            // Tắt trạng thái animation
            dragonAnim?.SetMeteorRain(false);

            SetMeteorRainEffectActive(false);

            // Tắt warning sau khi kết thúc một lần cast
            if (meteorRainWarningEffect != null)
                meteorRainWarningEffect.SetActive(false);

            // Nghỉ giữa 2 lần tấn công Rain
            yield return new WaitForSeconds(meteorRainBetweenPointsDelay);
        }

        #endregion
        //─────────────────────────────────────────────────────────────
    }
}
