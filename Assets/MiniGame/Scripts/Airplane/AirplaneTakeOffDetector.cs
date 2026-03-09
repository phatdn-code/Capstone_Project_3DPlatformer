using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Lớp này dùng để phát hiện khi máy bay cất cánh khỏi đường băng.
    /// Khi máy bay va chạm với platform có tag TakeOffPlatform và rời khỏi
    /// sau một khoảng thời gian hợp lệ thì sự kiện TakeOff sẽ được gọi.
    /// </summary>
    public class AirplaneTakeOffDetector : MonoBehaviour
    {
        /// <summary>
        /// Sự kiện được gọi khi máy bay do người chơi điều khiển cất cánh.
        /// Có thể dùng để hiển thị HUD, hiệu ứng hoặc animation.
        /// </summary>
        public static event GameEventActions.SimpleAction OnTakeOffEvent;

        /// <summary>
        /// Thời gian tối thiểu (giây) để được xem là máy bay đã đáp xuống
        /// chứ không phải va chạm (crash).
        /// </summary>
        private const float MIN_LANDING_DURATION = 1f;

        /// <summary>
        /// Khoảng thời gian tối thiểu giữa hai lần đáp xuống.
        /// Nếu nhỏ hơn giá trị này thì sẽ bị bỏ qua.
        /// </summary>
        private const float MIN_TIME_BETWEEN_LANDINGS = 10f;

        /// <summary>
        /// Thời điểm bắt đầu va chạm với platform.
        /// </summary>
        private float _collisionEnterTime = -1;

        /// <summary>
        /// Được gọi khi máy bay bắt đầu va chạm với một object.
        /// Nếu object có tag TakeOffPlatform và thỏa điều kiện thời gian,
        /// thì bắt đầu tính thời gian tiếp đất.
        /// </summary>
        void OnCollisionEnter(Collision collision)
        {
            // Thời gian kể từ lần tiếp đất trước
            float duration = Time.time - _collisionEnterTime;

            // Kiểm tra xem lần tiếp đất này có hợp lệ hay không
            bool validLanding = duration > MIN_TIME_BETWEEN_LANDINGS || _collisionEnterTime < 0;

            if (validLanding && collision.gameObject.CompareTag(GameTags.TakeOffPlatform))
            {
                // Lưu lại thời điểm bắt đầu va chạm
                _collisionEnterTime = Time.time;
            }
        }

        /// <summary>
        /// Được gọi khi máy bay rời khỏi object đang va chạm.
        /// Nếu thời gian tiếp đất đủ lâu thì coi như máy bay đã cất cánh.
        /// </summary>
        void OnCollisionExit(Collision collision)
        {
            float duration = Time.time - _collisionEnterTime;

            if (duration > MIN_LANDING_DURATION && collision.gameObject.CompareTag(GameTags.TakeOffPlatform))
            {
                // Gọi sự kiện cất cánh nếu có listener
                if (OnTakeOffEvent != null)
                {
                    OnTakeOffEvent();
                }
            }
        }
    }
}