using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// 🔒 Giới hạn khu vực di chuyển hình tròn (tường vô hình trơn mượt).
    /// Player không thể vượt ra ngoài, nhưng có thể trượt quanh rìa mượt mà như Physic Material friction thấp.
    /// </summary>
    [DisallowMultipleComponent]
    public class MovementBoundaryZone : SingletonMonobehaviour<MovementBoundaryZone>
    {
        [Header("Boundary Settings")]
        [SerializeField] private float boundaryRadius = 12f;
        [SerializeField] private Transform zoneCenter;

        [Header("Physical Behavior")]
        [SerializeField, Tooltip("Độ bật nhẹ khi va biên (tăng cảm giác phản lực)")]
        private float boundaryForce = 10f;

        [SerializeField, Tooltip("Giảm ma sát khi trượt quanh rìa (1 = trượt tự do)")]
        private float friction = 0.95f;

        [SerializeField, Tooltip("Khoảng đệm nhỏ bên trong để tránh jitter")]
        private float innerPadding = 0.05f;

        [Header("Activation Control")]
        [SerializeField, Tooltip("Có cần kích hoạt bằng trigger hay luôn bật")]
        private bool activateByTrigger = true;
        private bool isActive = false;

        [Header("Debug")]
        [SerializeField] private bool showGizmos = true;
        [SerializeField] private Color zoneColor = new Color(0.3f, 0.7f, 1f, 0.15f);

        private Player playerInside;

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            playerInside = PlayerHub.Instance.Player;

            if (zoneCenter == null)
                zoneCenter = transform;
        }

        private void Update()
        {
            // Nếu chưa kích hoạt thì không giới hạn
            if (!isActive || playerInside == null)
                return;

            // Kiểm tra và giới hạn chuyển động
            ApplyCircularPhysicsConstraint();
        }

        #endregion
        //─────────────────────────────────────────────
        #region === PUBLIC CONTROL ===

        /// <summary>
        /// 🔓 Kích hoạt vùng giới hạn (gọi từ BossEncounterCutscene hoặc sự kiện).
        /// </summary>
        public void ActivateBoundary() => isActive = true;

        /// <summary>
        /// Trả về bán kính giới hạn (cho script khác như SoldierRobot dùng).
        /// </summary>
        public float GetBoundaryRadius() => boundaryRadius;

        #endregion
        //─────────────────────────────────────────────
        #region === CORE PHYSICS CONSTRAINT ===

        /// <summary>
        /// Giới hạn di chuyển trong vòng tròn — Player trượt quanh rìa thay vì bị kẹt.
        /// </summary>
        private void ApplyCircularPhysicsConstraint()
        {
            Vector3 playerPos = playerInside.transform.position;
            Vector3 toPlayer = playerPos - zoneCenter.position;
            float distance = toPlayer.magnitude;
            float maxRadius = boundaryRadius - innerPadding;

            // Nếu Player vượt ra khỏi ranh giới
            if (distance > maxRadius)
            {
                Vector3 normal = toPlayer.normalized;

                // Đưa Player sát trong rìa (tránh jitter)
                Vector3 correctedPos = zoneCenter.position + normal * maxRadius;

                // Di chuyển mượt về rìa (nếu tốc độ cao)
                playerInside.transform.position = Vector3.Lerp(
                    playerPos,
                    correctedPos,
                    Time.deltaTime * boundaryForce
                );

                // Vận tốc hiện tại
                Vector3 velocity = playerInside.velocity;

                // Loại bỏ hướng đi ra ngoài (vuông góc tường)
                float outwardSpeed = Vector3.Dot(velocity, normal);
                if (outwardSpeed > 0f)
                    velocity -= normal * outwardSpeed;

                // Giữ lại vận tốc tiếp tuyến và thêm ma sát
                velocity *= friction;

                // Cộng phản lực nhẹ để Player cảm nhận có tường
                velocity -= normal * boundaryForce * 0.05f * Time.deltaTime;

                playerInside.velocity = velocity;
            }
        }

        #endregion
        //─────────────────────────────────────────────
        #region === GIZMOS ===

        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = zoneColor;
            Vector3 center = zoneCenter != null ? zoneCenter.position : transform.position;
            Gizmos.DrawWireSphere(center, boundaryRadius);
        }

        #endregion
    }
}
