using UnityEngine;
using UnityEngine.UI;

namespace MiniGame
{
    /// <summary>
    /// UI đồng hồ đếm ngược dạng kim quay (Dial Countdown)
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class CountdownDialUI : MonoBehaviour
    {
        /// <summary>
        /// Thanh thời gian đầy (màu bình thường)
        /// </summary>
        public Image barTimeFull;

        /// <summary>
        /// Thanh thời gian khi gần hết (màu cảnh báo)
        /// </summary>
        public Image barTimeLow;

        /// <summary>
        /// Kim đồng hồ quay theo thời gian còn lại
        /// </summary>
        public Image dialHand;

        /// <summary>
        /// Giá trị phần trăm khi bắt đầu làm mờ thanh full
        /// </summary>
        [Tooltip("Giá trị bắt đầu làm mờ thanh thời gian đầy")]
        [Space]
        public float blendStart = 0.5f;

        /// <summary>
        /// Giá trị phần trăm khi thanh full biến mất hoàn toàn
        /// </summary>
        [Tooltip("Giá trị khi thanh thời gian đầy biến mất hoàn toàn")]
        public float blendEnd = 0.2f;

        /// <summary>
        /// Tham chiếu tới bộ điều khiển thời gian
        /// </summary>
        protected CountdownController _timeController;

        /// <summary>
        /// CanvasGroup để điều khiển độ trong suốt UI
        /// </summary>
        protected CanvasGroup _canvasGroup;

        /// <summary>
        /// Thời điểm bắt đầu nhấp nháy
        /// </summary>
        private float _blinkStartTime;

        /// <summary>
        /// Đang ở trạng thái nhấp nháy hay không
        /// </summary>
        private bool _isBlinking;

        void Start()
        {
            // Tìm CountdownController trong Scene
            _timeController = GameObject.FindFirstObjectByType<CountdownController>();

            if (_timeController == null)
            {
                Debug.LogError("Không tìm thấy CountdownController trong Scene.");
                this.enabled = false;
                return;
            }

            // Lấy CanvasGroup của object
            _canvasGroup = GetComponent<CanvasGroup>();

            // Ẩn UI lúc bắt đầu game
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
            // Lấy thời gian còn lại
            float timeRemaining = _timeController.timeRemaining;

            // Lấy thời gian tối đa
            float maxTime = _timeController.maxTime;

            // Tính phần trăm thời gian còn lại
            float percent = timeRemaining / maxTime;

            // ===== Quay kim đồng hồ =====
            float newAngle = -percent * 180f + 90f;

            dialHand.rectTransform.rotation =
                Quaternion.AngleAxis(newAngle, Vector3.forward);

            // ===== Hiệu ứng làm mờ thanh thời gian =====
            if (percent < blendStart)
            {
                barTimeFull.color = new Color(
                    1,
                    1,
                    1,
                    (percent - blendEnd) / (blendStart - blendEnd));
            }

            // ===== Hiệu ứng nhấp nháy khi gần hết thời gian =====
            if (percent < CountdownController.LOW_TIME_PERCENT)
            {
                if (Time.timeScale > 0f)
                {
                    // Nếu mới bắt đầu nhấp nháy thì lưu thời gian
                    if (!_isBlinking)
                        _blinkStartTime = Time.time;

                    _isBlinking = true;

                    // Tạo hiệu ứng nhấp nháy alpha
                    _canvasGroup.alpha = Mathf.Clamp01(
                        Mathf.Cos((Time.time - _blinkStartTime) * 10f) * 0.5f + 0.5f + 0.25f);
                }
            }
            else
            {
                // Khi không còn ở trạng thái thời gian thấp
                if (_isBlinking)
                    _canvasGroup.alpha = 1f;

                _isBlinking = false;
            }
        }
    }
}