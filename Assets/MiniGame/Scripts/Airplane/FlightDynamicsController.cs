using UnityEngine;

namespace MiniGame
{
    [RequireComponent(typeof(Rigidbody))]
    public class FlightDynamicsController : MonoBehaviour
    {
        [Tooltip("Công suất tối đa của động cơ.")]
        [SerializeField] private float m_MaxEnginePower = 40f;

        [Tooltip("Hệ số lực nâng sinh ra khi máy bay di chuyển về phía trước.")]
        [SerializeField] private float m_Lift = 0.002f;

        [Tooltip("Tốc độ tại đó lực nâng bằng 0.")]
        [SerializeField] private float m_ZeroLiftSpeed = 300;

        [Tooltip("Độ mạnh điều khiển lăn cánh (Roll).")]
        [SerializeField] private float m_RollEffect = 1f;

        [Tooltip("Độ mạnh điều khiển chúi/ngóc (Pitch).")]
        [SerializeField] private float m_PitchEffect = 1f;

        [Tooltip("Độ mạnh điều khiển quay hướng (Yaw).")]
        [SerializeField] private float m_YawEffect = 0.2f;

        [Tooltip("Mức độ hỗ trợ quay khi máy bay đang nghiêng cánh.")]
        [SerializeField] private float m_BankedTurnEffect = 0.5f;

        [Tooltip("Mức ảnh hưởng của khí động học.")]
        [SerializeField] private float m_AerodynamicEffect = 0.02f;

        [Tooltip("Tự động chúi/ngóc khi máy bay nghiêng.")]
        [SerializeField] private float m_AutoTurnPitch = 0.5f;

        [Tooltip("Tự cân bằng trục roll khi không có input.")]
        [SerializeField] private float m_AutoRollLevel = 0.2f;

        [Tooltip("Tự cân bằng trục pitch khi không có input.")]
        [SerializeField] private float m_AutoPitchLevel = 0.2f;

        [Tooltip("Độ mạnh của phanh gió.")]
        [SerializeField] private float m_AirBrakesEffect = 3f;

        [Tooltip("Tốc độ thay đổi ga.")]
        [SerializeField] private float m_ThrottleChangeSpeed = 0.3f;

        [Tooltip("Hệ số tăng lực cản theo tốc độ.")]
        [SerializeField] private float m_DragIncreaseFactor = 0.001f;

        [Tooltip("Tốc độ tối đa của máy bay.")]
        [SerializeField] private float m_MaxSpeed = 10f;

        // ===== THUỘC TÍNH PUBLIC (CHỈ ĐỌC) =====
        public float Altitude { get; private set; }        // Độ cao so với mặt đất
        public float Throttle { get; private set; }        // Mức ga hiện tại
        public bool AirBrakes { get; private set; }        // Trạng thái phanh gió
        public float ForwardSpeed { get; private set; }   // Tốc độ theo hướng mũi máy bay
        public float EnginePower { get; private set; }    // Công suất động cơ hiện tại
        public float RollAngle { get; private set; }      // Góc roll
        public float PitchAngle { get; private set; }     // Góc pitch

        public float RollInput { get; private set; }
        public float PitchInput { get; private set; }
        public float YawInput { get; private set; }
        public float ThrottleInput { get; private set; }

        public float MaxSpeed => m_MaxSpeed;

        public float AerodynamicEffect
        {
            get => m_AerodynamicEffect;
            set => m_AerodynamicEffect = value;
        }

        // ===== BIẾN NỘI BỘ =====
        private float m_OriginalDrag;            // Drag ban đầu
        private float m_OriginalAngularDrag;     // Angular Drag ban đầu
        private float m_AeroFactor;              // Hệ số khí động học
        private bool m_Immobilized = false;      // Máy bay mất điều khiển
        private float m_BankedTurnAmount;
        private Rigidbody m_Rigidbody;

        private void Start()
        {
            m_Rigidbody = GetComponent<Rigidbody>();

            // Lưu lại drag ban đầu
            m_OriginalDrag = m_Rigidbody.linearDamping;
            m_OriginalAngularDrag = m_Rigidbody.angularDamping;

            // Thiết lập mô-men xoắn cho bánh xe
            for (int i = 0; i < transform.childCount; i++)
            {
                foreach (var wheel in transform.GetChild(i).GetComponentsInChildren<WheelCollider>())
                {
                    wheel.motorTorque = 0.18f;
                }
            }
        }

        /// <summary>
        /// Hàm điều khiển chính của máy bay
        /// </summary>
        public void Move(float rollInput, float pitchInput, float yawInput, float throttleInput, bool airBrakes)
        {
            RollInput = rollInput;
            PitchInput = pitchInput;
            YawInput = yawInput;
            ThrottleInput = throttleInput;
            AirBrakes = airBrakes;

            ClampInputs();
            CalculateRollAndPitchAngles();
            AutoLevel();
            CalculateForwardSpeed();
            ControlThrottle();
            CalculateDrag();
            CaluclateAerodynamicEffect();
            CalculateLinearForces();
            CalculateTorque();
            CalculateAltitude();
            LimitVelocity();
        }

        /// <summary>
        /// Giới hạn tốc độ tối đa
        /// </summary>
        private void LimitVelocity()
        {
            if (m_Rigidbody.linearVelocity.sqrMagnitude > m_MaxSpeed * m_MaxSpeed)
            {
                m_Rigidbody.linearVelocity =
                    m_Rigidbody.linearVelocity.normalized * m_MaxSpeed;
            }
        }

        /// <summary>
        /// Giới hạn input trong khoảng [-1, 1]
        /// </summary>
        private void ClampInputs()
        {
            RollInput = Mathf.Clamp(RollInput, -1, 1);
            PitchInput = Mathf.Clamp(PitchInput, -1, 1);
            YawInput = Mathf.Clamp(YawInput, -1, 1);
            ThrottleInput = Mathf.Clamp(ThrottleInput, -1, 1);
        }

        /// <summary>
        /// Tính góc roll và pitch của máy bay
        /// </summary>
        private void CalculateRollAndPitchAngles()
        {
            var flatForward = transform.forward;
            flatForward.y = 0;

            if (flatForward.sqrMagnitude > 0)
            {
                flatForward.Normalize();

                var localFlatForward = transform.InverseTransformDirection(flatForward);
                PitchAngle = Mathf.Atan2(localFlatForward.y, localFlatForward.z);

                var flatRight = Vector3.Cross(Vector3.up, flatForward);
                var localFlatRight = transform.InverseTransformDirection(flatRight);
                RollAngle = Mathf.Atan2(localFlatRight.y, localFlatRight.x);
            }
        }

        /// <summary>
        /// Tự động cân bằng máy bay khi không có input
        /// </summary>
        private void AutoLevel()
        {
            m_BankedTurnAmount = Mathf.Sin(RollAngle);

            if (RollInput == 0f)
                RollInput = -RollAngle * m_AutoRollLevel;

            if (PitchInput == 0f)
            {
                PitchInput = -PitchAngle * m_AutoPitchLevel;
                PitchInput -= Mathf.Abs(
                    m_BankedTurnAmount * m_BankedTurnAmount * m_AutoTurnPitch);
            }
        }

        /// <summary>
        /// Tính tốc độ theo hướng mũi máy bay
        /// </summary>
        private void CalculateForwardSpeed()
        {
            var localVelocity = transform.InverseTransformDirection(m_Rigidbody.linearVelocity);
            ForwardSpeed = Mathf.Max(0, localVelocity.z);
        }

        /// <summary>
        /// Điều khiển ga
        /// </summary>
        private void ControlThrottle()
        {
            if (m_Immobilized)
                ThrottleInput = -0.5f;

            Throttle = Mathf.Clamp01(
                Throttle + ThrottleInput * Time.deltaTime * m_ThrottleChangeSpeed);

            EnginePower = Throttle * m_MaxEnginePower;
        }

        /// <summary>
        /// Tính lực cản không khí
        /// </summary>
        private void CalculateDrag()
        {
            float extraDrag = m_Rigidbody.linearVelocity.magnitude * m_DragIncreaseFactor;

            m_Rigidbody.linearDamping = AirBrakes
                ? (m_OriginalDrag + extraDrag) * m_AirBrakesEffect
                : m_OriginalDrag + extraDrag;

            m_Rigidbody.angularDamping = m_OriginalAngularDrag * ForwardSpeed;
        }

        /// <summary>
        /// Hiệu ứng khí động học
        /// </summary>
        private void CaluclateAerodynamicEffect()
        {
            if (m_Rigidbody.linearVelocity.magnitude > 0)
            {
                m_AeroFactor = Vector3.Dot(
                    transform.forward, m_Rigidbody.linearVelocity.normalized);

                m_AeroFactor *= m_AeroFactor;

                var newVelocity = Vector3.Lerp(
                    m_Rigidbody.linearVelocity,
                    transform.forward * ForwardSpeed,
                    m_AeroFactor * ForwardSpeed * m_AerodynamicEffect * Time.deltaTime);

                m_Rigidbody.linearVelocity = newVelocity;

                if (m_Rigidbody.linearVelocity.sqrMagnitude > MaxSpeed * MaxSpeed * 0.01f)
                {
                    var lookRotation = Quaternion.LookRotation(
                        m_Rigidbody.linearVelocity, transform.up);

                    m_Rigidbody.rotation = Quaternion.Slerp(
                        m_Rigidbody.rotation,
                        lookRotation,
                        m_AerodynamicEffect * Time.deltaTime);
                }
            }
        }

        /// <summary>
        /// Tính các lực tác động tuyến tính
        /// </summary>
        private void CalculateLinearForces()
        {
            var forces = Vector3.zero;

            // Lực đẩy động cơ
            forces += EnginePower * transform.forward;

            // Lực nâng
            var liftDirection =
                Vector3.Cross(m_Rigidbody.linearVelocity, transform.right).normalized;

            var zeroLiftFactor =
                Mathf.InverseLerp(m_ZeroLiftSpeed, 0, ForwardSpeed);

            var liftPower =
                ForwardSpeed * ForwardSpeed * m_Lift * zeroLiftFactor * m_AeroFactor;

            forces += liftPower * liftDirection;

            Debug.DrawRay(transform.position, forces, Color.green);

            m_Rigidbody.AddForce(forces);
        }

        /// <summary>
        /// Tính mô-men xoắn quay máy bay
        /// </summary>
        private void CalculateTorque()
        {
            var torque = Vector3.zero;

            torque += PitchInput * m_PitchEffect * transform.right;
            torque += YawInput * m_YawEffect * transform.up;
            torque += -RollInput * m_RollEffect * transform.forward;
            torque += m_BankedTurnAmount * m_BankedTurnEffect * transform.up;

            m_Rigidbody.AddTorque(torque * ForwardSpeed * m_AeroFactor);
        }

        /// <summary>
        /// Tính độ cao so với mặt đất
        /// </summary>
        private void CalculateAltitude()
        {
            var ray = new Ray(transform.position - Vector3.up * 10, -Vector3.up);
            RaycastHit hit;

            Altitude = Physics.Raycast(ray, out hit)
                ? hit.distance + 10
                : transform.position.y;
        }

        /// <summary>
        /// Làm máy bay mất điều khiển
        /// </summary>
        public void Immobilize()
        {
            m_Immobilized = true;
        }

        /// <summary>
        /// Reset trạng thái máy bay
        /// </summary>
        public void Reset()
        {
            m_Immobilized = false;
        }
    }
}
