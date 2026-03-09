using System.Collections;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

namespace MiniGame
{
    /// <summary>
    /// Lớp này dùng để hiệu chỉnh (calibrate) trục dọc của điều khiển nghiêng (Tilt Input).
    /// Mục đích là để người chơi có thể cầm điện thoại ở vị trí thoải mái,
    /// sau đó hệ thống sẽ lấy vị trí đó làm trung tâm điều khiển.
    /// </summary>
    public class TiltControlCalibrator : MonoBehaviour
    {
        /// <summary>
        /// Đối tượng TiltInput cần được hiệu chỉnh.
        /// </summary>
        public TiltInput calibrationTarget;

        /// <summary>
        /// GameObject chứa UI hiển thị thông báo đang hiệu chỉnh.
        /// </summary>
        public GameObject calibrationPopup;

        /// <summary>
        /// Thời gian chờ sau khi nhấn nút Play trước khi bắt đầu hiệu chỉnh.
        /// </summary>
        public float delayAfterStartPlay = 8f;

        void OnEnable()
        {
            // Khi người chơi nhấn Play -> bắt đầu hiệu chỉnh sau một khoảng delay
            UIEventsPublisher.OnPlayEvent += CalibrateDelayed;

            // Khi game được unpause -> hiệu chỉnh lại ngay
            PauseController.OnUnPauseEvent += Calibrate;
        }

        void OnDisable()
        {
            UIEventsPublisher.OnPlayEvent -= CalibrateDelayed;
            PauseController.OnUnPauseEvent -= Calibrate;
        }

        /// <summary>
        /// Thực hiện hiệu chỉnh sau một khoảng thời gian delay.
        /// </summary>
        public virtual void CalibrateDelayed()
        {
            // Chỉ thực hiện nếu chế độ điều khiển Tilt được bật
            if (ControlSettingsManager.IsTiltEnabled)
            {
                StartCoroutine(CalibrateCoroutine(delayAfterStartPlay));
            }
        }

        /// <summary>
        /// Hiệu chỉnh ngay lập tức (không delay).
        /// </summary>
        public virtual void Calibrate()
        {
            if (ControlSettingsManager.IsTiltEnabled)
            {
                StartCoroutine(CalibrateCoroutine());
            }
        }

        /// <summary>
        /// Coroutine thực hiện quá trình hiệu chỉnh.
        /// </summary>
        private IEnumerator CalibrateCoroutine(float delay = 0)
        {
            // Chờ một khoảng thời gian nếu có delay
            if (delay > 0)
            {
                yield return new WaitForSeconds(delay);
            }

            // Hiển thị popup thông báo đang hiệu chỉnh
            if (calibrationPopup != null)
            {
                calibrationPopup.SetActive(true);
            }

            // Chờ thêm 3 giây để người chơi giữ điện thoại ổn định
            yield return new WaitForSeconds(3f);

            if (calibrationTarget == null)
            {
                yield break;
            }

            // Bắt đầu tính toán góc hiện tại của thiết bị
            float currentAngle = 0;

            if (Input.acceleration != Vector3.zero)
            {
                switch (calibrationTarget.tiltAroundAxis)
                {
                    case TiltInput.AxisOptions.ForwardAxis:
                        // Tính góc nghiêng theo trục trước-sau
                        currentAngle = Mathf.Atan2(Input.acceleration.x, -Input.acceleration.y) * Mathf.Rad2Deg;
                        break;

                    case TiltInput.AxisOptions.SidewaysAxis:
                        // Tính góc nghiêng theo trục trái-phải
                        currentAngle = Mathf.Atan2(Input.acceleration.z, -Input.acceleration.y) * Mathf.Rad2Deg;
                        break;
                }
            }

            // Giới hạn góc để tránh lỗi khi thiết bị ở vị trí bất thường
            currentAngle = Mathf.Min(180f, currentAngle);
            currentAngle = Mathf.Max(-180f, currentAngle);

            // Thiết lập offset để lấy vị trí hiện tại làm trung tâm điều khiển
            calibrationTarget.centreAngleOffset = -currentAngle;

            // Ẩn popup sau khi hiệu chỉnh xong
            if (calibrationPopup != null)
            {
                calibrationPopup.SetActive(false);
            }
        }
    }
}