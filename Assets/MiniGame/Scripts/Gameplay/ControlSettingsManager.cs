using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Quản lý và lưu trữ các cài đặt điều khiển của người chơi.
    /// Dữ liệu được lưu bằng PlayerPrefs để giữ lại giữa các lần chơi.
    /// </summary>
    public class ControlSettingsManager
    {
        /// <summary>
        /// Event được gọi khi người chơi bật điều khiển bằng Tilt (nghiêng điện thoại).
        /// </summary>
        public static event GameEventActions.SimpleAction OnTiltEnabledEvent;

        /// <summary>
        /// Event được gọi khi người chơi tắt điều khiển bằng Tilt.
        /// </summary>
        public static event GameEventActions.SimpleAction OnTiltDisabledEvent;

        /// <summary>
        /// Cho phép máy bay lăn cánh (roll).
        /// </summary>
        public static bool IsRollEnabled
        {
            get { return _isRollEnabled; }
            set
            {
                _isRollEnabled = value;

                PlayerPrefs.SetInt(PREF_KEY_ROLL_ENABLED, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
        private static bool _isRollEnabled;

        /// <summary>
        /// Cho phép điều khiển bằng chuột (dành cho PC).
        /// </summary>
        public static bool IsMouseEnabled
        {
            get { return _isMouseEnabled; }
            set
            {
                _isMouseEnabled = value;

                PlayerPrefs.SetInt(PREF_KEY_MOUSE_ENABLED, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
        private static bool _isMouseEnabled;

        /// <summary>
        /// Cho phép điều khiển bằng nghiêng điện thoại (Tilt).
        /// </summary>
        public static bool IsTiltEnabled
        {
            get { return _isTiltEnabled; }
            set
            {
                _isTiltEnabled = value;

                PlayerPrefs.SetInt(PREF_KEY_TILT_ENABLED, value ? 1 : 0);
                PlayerPrefs.Save();

                // Gửi event khi thay đổi chế độ điều khiển
                if (value)
                {
                    OnTiltEnabledEvent?.Invoke();
                }
                else
                {
                    OnTiltDisabledEvent?.Invoke();
                }
            }
        }
        private static bool _isTiltEnabled;

        /// <summary>
        /// Đảo ngược trục pitch (kéo xuống để bay lên giống flight simulator).
        /// </summary>
        public static bool IsInversePitch
        {
            get { return _isInversePitch; }
            set
            {
                _isInversePitch = value;

                PlayerPrefs.SetInt(PREF_KEY_INVERSE_PITCH, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }
        private static bool _isInversePitch;

        // Các key dùng để lưu dữ liệu trong PlayerPrefs
        private static string PREF_KEY_ROLL_ENABLED = "FlightControls_RollEnabled";
        private static string PREF_KEY_MOUSE_ENABLED = "FlightControls_MouseEnabled";
        private static string PREF_KEY_TILT_ENABLED = "FlightControls_TiltEnabled";
        private static string PREF_KEY_INVERSE_PITCH = "FlightControls_InversePitch";

        /// <summary>
        /// Static constructor – chạy một lần khi class được load.
        /// Dùng để khởi tạo cài đặt điều khiển từ PlayerPrefs.
        /// </summary>
        static ControlSettingsManager()
        {
            // Nếu game chưa từng lưu cài đặt
            if (!PlayerPrefs.HasKey(PREF_KEY_ROLL_ENABLED))
            {
                // Thiết lập mặc định
                IsRollEnabled = true;
                IsMouseEnabled = false;
                IsTiltEnabled = true;
                IsInversePitch = false;
            }
            else
            {
                // Tải cài đặt đã lưu
                IsRollEnabled = PlayerPrefs.GetInt(PREF_KEY_ROLL_ENABLED) == 1;
                IsMouseEnabled = PlayerPrefs.GetInt(PREF_KEY_MOUSE_ENABLED) == 1;
                IsTiltEnabled = PlayerPrefs.GetInt(PREF_KEY_TILT_ENABLED) == 1;
                IsInversePitch = PlayerPrefs.GetInt(PREF_KEY_INVERSE_PITCH) == 1;
            }
        }
    }
}