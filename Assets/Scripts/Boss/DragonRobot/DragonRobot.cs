using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        [SerializeField] private int totalSkillCount = 3;          // Số lượng skill để random (0 = Flame, 1 = Blast)
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

        [Header("Meteor Attack")]
        [SerializeField] private float meteorMoveDuration = 0.8f;            // Thời gian bay giữa các điểm
        [SerializeField] private float meteorAttackRadius = 30f;             // Bán kính đứng cách target
        [SerializeField] private float meteorStrikeAnimDuration = 1.0f;      // Thời gian anim meteor 1 lần
        [SerializeField] private float meteorBetweenPointsDelay = 1f;        // Delay nghỉ giữa 2 điểm
        [SerializeField] private GameObject meteorEffectObject;              // Effect VFX cho Meteor (spawn từ trên)
        [SerializeField] private GameObject meteorWarningEffect;             // Effect cảnh báo vùng rơi Meteor

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
            DisableAllVisualEffects();
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
        /// Bật/tắt GameObject effect Meteor (VFX trên trời rơi xuống).
        /// </summary>
        private void SetMeteorEffectActive(bool isActive)
        {
            if (meteorEffectObject == null) return;
            meteorEffectObject.SetActive(isActive);
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
        /// Tắt toàn bộ effect VFX của Dragon khi mới vào game / reset.
        /// </summary>
        private void DisableAllVisualEffects()
        {
            SetFlameEffectActive(false);
            SetMeteorEffectActive(false);

            if (blastFlashEffect != null)
                blastFlashEffect.SetActive(false);

            SetMeteorWarningActive(false);
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
                    yield return StartCoroutine(FlameThrowerRoutine());
                    break;

                case 1:
                    yield return StartCoroutine(BlastAttackRoutine());
                    break;

                case 2:
                    yield return StartCoroutine(MeteorAttackRoutine());
                    break;

                default:
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

            var zoneManager = PortalZoneManager.Instance;
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
            // Tắt effect cảnh báo sau khi chiêu Meteor của điểm này kết thúc
            SetMeteorWarningActive(false, null);
            SetMeteorEffectActive(true);
        }

        #endregion
        //─────────────────────────────────────────────────────────────

    }
}
