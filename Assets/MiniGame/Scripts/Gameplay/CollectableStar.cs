using UnityEngine;
using UnityStandardAssets.ImageEffects;

namespace MiniGame
{
    /// <summary>
    /// Script điều khiển quả cầu có thể nhặt được.
    /// Quả cầu sẽ xoay, lớn dần theo thời gian và biến mất khi người chơi chạm vào.
    /// </summary>
    public class CollectableStar : MonoBehaviour
    {
        // Delegate dùng để tạo sự kiện khi người chơi nhặt quả cầu
        public delegate void OnCollectAction();

        /// <summary>
        /// Sự kiện được gọi khi máy bay của người chơi nhặt được quả cầu
        /// Các script khác có thể lắng nghe sự kiện này
        /// </summary>
        public static event OnCollectAction OnCollectEvent;

        /// <summary>
        /// Cho phép tất cả các quả cầu trong game lớn dần theo thời gian
        /// </summary>
        public static bool growingEnabled = false;

        /// <summary>
        /// Tốc độ lớn lên của quả cầu
        /// </summary>
        public float growthSpeed = 0.1f;

        /// <summary>
        /// Kích thước tối đa mà quả cầu có thể đạt tới
        /// </summary>
        public float maxScale = 40f;

        /// <summary>
        /// Tốc độ xoay của các vòng tròn quanh quả cầu
        /// </summary>
        public float ringRotationSpeed = 150f;

        // Các object con
        public GameObject ring1;
        public GameObject ring2;
        public GameObject sphere;

        // Kiểm tra đã kích hoạt hay chưa
        private bool _activated;

        // Hiệu ứng bloom của camera
        private BloomOptimized _bloom;

        // Giá trị bloom ban đầu
        private float _bloomInitValue;

        // Kiểm tra đang thực hiện hiệu ứng biến mất
        private bool _isTweeningOut = false;

        // Kiểm tra đã bị destroy chưa
        private bool _isDestroyed = false;

        void Start()
        {
            // Tìm hiệu ứng Bloom trong scene
            _bloom = GameObject.FindFirstObjectByType<BloomOptimized>();

            if (_bloom != null)
            {
                _bloomInitValue = _bloom.intensity;
            }

            // Random rotation ban đầu cho object
            transform.localRotation = Random.rotation;
        }

        void Update()
        {
            // Nếu object đã bị destroy thì không cập nhật nữa
            if (_isDestroyed)
            {
                return;
            }

            // Xoay các vòng quanh quả cầu
            ring1.transform.Rotate(Vector3.right, ringRotationSpeed * Time.deltaTime);
            ring2.transform.Rotate(Vector3.up, ringRotationSpeed * Time.deltaTime);

            // Nếu cho phép grow thì tăng kích thước dần
            if (growingEnabled && transform.localScale.x < maxScale)
            {
                transform.localScale += Vector3.one * growthSpeed * Time.deltaTime;
            }

            // Nếu đang thực hiện hiệu ứng biến mất
            if (_isTweeningOut)
            {
                // Thu nhỏ object dần
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 5f * Time.deltaTime);

                // Giảm dần hiệu ứng bloom về ban đầu
                if (_bloom != null)
                {
                    _bloom.intensity = Mathf.Lerp(_bloom.intensity, _bloomInitValue, 5f * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Hàm được gọi khi có collider đi vào trigger
        /// </summary>
        void OnTriggerEnter(Collider collider)
        {
            // Nếu đã kích hoạt rồi thì bỏ qua
            if (_activated)
            {
                return;
            }

            // Kiểm tra nếu object va chạm là Player
            if (collider.gameObject.CompareTag(GameTags.Player))
            {
                _activated = true;

                // Gửi sự kiện cho các script khác
                if (OnCollectEvent != null)
                {
                    OnCollectEvent();
                }

                // Phát âm thanh khi nhặt
                var sound = GetComponentInParent<AudioSource>();
                if (sound != null)
                {
                    sound.Play();
                }

                // Bắt đầu hiệu ứng biến mất
                _isTweeningOut = true;

                // Tăng bloom để tạo hiệu ứng flash màn hình
                if (_bloom != null)
                {
                    _bloom.intensity = 0.5f;
                }

                // Làm các boids tản ra xa
                var boids = GetComponentInParent<BoidFlockManager>();
                if (boids != null)
                {
                    boids.neighborDistance = 250f;
                }

                // Gọi destroy sau 1 giây
                // Không destroy toàn bộ object vì sẽ ảnh hưởng tới boids
                Invoke("DestroyNow", 1f);
            }
        }

        /// <summary>
        /// Thay đổi giá trị bloom từ script khác
        /// </summary>
        public void TweenBloom(float value)
        {
            _bloom.intensity = value;
        }

        /// <summary>
        /// Destroy các object con của quả cầu
        /// </summary>
        private void DestroyNow()
        {
            _bloom.intensity = _bloomInitValue;

            Destroy(sphere);
            Destroy(ring1);
            Destroy(ring2);

            _isDestroyed = true;
        }
    }
}