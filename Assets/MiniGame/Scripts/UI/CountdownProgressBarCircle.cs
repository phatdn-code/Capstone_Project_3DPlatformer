using UnityEngine;
using UnityEngine.UI;

namespace MiniGame
{
    /// <summary>
    /// UI thanh tiến trình đếm ngược dạng hình tròn
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CountdownProgressBarCircle : MonoBehaviour
    {
        /// <summary>
        /// Image của thanh thời gian đầy
        /// </summary>
        public Image barTimeFull;

        /// <summary>
        /// Image của thanh thời gian khi gần hết
        /// </summary>
        public Image barTimeLow;

        /// <summary>
        /// Giá trị phần trăm khi bắt đầu làm mờ thanh full
        /// </summary>
        [Tooltip("Giá trị phần trăm khi thanh full bắt đầu mờ")]
        [Space]
        public float blendStart = 0.5f;

        /// <summary>
        /// Giá trị phần trăm khi thanh full biến mất hoàn toàn
        /// </summary>
        [Tooltip("Giá trị phần trăm khi thanh full biến mất hoàn toàn")]
        public float blendEnd = 0.2f;

        /// <summary>
        /// Nếu bật: thanh tròn sẽ giảm kích thước
        /// Nếu tắt: thanh tròn sẽ giảm fillAmount
        /// </summary>
        [Tooltip("Nếu bật sẽ giảm bằng scale thay vì fillAmount")]
        [Space]
        public bool decreaseByScaling = false;

        /// <summary>
        /// Tham chiếu tới bộ điều khiển thời gian
        /// </summary>
        protected CountdownController _timeController;

        /// <summary>
        /// CanvasGroup để điều khiển độ trong suốt của UI
        /// </summary>
        protected CanvasGroup _canvasGroup;

        /// <summary>
        /// Thời điểm bắt đầu hiệu ứng nhấp nháy
        /// </summary>
        private float _blinkStartTime;

        /// <summary>
        /// Có đang nhấp nháy hay không
        /// </summary>
        private bool _isBlinking;

        void Start()
        {
            // Tìm CountdownController trong scene
            _timeController = GameObject.FindFirstObjectByType<CountdownController>();

            if (_timeController == null)
            {
                Debug.LogError("Không tìm thấy CountdownController.");
                this.enabled = false;
                return;
            }

            // Lấy CanvasGroup
            _canvasGroup = GetComponent<CanvasGroup>();

            // Ẩn UI khi bắt đầu game
            _canvasGroup.alpha = 0;
        }

        void OnEnable()
        {
            // Đăng ký sự kiện
            AirplaneTakeOffDetector.OnTakeOffEvent += FadeIn;
            PauseController.OnPauseEvent += FadeOut;
            PauseController.OnUnPauseEvent += FadeIn;
            CountdownController.OnTimeEmptyEvent += FadeOut;
        }

        void OnDisable()
        {
            // Hủy đăng ký sự kiện
            AirplaneTakeOffDetector.OnTakeOffEvent -= FadeIn;
            PauseController.OnPauseEvent -= FadeOut;
            PauseController.OnUnPauseEvent -= FadeIn;
            CountdownController.OnTimeEmptyEvent -= FadeOut;
        }

        /// <summary>
        /// Hiện UI
        /// </summary>
        private void FadeIn()
        {
            Fader.FadeAlpha(this, _canvasGroup, true, 1);
        }

        /// <summary>
        /// Ẩn UI
        /// </summary>
        private void FadeOut()
        {
            Fader.FadeAlpha(this, _canvasGroup, false, 1);
        }

        void Update()
        {
            // Tính phần trăm thời gian còn lại
            float percent = _timeController.timeRemaining / _timeController.maxTime;

            // ===== Cách giảm thanh thời gian =====

            if (decreaseByScaling)
            {
                // Giảm kích thước hình tròn theo phần trăm
                Vector3 newScale = new Vector3(percent, percent, 1f);

                barTimeFull.rectTransform.localScale = newScale;
                barTimeLow.rectTransform.localScale = newScale;
            }
            else
            {
                // Giảm lượng fill của thanh tròn
                barTimeFull.fillAmount = percent;
                barTimeLow.fillAmount = percent;
            }

            // ===== Hiệu ứng làm mờ thanh full khi gần hết thời gian =====
            if (percent < blendStart)
            {
                float alpha = (percent - blendEnd) / (blendStart - blendEnd);

                barTimeFull.color = new Color(1, 1, 1, alpha);
            }

            // ===== Hiệu ứng nhấp nháy khi thời gian thấp =====
            if (percent < CountdownController.LOW_TIME_PERCENT)
            {
                // Nếu mới bắt đầu nhấp nháy
                if (!_isBlinking)
                    _blinkStartTime = Time.time;

                _isBlinking = true;

                // Hiệu ứng nhấp nháy alpha bằng hàm cos
                _canvasGroup.alpha = Mathf.Clamp01(
                    Mathf.Cos((Time.time - _blinkStartTime) * 10f) * 0.5f + 0.5f + 0.25f);
            }
            else
            {
                // Trả lại alpha bình thường
                if (_isBlinking)
                    _canvasGroup.alpha = 1f;

                _isBlinking = false;
            }
        }
    }
}