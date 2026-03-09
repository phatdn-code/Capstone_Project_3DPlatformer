using UnityEngine;

namespace MiniGame
{
    // Script dùng để điều khiển các vệt bay (trail) phía sau máy bay
    public class AircraftTrailController : MonoBehaviour
    {

        /// <summary>
        /// GameObject chứa toàn bộ các TrailRenderer của máy bay
        /// Ví dụ: vệt khói ở cánh hoặc phía sau động cơ
        /// </summary>
        public GameObject trailsContainer;

        /// <summary>
        /// Bật hiệu ứng vệt bay
        /// Thường dùng khi máy bay bắt đầu bay hoặc tăng tốc
        /// </summary>
        public virtual void ActivateTrails()
        {
            // Kiểm tra container có tồn tại không
            if (trailsContainer != null)
            {
                // Bật GameObject chứa trail
                trailsContainer.SetActive(true);
            }
        }

        /// <summary>
        /// Tắt hiệu ứng vệt bay
        /// Ví dụ khi máy bay dừng hoặc không cần hiển thị vệt khói
        /// </summary>
        public virtual void DeactivateTrails()
        {
            if (trailsContainer != null)
            {
                // Tắt GameObject chứa trail
                trailsContainer.SetActive(false);
            }
        }

        /// <summary>
        /// Xóa toàn bộ vệt bay hiện tại
        /// Dùng khi reset máy bay hoặc respawn để tránh vệt cũ còn lại trên màn hình
        /// </summary>
        public virtual void ClearTrails()
        {
            if (trailsContainer != null)
            {
                // Lấy tất cả component TrailRenderer nằm trong container
                var renderers = trailsContainer.GetComponentsInChildren<TrailRenderer>();

                // Duyệt qua từng TrailRenderer
                foreach (TrailRenderer r in renderers)
                {
                    // Xóa toàn bộ vệt đã vẽ
                    r.Clear();

                    // nếu bật có thể dùng để reset thời gian trail
                    // r.time = 0;
                }
            }
        }
    }
}