using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// WaterProjectile: đạn nước bay theo hướng + gravity, hit thì play VFX và despawn.
    /// Logic mới: Shield active thì không trừ máu boss; shield mất thì hit boss sẽ trừ máu.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(ProjectileVfx))]
    public class WaterProjectile : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR ===

        [Header("Damage")]
        [SerializeField] private int damageToBoss = 10; // Sát thương lên boss khi KHÔNG còn shield

        [Header("Speed Settings")]
        [SerializeField] private float projectileSpeed = 10f;

        [Header("Lifetime Settings")]
        [SerializeField] private float maxLifeTime = 5f;
        [SerializeField] private float destroyDelayAfterHit = 2f;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME ===

        private Rigidbody _rb;
        private ProjectileVfx _vfx;
        private Collider _collider;

        private bool _hasHit;
        private float _lifeTimer;

        #endregion

        //────────────────────────────────────────────────────
        #region === INIT ===

        /// <summary>Cache component cần dùng.</summary>
        private void CacheComponents()
        {
            if (_rb == null) _rb = GetComponent<Rigidbody>();
            if (_vfx == null) _vfx = GetComponent<ProjectileVfx>();
            if (_collider == null) _collider = GetComponent<Collider>();
        }

        /// <summary>Start: cache và bật gravity.</summary>
        private void Start()
        {
            CacheComponents();
            if (_rb != null) _rb.useGravity = true;
        }

        /// <summary>OnEnable: reset trạng thái khi spawn lại.</summary>
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
        #region === UPDATE ===

        /// <summary>Update: cập nhật VFX bay + tự despawn khi hết lifetime.</summary>
        private void Update()
        {
            if (_vfx != null && !_hasHit && _rb != null)
                _vfx.UpdateProjectile(transform.position, _rb.linearVelocity);

            if (_hasHit) return;

            _lifeTimer += Time.deltaTime;
            if (_lifeTimer >= maxLifeTime)
                Despawn();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === LAUNCH ===

        /// <summary>Bắn đạn theo hướng forward.</summary>
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

            _vfx?.StartProjectile(startPos, velocity);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === HIT / DAMAGE ===

        /// <summary>OnTriggerEnter: hit thì play VFX, xử lý shield/boss, rồi despawn.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_hasHit) return;

            _hasHit = true;

            Vector3 hitPoint = transform.position;
            Vector3 hitNormal =
                (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.0001f)
                    ? -_rb.linearVelocity.normalized
                    : Vector3.up;

            StopPhysics();

            // 1) Nếu đang đụng shield active -> absorb, không trừ máu boss
            if (IsShieldActiveOnHit(other))
            {
                PlayHitVfx(hitPoint, hitNormal);
                DisableColliderAndDespawn();
                return;
            }

            // 2) Nếu shield không active -> thử trừ máu boss
            TryDamageBoss(other);

            PlayHitVfx(hitPoint, hitNormal);
            DisableColliderAndDespawn();
        }

        /// <summary>Ngừng rigidbody để projectile đứng yên khi hit.</summary>
        private void StopPhysics()
        {
            if (_rb == null) return;
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        /// <summary>Kiểm tra shield trong parent có đang active không.</summary>
        private bool IsShieldActiveOnHit(Collider other)
        {
            // BossShieldController là script bạn đã tạo để quản lý shield
            BossShieldController shield = other.GetComponentInParent<BossShieldController>();
            return shield != null && shield.IsActive;
        }

        /// <summary>Nếu trúng boss (hoặc child collider của boss) thì trừ máu.</summary>
        private void TryDamageBoss(Collider other)
        {
            if (damageToBoss <= 0) return;

            // Collider có thể nằm ở child (Skin/Hitbox), nên lấy theo parent
            BossHealth bossHealth = other.GetComponentInParent<BossHealth>();
            if (bossHealth == null) return;

            DragonRobot dragon = other.GetComponentInParent<DragonRobot>();
            if (dragon != null && dragon.IsDamageImmuneThisRound) return;

            bossHealth.TakeDamage(damageToBoss);

        }

        /// <summary>Play VFX khi hit.</summary>
        private void PlayHitVfx(Vector3 hitPoint, Vector3 hitNormal)
        {
            _vfx?.PlayHit(hitPoint, hitNormal);
        }

        /// <summary>Tắt collider để không hit nhiều lần, rồi despawn sau delay.</summary>
        private void DisableColliderAndDespawn()
        {
            if (_collider != null)
                _collider.enabled = false;

            Invoke(nameof(Despawn), destroyDelayAfterHit);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === DESPAWN ===

        /// <summary>Stop VFX và hủy projectile (hoặc đổi sang pool sau).</summary>
        private void Despawn()
        {
            _vfx?.StopAll();
            Destroy(gameObject);
        }

        #endregion
    }
}
