using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BossFireball — điều khiển cầu lửa của boss (dùng pooling).
    /// Tự chứa toàn bộ thiết lập damage/speed/lifetime.
    /// Gây sát thương lan & hiệu ứng khi trúng Player hoặc tường.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public class BossFireball : MonoBehaviour
    {
        //─────────────────────────────────────────────
        // INSPECTOR FIELDS
        //─────────────────────────────────────────────
        [Header("Fireball Settings")]
        [SerializeField] private int damage = 25;
        [SerializeField] private float speed = 12f;
        [SerializeField] private float lifetime = 10f;
        [SerializeField] private float collisionRadius = 0.1f;

        [Header("Explosion Settings")]
        [Tooltip("Bán kính gây sát thương lan")]
        [SerializeField] private float explosionRadius = 3f;
        [Tooltip("Lực đẩy lên Player trong vùng nổ")]
        [SerializeField] private float explosionForce = 8f;

        [Header("Hit Effect (Particle)")]
        [Tooltip("Hiệu ứng khi cầu lửa trúng Player hoặc tường")]
        [SerializeField] private GameObject hitEffect;

        //─────────────────────────────────────────────
        // RUNTIME FIELDS
        //─────────────────────────────────────────────
        private Rigidbody rb;
        private bool hasHit;
        private Player target;

        //─────────────────────────────────────────────
        // UNITY LIFECYCLE
        //─────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        private void Start()
        {
            rb.useGravity = false;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        private void Update()
        {
            if (hasHit) return;
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);
            CheckCollision();
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }

        //─────────────────────────────────────────────
        // PUBLIC API
        //─────────────────────────────────────────────
        /// <summary>
        /// Setup từ Pool — chỉ cần truyền Player target.
        /// Các thông số khác giữ nguyên trong prefab.
        /// </summary>
        public void SetupFromPool(Player newTarget)
        {
            target = newTarget;
            hasHit = false;
            ResetPhysics();

            Vector3 dir = GetLaunchDirection();
            rb.linearVelocity = dir * speed;

            StopAllCoroutines();
            StartCoroutine(AutoDisableAfterLifetime());
            gameObject.SetActive(true);
        }

        //─────────────────────────────────────────────
        // INTERNAL HELPERS
        //─────────────────────────────────────────────
        private Vector3 GetLaunchDirection()
        {
            if (target != null)
            {
                Vector3 direction = (target.transform.position - transform.position).normalized;
                direction.y = 0;
                return direction;
            }

            Vector3 fallback = transform.forward;
            fallback.y = 0f;
            return fallback.normalized;
        }

        private void ResetPhysics()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        //─────────────────────────────────────────────
        // COLLISION
        //─────────────────────────────────────────────
        private void CheckCollision()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, collisionRadius);

            foreach (var col in colliders)
            {
                if (col.CompareTag(GameTags.Player))
                {
                    if (col.TryGetComponent<Player>(out var player))
                        player.ApplyDamage(damage, transform.position);

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

        private void OnHit()
        {
            if (hasHit) return;
            hasHit = true;

            SpawnHitEffect();
            DealExplosionDamage();
            ApplyExplosionForce();

            ResetForPool();
            gameObject.SetActive(false);
        }

        //─────────────────────────────────────────────
        // EFFECTS
        //─────────────────────────────────────────────
        private void SpawnHitEffect()
        {
            if (hitEffect == null) return;

            var pooled = PoolManager.Instance.ReuseComponent(
                hitEffect, transform.position, Quaternion.identity);

            if (pooled == null)
                Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        //─────────────────────────────────────────────
        // EXPLOSION LOGIC
        //─────────────────────────────────────────────
        private void DealExplosionDamage()
        {
            ForEachPlayerInRange((player, distance, col) =>
            {
                float multiplier = Mathf.Clamp01(1f - (distance / explosionRadius));
                multiplier = Mathf.Max(multiplier, 0.1f);
                int finalDamage = Mathf.RoundToInt(damage * multiplier);

                player.ApplyDamage(finalDamage, transform.position);
            });
        }

        private void ApplyExplosionForce()
        {
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

        //─────────────────────────────────────────────
        // CLEANUP
        //─────────────────────────────────────────────
        private void ResetForPool()
        {
            hasHit = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            StopAllCoroutines();
        }

        private IEnumerator AutoDisableAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);
            if (!hasHit)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }
        }
    }
}
