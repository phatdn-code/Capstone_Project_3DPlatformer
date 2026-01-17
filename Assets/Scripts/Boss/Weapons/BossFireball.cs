using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BossFireball — điều khiển cầu lửa của boss (dùng pooling).
    /// Gây sát thương lan & hiệu ứng khi trúng Player hoặc tường.
    /// Không gây damage nếu boss đã chết.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class BossFireball : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR: SETTINGS ===

        [Header("Fireball Settings")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float collisionRadius = 0.1f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 3f;
        [SerializeField] private float explosionForce = 8f;

        [Header("Hit Effect (Particle)")]
        [SerializeField] private GameObject hitEffect;

        [Header("Direction Mode")]
        [SerializeField] private bool useTargetForwardDirection;

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME: CACHED / STATE ===

        private Rigidbody rb;
        private Collider col;

        private Transform target;
        private BossCore ownerBoss;

        private bool hasHit;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>VN: Cache Rigidbody/Collider để dùng nhanh, tránh GetComponent nhiều lần.</summary>
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            col = GetComponent<Collider>();
        }

        /// <summary>VN: Thiết lập Rigidbody cơ bản (không gravity, không kinematic, freeze rotation).</summary>
        private void Start()
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        /// <summary>VN: Mỗi frame quay fireball + kiểm tra va chạm (nếu chưa hit).</summary>
        private void Update()
        {
            if (hasHit) return;

            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            CheckCollision();
        }

        /// <summary>VN: Vẽ gizmo bán kính nổ để debug trong Scene view.</summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === POOL / SETUP ===

        /// <summary>VN: Setup từ Pool (gán target + boss, reset state, bắn đi, start lifetime).</summary>
        public void SetupFromPool(Transform newTarget, BossCore boss)
        {
            target = newTarget;
            ownerBoss = boss;

            gameObject.SetActive(true);

            hasHit = false;
            ResetPhysics();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            Vector3 dir = GetLaunchDirection();
            rb.linearVelocity = dir * speed;

            StopAllCoroutines();
            StartCoroutine(AutoDisableAfterLifetime());
        }

        /// <summary>VN: Tự tắt sau lifetime nếu chưa hit (phục vụ pooling).</summary>
        private IEnumerator AutoDisableAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);

            if (!hasHit)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }
        }

        /// <summary>VN: Reset nhẹ trạng thái khi trả về pool (dừng vận tốc + bật collider).</summary>
        private void ResetForPool()
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            if (col != null) col.enabled = true;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === MOVEMENT / PHYSICS ===

        /// <summary>VN: Tính hướng bắn (theo forward target hoặc bay tới vị trí target).</summary>
        private Vector3 GetLaunchDirection()
        {
            if (target != null)
            {
                Vector3 direction;

                // Bay theo hướng forward của target
                if (useTargetForwardDirection)
                    direction = target.forward;

                // Behavior cũ: bay từ fireball → target.position
                else
                    direction = (target.position - transform.position).normalized;

                direction.y = 0;
                return direction;
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.normalized;
        }

        /// <summary>VN: Reset vận tốc vật lý về 0 trước khi tái sử dụng.</summary>
        private void ResetPhysics()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === COLLISION / HIT ===

        /// <summary>VN: Check va chạm bằng OverlapSphere (trúng Player/Wall thì OnHit).</summary>
        private void CheckCollision()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, collisionRadius);

            foreach (var col in colliders)
            {
                if (col.CompareTag(GameTags.Player))
                {
                    if (col.TryGetComponent<Player>(out var player))
                    {
                        // ⚠️ Boss đã chết thì không gây damage
                        if (ownerBoss == null || !ownerBoss.IsAlive)
                            break;

                        player.ApplyDamage(damage, transform.position);
                    }

                    OnHit();
                    return;
                }

                else if (col.CompareTag("Wall"))
                {
                    OnHit();
                    return;
                }
            }
        }

        /// <summary>VN: Xử lý khi đã hit (khóa hit, tắt collider, dừng RB, nổ, rồi disable sau delay).</summary>
        private void OnHit()
        {
            if (hasHit) return;
            hasHit = true;

            // Chặn va chạm lại ngay lập tức để khỏi spam hit
            if (col != null) col.enabled = false;

            // Dừng chuyển động để không kẹt/đẩy lùi lung tung
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            SpawnHitEffect();
            DealExplosionDamage();
            ApplyExplosionForce();

            StopAllCoroutines();
            StartCoroutine(DisableAfterDelay(0.2f));
        }

        /// <summary>VN: Tắt object sau một khoảng delay ngắn (cho VFX/logic kịp chạy).</summary>
        private IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === VFX / EXPLOSION ===

        /// <summary>VN: Spawn hit effect bằng pool (nếu có).</summary>
        private void SpawnHitEffect()
        {
            if (hitEffect == null) return;

            var pooled = PoolManager.Instance.ReuseComponent(
                hitEffect, transform.position, Quaternion.identity);
        }

        /// <summary>VN: Gây damage lan theo bán kính nổ (boss chết thì skip).</summary>
        private void DealExplosionDamage()
        {
            if (ownerBoss != null && !ownerBoss.IsAlive) return; // 🔒 Boss đã chết => không gây damage

            ForEachPlayerInRange((player, distance, col) =>
            {
                float multiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                multiplier = Mathf.Max(multiplier, 0.1f);
                int finalDamage = Mathf.RoundToInt(damage * multiplier);

                player.ApplyDamage(finalDamage, transform.position);
            });
        }

        /// <summary>VN: Đẩy lực nổ lên Rigidbody của player trong bán kính (boss chết thì skip).</summary>
        private void ApplyExplosionForce()
        {
            if (ownerBoss != null && !ownerBoss.IsAlive) return;

            ForEachPlayerInRange((player, distance, col) =>
            {
                if (col.TryGetComponent<Rigidbody>(out var prb))
                {
                    Vector3 dir = (col.transform.position - transform.position).normalized;
                    float forceMul = 1f - (distance / explosionRadius);
                    prb.AddForce(dir * explosionForce * forceMul, ForceMode.Impulse);
                }
            });
        }

        /// <summary>VN: Duyệt các player trong bán kính và callback action (tái sử dụng cho damage/force).</summary>
        private void ForEachPlayerInRange(System.Action<Player, float, Collider> action)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var c in colliders)
            {
                if (!c.CompareTag(GameTags.Player)) continue;

                if (c.TryGetComponent<Player>(out var player))
                {
                    float distance = Vector3.Distance(transform.position, c.transform.position);
                    action?.Invoke(player, distance, c);
                }
            }
        }

        #endregion

        //─────────────────────────────────────────────
        #region === EXTERNAL CONTROL ===

        /// <summary>VN: Boss chết gọi hàm này để tắt ngay fireball (dọn coroutine + dừng RB).</summary>
        public void ForceDisableFromBossDeath()
        {
            if (!gameObject.activeInHierarchy) return;

            hasHit = true;
            StopAllCoroutines();

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            gameObject.SetActive(false);
        }

        #endregion
    }
}
