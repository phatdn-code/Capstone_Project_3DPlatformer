using System.Collections;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using MiniGame;

namespace MiniGame
{
    /// <summary>
    /// Đọc input từ người chơi (PC hoặc Mobile)
    /// và truyền dữ liệu điều khiển cho FlightDynamicsController
    /// </summary>
    [RequireComponent(typeof(FlightDynamicsController))]
    public class AircraftInputController : MonoBehaviour
    {
        [Header("Giới hạn dành cho điều khiển trên Mobile")]

        [Tooltip("Góc roll tối đa cho phép trên mobile (độ).")]
        public float maxRollAngle = 80f;

        [Tooltip("Góc pitch tối đa cho phép trên mobile (độ).")]
        public float maxPitchAngle = 80f;

        // Tham chiếu tới máy bay đang được điều khiển
        private FlightDynamicsController _airplane;

        private void Awake()
        {
            // Lấy AeroplaneController gắn trên cùng GameObject
            _airplane = GetComponent<FlightDynamicsController>();
        }

        private IEnumerator Start()
        {
            // Tắt hiệu ứng khí động học lúc bắt đầu
            // để tránh rung máy bay khi còn trên mặt đất
            float aerodynamicEffect = _airplane.AerodynamicEffect;
            _airplane.AerodynamicEffect = 0f;

            yield return new WaitForSeconds(3f);

            // Bật lại hiệu ứng khí động học
            _airplane.AerodynamicEffect = aerodynamicEffect;
        }

        private void FixedUpdate()
        {
            // Đọc input chuột (nếu được bật)
            float mousePitch = ControlSettingsManager.IsMouseEnabled
                ? CrossPlatformInputManager.GetAxis("Mouse Y")
                : 0f;

            float mouseRoll = ControlSettingsManager.IsMouseEnabled
                ? CrossPlatformInputManager.GetAxis("Mouse X")
                : 0f;

            // Đọc input điều khiển máy bay
            float roll = ControlSettingsManager.IsRollEnabled
                ? CrossPlatformInputManager.GetAxis("Roll") + mouseRoll
                : 0f;

            float pitch =
                (ControlSettingsManager.IsInversePitch ? -1f : 1f)
                * CrossPlatformInputManager.GetAxis("Pitch")
                + mousePitch;

            float yaw = CrossPlatformInputManager.GetAxis("Yaw");
            bool airBrakes = CrossPlatformInputManager.GetButton("Brakes");

            // Tự động tăng ga, hoặc giảm ga khi bật phanh gió
            float throttle = airBrakes ? -1f : 1f;

#if MOBILE_INPUT
            // Trên mobile, roll luôn được bật
            roll = CrossPlatformInputManager.GetAxis("Roll");
            AdjustInputForMobileControls(ref roll, ref pitch, ref throttle);
#endif

            // Truyền input sang AeroplaneController
            _airplane.Move(roll, pitch, yaw, throttle, airBrakes);
        }

        /// <summary>
        /// Điều chỉnh input để phù hợp với điều khiển trên mobile
        /// (dựa trên góc mong muốn thay vì input trực tiếp)
        /// </summary>
        private void AdjustInputForMobileControls(
            ref float roll,
            ref float pitch,
            ref float throttle)
        {
            float intendedRollAngle =
                roll * maxRollAngle * Mathf.Deg2Rad;

            float intendedPitchAngle =
                pitch * maxPitchAngle * Mathf.Deg2Rad;

            roll = Mathf.Clamp(
                intendedRollAngle - _airplane.RollAngle,
                -1f, 1f);

            pitch = Mathf.Clamp(
                intendedPitchAngle - _airplane.PitchAngle,
                -1f, 1f);

            float intendedThrottle =
                throttle * 0.5f + 0.5f;

            throttle = Mathf.Clamp(
                intendedThrottle - _airplane.Throttle,
                -1f, 1f);
        }
    }
}
