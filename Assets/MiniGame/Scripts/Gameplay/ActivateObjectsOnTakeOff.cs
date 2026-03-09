using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Script này dùng để kích hoạt các GameObject
    /// khi máy bay của người chơi cất cánh khỏi mặt đất.
    /// </summary>
    public class ActivateObjectsOnTakeOff : MonoBehaviour
    {
        /// <summary>
        /// Thời gian trễ trước khi kích hoạt object sau khi máy bay cất cánh.
        /// </summary>
        public float delay = 0;

        /// <summary>
        /// Danh sách các GameObject sẽ được kích hoạt.
        /// </summary>
        public GameObject[] objectsToActivate;

        void OnEnable()
        {
            // Đăng ký lắng nghe sự kiện máy bay cất cánh
            AirplaneTakeOffDetector.OnTakeOffEvent += OnTakeOff;
        }

        void OnDisable()
        {
            // Hủy đăng ký sự kiện khi script bị tắt
            AirplaneTakeOffDetector.OnTakeOffEvent -= OnTakeOff;
        }

        /// <summary>
        /// Được gọi khi sự kiện cất cánh xảy ra.
        /// </summary>
        private void OnTakeOff()
        {
            // Gọi hàm kích hoạt sau một khoảng delay
            Invoke(nameof(OnTakeOffCore), delay);
        }

        /// <summary>
        /// Kích hoạt các object trong danh sách.
        /// </summary>
        private void OnTakeOffCore()
        {
            foreach (var target in objectsToActivate)
            {
                if (target != null)
                {
                    target.SetActive(true);
                }
            }
        }
    }
}