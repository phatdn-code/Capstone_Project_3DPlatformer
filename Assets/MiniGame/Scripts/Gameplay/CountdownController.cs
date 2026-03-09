using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Quản lý hệ thống đếm ngược thời gian của màn chơi
    /// </summary>
    public class CountdownController : MonoBehaviour
    {
        /// Tổng thời gian của màn chơi (giây)
        public float maxTime = 90f;

        /// Thời gian còn lại
        public float timeRemaining { get; private set; }

        /// Event khi thời gian sắp hết
        public static event GameEventActions.SimpleAction OnTimeLowEvent;

        /// Event khi hết thời gian
        public static event GameEventActions.SimpleAction OnTimeEmptyEvent;

        /// Phần trăm thời gian được coi là thấp
        public const float LOW_TIME_PERCENT = 0.25f;

        /// Có đang đếm thời gian hay không
        private bool _isCounting = false;

        /// Cho script khác đọc trạng thái timer
        public bool IsCounting => _isCounting;

        /// Đã kích hoạt cảnh báo low time chưa
        private bool _lowTimeRegistered = false;

        void Start()
        {
            ResetTimer();
        }

        void OnEnable()
        {
            AirplaneTakeOffDetector.OnTakeOffEvent += HandleTakeOff;
        }

        void OnDisable()
        {
            AirplaneTakeOffDetector.OnTakeOffEvent -= HandleTakeOff;
        }

        /// Reset timer
        public void ResetTimer()
        {
            timeRemaining = maxTime;
            _isCounting = false;
            _lowTimeRegistered = false;
        }

        /// Khi máy bay cất cánh -> bắt đầu đếm
        private void HandleTakeOff()
        {
            _isCounting = true;
        }

        /// Cộng thêm thời gian khi nhặt pickup
        public void AddTime(float extraTime)
        {
            timeRemaining += extraTime;

            // Giới hạn không vượt quá maxTime
            if (timeRemaining > maxTime)
                timeRemaining = maxTime;

            // reset cảnh báo low time nếu được cộng thời gian
            _lowTimeRegistered = false;
        }

        void Update()
        {
            if (!_isCounting) return;

            timeRemaining -= Time.deltaTime;

            if (timeRemaining < 0)
                timeRemaining = 0;

            float percent = timeRemaining / maxTime;

            if (!_lowTimeRegistered && percent <= LOW_TIME_PERCENT)
            {
                _lowTimeRegistered = true;
                OnTimeLowEvent?.Invoke();
            }

            if (timeRemaining <= 0)
            {
                _isCounting = false;
                OnTimeEmptyEvent?.Invoke();
            }
        }
    }
}