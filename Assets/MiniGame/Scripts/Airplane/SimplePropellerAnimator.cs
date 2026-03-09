using UnityEngine;
using MiniGame;

namespace MiniGame
{
    // Yêu cầu GameObject phải có FlightDynamicsController
    // Nếu chưa có Unity sẽ tự thêm vào
    [RequireComponent(typeof(FlightDynamicsController))]
    public class SimplePropellerAnimator : MonoBehaviour
    {
        /// <summary>
        /// Transform của model cánh quạt (propeller) cần xoay
        /// </summary>
        public Transform propellerModel;

        /// <summary>
        /// Tốc độ quay tối đa của cánh quạt (RPM - vòng/phút)
        /// </summary>
        public float maxRpm = 2000;

        /// <summary>
        /// Nếu true thì quay theo trục X (thường dùng cho model import từ Blender)
        /// Nếu false thì quay theo trục Y
        /// </summary>
        public bool rotateAroundX = false;

        // Tham chiếu đến script điều khiển máy bay
        private FlightDynamicsController _airplane;

        /// <summary>
        /// Hằng số dùng để chuyển đổi từ RPM sang độ/giây
        /// </summary>
        private const float RPM_TO_DPS = 60f;

        // Renderer của propeller (hiện chưa sử dụng nhưng có thể dùng để bật/tắt blur effect)
        private Renderer _propellerModelRenderer;

        private void Awake()
        {
            // Lấy component FlightDynamicsController từ GameObject
            _airplane = GetComponent<FlightDynamicsController>();
        }

        private void Update()
        {
            // Nếu chưa có controller hoặc chưa gán propeller model thì thoát
            if (!_airplane || !propellerModel)
            {
                return;
            }

            // Tính tốc độ quay dựa vào throttle của máy bay
            // Throttle càng lớn thì cánh quạt quay càng nhanh
            float rotation = maxRpm * _airplane.Throttle * Time.deltaTime * RPM_TO_DPS;

            // Xoay cánh quạt theo trục được chọn
            if (rotateAroundX)
            {
                // Xoay theo trục X
                propellerModel.Rotate(rotation, 0, 0);
            }
            else
            {
                // Xoay theo trục Y
                propellerModel.Rotate(0, rotation, 0);
            }
        }
    }
}