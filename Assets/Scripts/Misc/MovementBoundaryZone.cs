using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// 🔒 Giới hạn khu vực di chuyển của Player trong bán kính nhất định.
    /// Khi Player cố ra ngoài, hệ thống sẽ đẩy ngược lại và khóa input tạm thời.
    /// </summary>
    [DisallowMultipleComponent]
    public class MovementBoundaryZone : SingletonMonobehaviour<MovementBoundaryZone>
    {
        //─────────────────────────────────────────────
        #region ✦ BIẾN CÀI ĐẶT ✦

        [Header("Boundary Settings")]
        [SerializeField] private float boundaryRadius = 12f;       // Bán kính giới hạn
        [SerializeField] private Transform zoneCenter;             // Tâm vùng giới hạn

        [Header("Push Back Settings")]
        [SerializeField] private float pushBackForce = 10f;        // Lực đẩy ngược lại
        [SerializeField] private float pushBackDuration = 0.3f;    // Thời gian đẩy
        [SerializeField] private bool restrictInput = true;        // Khóa input tạm thời

        [Header("Activation")]
        [SerializeField] private bool activateByTrigger = true;    // Có kích hoạt bằng trigger không
        private bool isActive = false;                             // Đang hoạt động hay không

        [Header("Debug")]
        [SerializeField] private Color zoneColor = new Color(0.3f, 0.7f, 1f, 0.15f);
        [SerializeField] private bool showGizmos = true;

        // Tham chiếu Player & Input
        private Player playerInside;
        private PlayerInputManager inputManager;

        #endregion
        //─────────────────────────────────────────────
        #region ✦ UNITY LIFECYCLE ✦

        private void Start()
        {
            playerInside = PlayerHub.Instance.Player;
            inputManager = PlayerHub.Instance.InputManager;

            if (zoneCenter == null)
                zoneCenter = transform;
        }

        private void Update()
        {
            if (!isActive || playerInside == null)
                return;

            float distance = Vector3.Distance(zoneCenter.position, playerInside.transform.position);
            if (distance > boundaryRadius)
                HandlePlayerOutsideZone();
        }

        #endregion
        //─────────────────────────────────────────────
        #region ✦ KÍCH HOẠT VÙNG ✦

        /// <summary>
        /// 🔓 Kích hoạt vùng giới hạn (gọi từ BossEncounterCutscene).
        /// </summary>
        public void ActivateBoundary() => isActive = true;

        /// <summary>
        /// Trả về bán kính giới hạn (cho các script khác dùng, ví dụ SoldierRobot).
        /// </summary>
        public float GetBoundaryRadius() => boundaryRadius;

        #endregion
        //─────────────────────────────────────────────
        #region ✦ XỬ LÝ PLAYER ✦

        /// <summary>
        /// Khi Player ra khỏi vùng, đẩy ngược lại và khóa input ngắn.
        /// </summary>
        private void HandlePlayerOutsideZone()
        {
            Vector3 toPlayer = playerInside.transform.position - zoneCenter.position;
            Vector3 pushDirection = -toPlayer.normalized;

            if (restrictInput && inputManager != null)
                inputManager.DisableMovementTemporarily(pushBackDuration);

            StartCoroutine(PushBackPlayer(playerInside, pushDirection * pushBackForce));
        }

        /// <summary>
        /// Coroutine đẩy Player trở lại vùng an toàn.
        /// </summary>
        private IEnumerator PushBackPlayer(Player player, Vector3 force)
        {
            float elapsed = 0f;
            Vector3 startVel = player.velocity;

            while (elapsed < pushBackDuration)
            {
                player.velocity = Vector3.Lerp(startVel, force, elapsed / pushBackDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            player.velocity = Vector3.zero;
        }

        #endregion
        //─────────────────────────────────────────────
        #region ✦ GIZMOS ✦

        /// <summary>
        /// Vẽ vùng giới hạn trong Scene để dễ quan sát.
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!showGizmos) return;

            Gizmos.color = zoneColor;
            Vector3 center = zoneCenter != null ? zoneCenter.position : transform.position;
            Gizmos.DrawSphere(center, boundaryRadius);
        }

        #endregion
    }
}
