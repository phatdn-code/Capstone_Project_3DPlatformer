using PLAYERTWO.PlatformerProject;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Đạn nước bay theo quỹ đạo bomb:
    /// - Có 2 kiểu:
    ///   + Launch(target): tính vận tốc để bay từ start → target trong thời gian định trước.
    ///   + LaunchForward: bay theo hướng forward với tốc độ cố định, cong xuống vì gravity.
    /// - Trong khi bay: cập nhật VFX (ProjectileVfx.UpdateProjectile).
    /// - Khi va chạm: gọi ProjectileVfx.PlayHit rồi tự hủy / trả pool.
    /// - Nếu không va chạm: hết maxLifeTime thì tự despawn, KHÔNG play hit.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(ProjectileVfx))]
    public class WaterProjectile : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Speed Settings")]
        [SerializeField] private float projectileSpeed = 10f;        // Tốc độ bắn khi dùng LaunchForward

        [Header("Lifetime Settings")]
        [SerializeField] private float maxLifeTime = 5f;             // Sống tối đa nếu không trúng gì
        [SerializeField] private float destroyDelayAfterHit = 2f;    // Trễ trước khi despawn sau va chạm

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private Rigidbody _rb;
        private ProjectileVfx _vfx;
        private Collider _collider;

        private bool _hasHit;
        private float _lifeTimer;

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === INITIALIZATION / STATE ===

        /// <summary>
        /// Cache các component cần dùng (gọi lại được nhiều lần, an toàn nếu null).
        /// </summary>
        private void CacheComponents()
        {
            if (_rb == null)
                _rb = GetComponent<Rigidbody>();

            if (_vfx == null)
                _vfx = GetComponent<ProjectileVfx>();

            if (_collider == null)
                _collider = GetComponent<Collider>();
        }

        /// <summary>
        /// Gọi lúc Start: cache component + bật gravity.
        /// </summary>
        private void Start()
        {
            CacheComponents();

            if (_rb != null)
                _rb.useGravity = true;
        }

        /// <summary>
        /// Reset trạng thái mỗi lần đạn được bật (spawn lại).
        /// </summary>
        private void OnEnable()
        {
            CacheComponents();

            _hasHit = false;
            _lifeTimer = 0f;

            if (_collider != null)
                _collider.enabled = true;
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === UPDATE LOOP ===

        /// <summary>
        /// Cập nhật VFX đạn bay + kiểm tra thời gian sống.
        /// </summary>
        private void Update()
        {
            // Cập nhật VFX đạn bay
            if (_vfx != null && !_hasHit && _rb != null)
                _vfx.UpdateProjectile(transform.position, _rb.linearVelocity);

            // Tự hủy nếu không trúng gì trong thời gian maxLifeTime
            if (!_hasHit)
            {
                _lifeTimer += Time.deltaTime;
                if (_lifeTimer >= maxLifeTime)
                    Despawn();
            }
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === LAUNCH API ===

        /// <summary>
        /// Bắn theo hướng forward:
        /// - Đạn bay theo direction rồi cong xuống vì gravity.
        /// - Không cần truyền target, chỉ cần hướng súng.
        /// </summary>
        public void LaunchForward(Vector3 startPos, Vector3 direction)
        {
            CacheComponents();

            transform.position = startPos;
            _hasHit = false;
            _lifeTimer = 0f;

            if (_collider != null)
                _collider.enabled = true;

            if (_rb == null)
                return;

            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            Vector3 velocity = direction.normalized * projectileSpeed;

            _rb.linearVelocity = velocity;

            if (_vfx != null)
                _vfx.StartProjectile(startPos, velocity);
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === COLLISION / DESPAWN ===

        /// <summary>
        /// Xử lý va chạm trigger:
        /// - Luôn play hit VFX (dù có phải Boss hay không).
        /// - Nếu đụng đúng tag Boss, thử lấy DragonRobot và Debug "Hello".
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit)
                return;

            _hasHit = true;

            // Với trigger không có contact point, dùng vị trí projectile làm hitPoint
            Vector3 hitPoint = transform.position;

            // Normal ước lượng ngược hướng bay, fallback lên Vector3.up
            Vector3 hitNormal =
                (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.0001f)
                    ? -_rb.linearVelocity.normalized
                    : Vector3.up;

            // Dừng chuyển động vật lý
            if (_rb != null)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }

            // Nếu là Boss thì thử lấy DragonRobot và debug
            if (other.TryGetComponent(out DragonRobot boss))
            {
                Debug.Log("Hello từ WaterProjectile – trúng DragonRobot!");
                // Sau này bạn có thể gọi boss.TakeDamage(...) tại đây.
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

        /// <summary>
        /// Dừng VFX và hủy đạn (sau này có thể đổi thành trả về pool).
        /// </summary>
        private void Despawn()
        {
            if (_vfx != null)
                _vfx.StopAll();

            // Nếu sau này dùng Pool thì đổi chỗ này thành: gameObject.SetActive(false);
            Destroy(gameObject);
        }

        #endregion
        //────────────────────────────────────────────────────
    }
}
