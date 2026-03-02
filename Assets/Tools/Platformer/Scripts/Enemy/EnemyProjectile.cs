using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// EnemyProjectile — projectile cho enemy (dùng pooling).
    /// - Nhận damage qua Init(int).
    /// - Tự tắt sau lifetime hoặc khi va chạm Player/Wall.
    /// - Có thể bật VFX khi hit (tùy chọn).
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class EnemyProjectile : MonoBehaviour
    {
        #region ===== INSPECTOR =====

        [Header("Projectile Settings")]
        [SerializeField] private float lifetime = 6f;
        [SerializeField] private float collisionRadius = 0.12f;

        [Header("Hit Settings")]
        [SerializeField] private string wallTag = "Wall";
        [SerializeField] private bool rotateVisual = true;
        [SerializeField] private float rotateSpeed = 180f;

        [Header("Hit Effect (Optional)")]
        [SerializeField] private GameObject hitEffect;

        #endregion

        #region ===== RUNTIME =====

        private Rigidbody rb;
        private Collider col;

        private int damage = 1;
        private bool hasHit;
        private Coroutine lifetimeRoutine;

        #endregion

        #region ===== UNITY =====

        /// <summary>Cache Rigidbody/Collider.</summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        /// <summary>Thiết lập Rigidbody cơ bản.</summary>
        private void Start()
        {
            if (rb == null) return;

            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>Reset state khi object được bật lại từ pool.</summary>
        private void OnEnable()
        {
            hasHit = false;

            if (col != null)
                col.enabled = true;

            ResetPhysics();

            RestartLifetime();
        }

        /// <summary>Quay projectile + check collision (nếu dùng overlap).</summary>
        private void Update()
        {
            if (hasHit) return;

            if (rotateVisual)
                transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);

            CheckCollisionOverlap();
        }

        #endregion

        #region ===== PUBLIC API (ENEMY CALLS) =====

        /// <summary>
        /// Nhận damage từ Enemy qua SendMessage("Init", dmg).
        /// </summary>
        public void Init(int newDamage)
        {
            damage = Mathf.Max(0, newDamage);
        }

        /// <summary>
        /// Nếu bạn muốn bắn kiểu "projectile tự set velocity", có thể gọi hàm này.
        /// (Không bắt buộc nếu Enemy đã set rb.velocity rồi.)
        /// </summary>
        public void Launch(Vector3 direction, float speed)
        {
            if (rb == null) return;

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = transform.forward;

            direction.Normalize();
            rb.linearVelocity = direction * speed;
        }

        #endregion

        #region ===== COLLISION =====

        /// <summary>
        /// Check va chạm bằng OverlapSphere (ổn định cho projectile pooling).
        /// </summary>
        private void CheckCollisionOverlap()
        {
            if (collisionRadius <= 0f) return;

            Collider[] hits = Physics.OverlapSphere(transform.position, collisionRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (h == null) continue;

                if (h.CompareTag(GameTags.Player))
                {
                    if (h.TryGetComponent<Player>(out var player))
                        player.ApplyDamage(damage, transform.position);

                    OnHit();
                    return;
                }

                if (!string.IsNullOrEmpty(wallTag) && h.CompareTag(wallTag))
                {
                    OnHit();
                    return;
                }
            }
        }

        /// <summary>
        /// Nếu bạn dùng collider trigger, vẫn hỗ trợ OnTriggerEnter.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (hasHit) return;

            if (other.CompareTag(GameTags.Player))
            {
                if (other.TryGetComponent<Player>(out var player))
                    player.ApplyDamage(damage, transform.position);

                OnHit();
                return;
            }

            if (!string.IsNullOrEmpty(wallTag) && other.CompareTag(wallTag))
            {
                OnHit();
            }
        }

        /// <summary>
        /// Nếu collider không phải trigger, vẫn hỗ trợ OnCollisionEnter.
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (hasHit) return;

            var other = collision.collider;
            if (other == null) return;

            if (other.CompareTag(GameTags.Player))
            {
                if (other.TryGetComponent<Player>(out var player))
                    player.ApplyDamage(damage, transform.position);

                OnHit();
                return;
            }

            if (!string.IsNullOrEmpty(wallTag) && other.CompareTag(wallTag))
            {
                OnHit();
            }
        }

        /// <summary>
        /// Xử lý khi trúng: khóa hit, tắt collider, dừng RB, spawn VFX, rồi disable.
        /// </summary>
        private void OnHit()
        {
            if (hasHit) return;
            hasHit = true;

            if (col != null)
                col.enabled = false;

            ResetPhysics();

            SpawnHitEffect();

            StopLifetime();
            StartCoroutine(DisableAfterDelay(0.05f));
        }

        #endregion

        #region ===== LIFETIME / POOL =====

        /// <summary>Tự tắt sau lifetime.</summary>
        private void RestartLifetime()
        {
            StopLifetime();

            if (lifetime > 0f)
                lifetimeRoutine = StartCoroutine(AutoDisableAfterLifetime());
        }

        /// <summary>Dừng coroutine lifetime (nếu có).</summary>
        private void StopLifetime()
        {
            if (lifetimeRoutine == null) return;
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        /// <summary>Coroutine: hết thời gian thì tắt để trả về pool.</summary>
        private IEnumerator AutoDisableAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);

            if (!hasHit)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }
        }

        /// <summary>Reset nhẹ khi trả về pool.</summary>
        private void ResetForPool()
        {
            ResetPhysics();

            if (col != null)
                col.enabled = true;
        }

        /// <summary>Tắt sau delay ngắn để tránh hit lặp.</summary>
        private IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        #endregion

        #region ===== HELPERS =====

        /// <summary>Reset velocity vật lý.</summary>
        private void ResetPhysics()
        {
            if (rb == null) return;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        /// <summary>Spawn hit effect bằng PoolManager nếu có.</summary>
        private void SpawnHitEffect()
        {
            if (hitEffect == null) return;
            if (PoolManager.Instance == null) return;

            PoolManager.Instance.ReuseComponent(hitEffect, transform.position, Quaternion.identity);
        }

        #endregion
    }
}