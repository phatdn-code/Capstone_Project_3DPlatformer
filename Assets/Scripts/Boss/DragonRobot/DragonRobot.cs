using DG.Tweening;
using UnityEngine;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// DragonRobot: di chuyển, xoay mặt và điều khiển skill tấn công (Flame Thrower).
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
        [SerializeField] private int totalSkillCount = 1;          // Số lượng skill để random
        [SerializeField] private float attackStartDelay = 1f;      // Delay trước khi bắt đầu tấn công

        [Header("Flame Thrower")]
        [SerializeField] private float flameAttackDuration = 2f;   // Thời gian duy trì Flame Thrower
        [SerializeField] private GameObject flameEffectObject;     // GameObject effect phun lửa

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private DragonRobotAnimation dragonAnim;
        private Coroutine attackRoutine;

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

        /// <summary>Tự động gán Player từ PlayerHub nếu chưa set trong Inspector.</summary>
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

                                 else StartRandomAttack();
                             }
                         }
                     });
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === FACING / LOOK AT TARGET ===

        /// <summary>
        /// Xoay visualRoot để nhìn về vị trí target:
        /// - Dùng xoay 3D theo Y hoặc flip X tùy cài đặt.
        /// - Trả về Tween xoay (nếu dùng DOTween rotation) để bắt OnComplete bên ngoài.
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
        #region === ATTACK LOGIC / FLAME THROWER ===

        /// <summary>Bật/tắt GameObject effect phun lửa (VFX).</summary>
        private void SetFlameEffectActive(bool isActive)
        {
            if (flameEffectObject == null) return;
            flameEffectObject.SetActive(isActive);
        }

        /// <summary>
        /// Bắt đầu chuỗi tấn công: dừng attack cũ (nếu đang chạy)
        /// rồi khởi chạy RandomAttackRoutine.
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
        /// - Chờ attackStartDelay giây.
        /// - Random 1 skill dựa trên totalSkillCount.
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

                default:
                    // Chưa có skill khác → fallback về Flame Thrower
                    yield return StartCoroutine(FlameThrowerRoutine());
                    break;
            }

            attackRoutine = null;
        }

        /// <summary>
        /// Routine Flame Thrower:
        /// - Bật bool animation Flame.
        /// - Tắt bool Flame + tắt VFX nếu đang bật.
        /// </summary>
        private IEnumerator FlameThrowerRoutine()
        {
            if (dragonAnim != null)
                dragonAnim.SetFlameThrower(true);

            yield return new WaitForSeconds(flameAttackDuration);

            if (dragonAnim != null)
                dragonAnim.SetFlameThrower(false);

            SetFlameEffectActive(false);
        }

        /// <summary>
        /// Hàm gọi từ Animation Event: bắt đầu VFX phun lửa khi clip tới frame phun.
        /// </summary>
        public void StartFlameThrowerFromAnimation()
        {
            SetFlameEffectActive(true);
        }

        #endregion
        //─────────────────────────────────────────────────────────────
    }
}
