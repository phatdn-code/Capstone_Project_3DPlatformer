using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Điều khiển hành vi của một boid riêng lẻ trong đàn.
    /// Boid sẽ tự điều chỉnh hướng bay dựa trên các boid xung quanh
    /// theo thuật toán flocking (Separation, Alignment, Cohesion).
    /// </summary>
    public class BoidAgent : MonoBehaviour
    {
        /// <summary>
        /// Tham chiếu đến bộ quản lý đàn boid.
        /// </summary>
        public BoidFlockManager master;

        void Update()
        {
            var currentPosition = transform.position;
            var currentRotation = transform.rotation;

            // Khởi tạo các vector điều khiển hướng bay
            var separation = Vector3.zero;                 // Tránh va chạm
            var alignment = master.transform.forward;      // Bay cùng hướng
            var cohesion = master.transform.position;      // Bay gần trung tâm đàn

            // Tìm các boid xung quanh trong bán kính neighborDistance
            var nearbyBoids = Physics.OverlapSphere(
                currentPosition,
                master.neighborDistance,
                master.searchLayer
            );

            // Cộng dồn các vector điều khiển
            foreach (var boid in nearbyBoids)
            {
                // Bỏ qua chính nó
                if (boid.gameObject == gameObject)
                {
                    continue;
                }

                var t = boid.transform;

                // Vector tránh va chạm
                separation += GetSeparationVector(t);

                // Lấy hướng bay của boid khác
                alignment += t.forward;

                // Lấy vị trí của boid khác
                cohesion += t.position;
            }

            // Tính trung bình các vector
            var avg = 1.0f / nearbyBoids.Length;

            alignment *= avg;
            cohesion *= avg;

            // Vector hướng về trung tâm đàn
            cohesion = (cohesion - currentPosition).normalized;

            // Tính hướng bay mới dựa trên 3 quy tắc
            var direction = separation + alignment + cohesion;

            var rotation = Quaternion.FromToRotation(
                Vector3.forward,
                direction.normalized
            );

            // Áp dụng xoay mượt (interpolation)
            if (rotation != currentRotation)
            {
                var ip = Mathf.Exp(-master.rotationCoefficient * Time.deltaTime);
                transform.rotation = Quaternion.Slerp(rotation, currentRotation, ip);
            }

            // Thêm nhiễu ngẫu nhiên để chuyển động tự nhiên hơn
            var noise = Mathf.PerlinNoise(Time.time, Random.value * 10.0f) * 2.0f - 1.0f;

            var speed = master.speed * (1.0f + noise * master.speedVariation);

            // Di chuyển về phía trước
            transform.position = currentPosition + transform.forward * speed * Time.deltaTime;
        }

        /// <summary>
        /// Tính vector tránh va chạm với một boid khác.
        /// Khoảng cách càng gần thì lực đẩy càng mạnh.
        /// </summary>
        private Vector3 GetSeparationVector(Transform target)
        {
            var vectorFromTarget = transform.position - target.transform.position;

            var distance = vectorFromTarget.magnitude;

            // Tính hệ số tránh va chạm
            var scaler = Mathf.Clamp01(1.0f - distance / master.neighborDistance);

            return vectorFromTarget * (scaler / distance);
        }
    }
}