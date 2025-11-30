using DG.Tweening;
using UnityEngine;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// DragonRobot: di chuyển, xoay mặt và điều khiển các skill tấn công (Flame / Blast).
    /// </summary>
    public class DragonRobot : BossCore
    {
        //─────────────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Player Reference")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 6f;         // Tốc độ cơ bản (unit/giây)
        [SerializeField] private float distanceSpeedFactor = 0.6f; // Tốc độ cộng thêm theo khoảng cách
        [SerializeField] private float minUnitsPerSecond = 5f;     // Tốc độ tối thiểu
        [SerializeField] private float maxUnitsPerSecond = 25f;    // Tốc độ tối đa
        [SerializeField] private Ease moveEase = Ease.InOutSine;   // Độ mượt khi bay

        [Header("Visual / Facing Settings")]
        [SerializeField] private Transform visualRoot;             // Gốc hiển thị (model/sprite)
        [SerializeField] private bool useYRotation = true;         // True = xoay 3D theo Y, False = flip X
        [SerializeField] private float turnDuration = 0.2f;        // Thời gian xoay mặt

        [Header("Animation Logic")]
        [SerializeField] private float horizontalAnimThreshold = 0.1f; // Ngưỡng |deltaX| để tính là đi ngang

        [Header("Attack Settings")]
        [SerializeField] private int totalSkillCount = 2;          // Số lượng skill để random (0 = Flame, 1 = Blast)
        [SerializeField] private float attackStartDelay = 1f;      // Delay trước khi bắt đầu tấn công

        [Header("Flame Thrower")]
        [SerializeField] private float flameAttackDuration = 2f;   // Thời gian duy trì Flame Thrower
        [SerializeField] private float flameMoveDuration = 1.2f;   // Thời gian đi tới / về điểm cast Flame
        [SerializeField] private GameObject flameEffectObject;     // GameObject effect phun lửa

        [Header("Blast Attack")]
        [SerializeField] private BossFireball fireballPrefab;      // Prefab cầu lửa
        [SerializeField] private Transform fireballSpawnPoint;     // Vị trí spawn cầu lửa
        [SerializeField] private GameObject blastFlashEffect;      // Flash effect khi bắn cầu lửa
        [SerializeField] private int blastFireballCount = 3;       // Tổng số quả phải bắn
        [SerializeField] private float blastAimDuration = 0.5f;    // Thời gian ngắm trước mỗi phát
        [SerializeField] private float blastShotAnimDuration = 0.6f; // Thời gian ước chừng animation 1 phát

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private DragonRobotAnimation dragonAnim;
        private Coroutine attackRoutine;

        // Blast state
        private bool isBlastSequenceActive = false;   // Đang chạy chuỗi blast hay không
        private bool isBlastRotLocked = false;        // Có cho xoay về Player trong lúc blast không
        private int blastShotsDone = 0;               // Đã bắn được bao nhiêu quả trong chuỗi

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Khởi tạo ban đầu: cache component và tìm Player nếu cần.</summary>
        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
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

            if (visualRoot == null)
                visualRoot = transform;
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

                         var zoneManager = PortalZoneManager.Instance;

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
                             if (shouldAttack)
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
        /// Bắt đầu chuỗi tấn công: dừng attack cũ (nếu đang chạy) rồi chạy RandomAttackRoutine.
        /// </summary>
        private void StartRandomAttack()
        {
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }

            attackRoutine = StartCoroutine(RandomAttackRoutine());
        }

        /// <summary>
        /// Coroutine random skill:
        /// - Chờ attackStartDelay.
        /// - Random skill dựa trên totalSkillCount.
        /// - Thực thi routine skill tương ứng.
        /// </summary>
        private IEnumerator RandomAttackRoutine()
        {
            yield return new WaitForSeconds(attackStartDelay);

            int skillCount = Mathf.Max(1, totalSkillCount);
            int index = Random.Range(0, skillCount);

            switch (index)
            {
                case 0:
                    // Skill 0: Flame Thrower
                    yield return StartCoroutine(FlameThrowerRoutine());
                    break;

                case 1:
                    // Skill 1: Blast Attack (bắn 3 quả cầu lửa)
                    yield return StartCoroutine(BlastAttackRoutine());
                    break;

                default:
                    // Chưa có skill khác → fallback về Flame Thrower
                    yield return StartCoroutine(FlameThrowerRoutine());
                    break;
            }

            attackRoutine = null;
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
            var zoneManager = PortalZoneManager.Instance;

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

            // Thời gian duy trì chiêu (VFX bật bằng Animation Event)
            yield return new WaitForSeconds(flameAttackDuration);

            // 3. Tắt animation Flame + đảm bảo tắt VFX
            if (dragonAnim != null)
                dragonAnim.SetFlameThrower(false);

            SetFlameEffectActive(false);

            // 4. Di chuyển về lại bossEntryPoint (nếu có)
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
            SetFlameEffectActive(true);
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
            var zoneManager = PortalZoneManager.Instance;

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

        #endregion
        //─────────────────────────────────────────────────────────────

    }
}
