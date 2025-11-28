using DG.Tweening;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class DragonRobot : BossCore
    {
        //─────────────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Player Reference")]
        [SerializeField] private new Player player;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Movement Settings")]
        [SerializeField] private float baseMoveSpeed = 6f;           // Base speed (units/sec)
        [SerializeField] private float distanceSpeedFactor = 0.6f;   // Tốc độ + thêm theo distance
        [SerializeField] private float minUnitsPerSecond = 5f;       // Min speed
        [SerializeField] private float maxUnitsPerSecond = 25f;      // Max speed
        [SerializeField] private Ease moveEase = Ease.InOutSine;     // Tween ease for flying

        [Header("Visual / Facing Settings")]
        [Tooltip("Sprite/Model root. If null → use this transform.")]
        [SerializeField] private Transform visualRoot;
        [Tooltip("Use 3D Y rotation (LookAt) instead of flip scale.")]
        [SerializeField] private bool useYRotation = true;
        [Tooltip("Time to rotate when changing facing.")]
        [SerializeField] private float turnDuration = 0.2f;

        [Header("Animation Logic")]
        [Tooltip("Minimum |deltaX| để tính là di chuyển ngang.")]
        [SerializeField] private float horizontalAnimThreshold = 0.1f;

        #endregion
        //─────────────────────────────────────────────────────────────


        #region === RUNTIME ===

        private DragonRobotAnimation dragonAnim;

        #endregion


        //─────────────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        protected override void Start()
        {
            base.Start();
            InitializeComponents();
            InitializePlayer();
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === INITIALIZATION ===

        private void InitializePlayer()
        {
            if (player == null && autoFindPlayer)
                player = PlayerHub.Instance.Player;
        }

        private void InitializeComponents()
        {
            dragonAnim = BossAnim as DragonRobotAnimation;

            if (visualRoot == null)
                visualRoot = transform;
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === MOVE WITH AUTO TURN ===

        /// <summary>
        /// - Xa → chạy nhanh hơn, gần → chậm lại nhưng vẫn đủ nhanh.
        /// - Chỉ bật move anim khi chủ yếu đi ngang.
        /// - Trong lúc bay thì nhìn theo target.
        /// - Đến nơi xong → quay mặt về hướng currentZone (PortalZoneManager).
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

            // Chỉ bật move anim nếu di chuyển chủ yếu ngang
            if (horizontalDominant)
                dragonAnim?.SetMoving(true);

            else dragonAnim?.SetMoving(false);

            // SPEED: base + distance * factor, clamp trong min/max
            float unitsPerSecond = baseMoveSpeed + distance * distanceSpeedFactor;
            unitsPerSecond = Mathf.Clamp(unitsPerSecond, minUnitsPerSecond, maxUnitsPerSecond);

            // Di chuyển bằng DOTween với speed-based
            transform.DOMove(end, unitsPerSecond)
                     .SetSpeedBased(true)   // unitsPerSecond = units / second
                     .SetEase(moveEase)
                     .OnComplete(() =>
                     {
                         // Dừng move anim
                         dragonAnim?.SetMoving(false);

                         // Sau khi tới điểm bossEntryPoint → quay về hướng currentZone
                         var zoneManager = PortalZoneManager.Instance;
                         if (zoneManager != null)
                         {
                             Vector3 lookPoint = zoneManager.GetCurrentZoneFacingPoint();
                             FaceTowards(lookPoint);
                         }
                     });
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        //─────────────────────────────────────────────────────────────
        #region === FACE DIRECTION / LOOK AT TARGET ===

        /// <summary>
        /// Xoay visualRoot nhìn về targetPos.
        /// - useYRotation = true  → 3D LookAt (yaw only).
        /// - useYRotation = false → 2D-style flip trên X scale.
        /// </summary>
        private void FaceTowards(Vector3 targetPos, bool keepUpright = true)
        {
            if (visualRoot == null) return;

            Vector3 dir = targetPos - visualRoot.position;

            if (useYRotation)
            {
                if (keepUpright)
                    dir.y = 0f;

                if (dir.sqrMagnitude < 0.0001f) return;

                dir.Normalize();

                Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);

                visualRoot.DOKill();
                visualRoot.DORotateQuaternion(targetRot, turnDuration);
            }
            else
            {
                float dirX = dir.x;
                if (Mathf.Approximately(dirX, 0f)) return;

                Vector3 s = visualRoot.localScale;
                s.x = dirX > 0f ? Mathf.Abs(s.x) : -Mathf.Abs(s.x);
                visualRoot.localScale = s;
            }
        }

        #endregion
        //─────────────────────────────────────────────────────────────



        protected override void UpdateBossBehavior() { }
    }
}
