using UnityEngine;

namespace PixPlays.ElementalVFX
{
    /// <summary>
    /// Đạn nước bay theo quỹ đạo bomb:
    /// - Tính vận tốc ban đầu để bay từ start → target trong thời gian nhất định.
    /// - Trong khi bay: cập nhật VFX (ProjectileVfx.UpdateProjectile).
    /// - Khi va chạm: gọi ProjectileVfx.PlayHit, sau đó tự hủy / trả về pool.
    /// - Nếu không va chạm: bay hết thời gian maxLifeTime rồi tự despawn, KHÔNG play hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class WaterProjectile : MonoBehaviour
    {
        [Header("Ballistic Settings")]
        [SerializeField] private float defaultFlightTime = 1.2f;     // Thời gian bay mặc định
        [SerializeField] private float gravityScale = 1.0f;          // Nhân với Physics.gravity.y

        [Header("Lifetime Settings")]
        [SerializeField] private float maxLifeTime = 5f;             // Sống tối đa nếu không trúng gì
        [SerializeField] private float destroyDelayAfterHit = 2f;    // Trễ trước khi despawn sau va chạm

        [Header("Collision Settings")]
        [SerializeField] private LayerMask hitMask = ~0;             // Lớp nào mới tính là trúng

        private Rigidbody _rb;
        private ProjectileVfx _vfx;
        private Collider _collider;

        private bool _hasHit;
        private float _lifeTimer;

        private void Start()
        {
            // Cache toàn bộ component ở Start
            _rb = GetComponent<Rigidbody>();
            _vfx = GetComponent<ProjectileVfx>();
            _collider = GetComponent<Collider>();

            if (_rb != null)
                _rb.useGravity = true;
        }

        private void OnEnable()
        {
            _hasHit = false;
            _lifeTimer = 0f;

            // Bật lại collider mỗi lần spawn
            if (_collider != null)
                _collider.enabled = true;
        }

        private void Update()
        {
            // Cập nhật VFX bay
            if (_vfx != null && !_hasHit && _rb != null)
                _vfx.UpdateProjectile(transform.position, _rb.linearVelocity);

            // Tự hủy nếu quá lâu mà không trúng
            if (!_hasHit)
            {
                _lifeTimer += Time.deltaTime;
                if (_lifeTimer >= maxLifeTime)
                    Despawn();
            }
        }

        /// <summary>
        /// Gọi từ WaterCannon:
        /// - Đặt vị trí ban đầu.
        /// - Tính vận tốc để bay tới target trong flightTimeOverride / defaultFlightTime.
        /// - Gọi VFX cast.
        /// </summary>
        public void Launch(Vector3 startPos, Vector3 targetPos, float flightTimeOverride = -1f)
        {
            transform.position = startPos;
            _hasHit = false;
            _lifeTimer = 0f;

            if (_collider != null)
                _collider.enabled = true;

            if (_rb == null)
                return;

            float t = flightTimeOverride > 0f ? flightTimeOverride : defaultFlightTime;
            Vector3 velocity = CalculateBallisticVelocity(startPos, targetPos, t);

            _rb.linearVelocity = velocity;

            // Cast từ vị trí bắn, hướng theo vận tốc
            if (_vfx != null)
                _vfx.PlayCast(startPos, velocity);
        }

        /// <summary>
        /// Tính vận tốc ban đầu để bay từ start → target trong thời gian t,
        /// có xét gravity (giống logic bomb rơi).
        /// </summary>
        private Vector3 CalculateBallisticVelocity(Vector3 start, Vector3 target, float timeToTarget)
        {
            if (timeToTarget <= 0.01f)
                timeToTarget = 0.3f;

            Vector3 toTarget = target - start;

            // Tách XZ và Y
            Vector3 toTargetXZ = new Vector3(toTarget.x, 0f, toTarget.z);
            float yOffset = toTarget.y;

            float g = Mathf.Abs(Physics.gravity.y) * Mathf.Max(0.0001f, gravityScale);

            // Vận tốc ngang
            Vector3 vXZ = toTargetXZ / timeToTarget;

            // Vận tốc dọc: s = v_y * t - 0.5 * g * t^2 => v_y = (s + 0.5*g*t^2)/t
            float vY = (yOffset + 0.5f * g * timeToTarget * timeToTarget) / timeToTarget;

            Vector3 result = vXZ;
            // Nếu gravity.y âm (mặc định Unity), cần đảo dấu cho đúng hướng
            result.y = vY * -Mathf.Sign(Physics.gravity.y);

            return result;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (_hasHit)
                return;

            // Kiểm tra layer có nằm trong hitMask không
            if (((1 << collision.gameObject.layer) & hitMask) == 0)
                return;

            _hasHit = true;

            Vector3 hitPoint = collision.contacts.Length > 0
                ? collision.contacts[0].point
                : transform.position;

            Vector3 hitNormal = collision.contacts.Length > 0
                ? collision.contacts[0].normal
                : (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.0001f
                    ? -_rb.linearVelocity.normalized
                    : Vector3.up);

            // Dừng chuyển động vật lý
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // Gọi VFX trúng
            if (_vfx != null)
                _vfx.PlayHit(hitPoint, hitNormal);

            // Tắt collider để không va chạm thêm
            if (_collider != null)
                _collider.enabled = false;

            // Đợi một chút cho VFX chạy rồi despawn
            Invoke(nameof(Despawn), destroyDelayAfterHit);
        }

        private void Despawn()
        {
            // Dừng VFX nếu còn
            if (_vfx != null)
                _vfx.StopAll();

            // Nếu sau này dùng Pool thì đổi chỗ này thành: gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }
}
