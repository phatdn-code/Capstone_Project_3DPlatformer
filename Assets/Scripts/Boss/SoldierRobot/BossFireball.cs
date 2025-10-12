using System.Collections;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Cầu lửa của boss (dùng pooling).
    /// Khi trúng player hoặc tường:
    /// - Hiển thị hit effect (particle).
    /// - Gây sát thương lan trong bán kính nhỏ.
    /// - Đẩy các player gần đó (explosionForce).
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
        public void SetupFromPool(int dmg, float spd, float life)
        {
            damage = dmg;
            speed = spd;
            lifetime = life;

            hasHit = false;
            ResetPhysics();

            Vector3 dir = transform.forward;
            dir.y = 0f;
            dir.Normalize();
            rb.linearVelocity = dir * speed;

            StopAllCoroutines();
            StartCoroutine(AutoDisableAfterLifetime());
            gameObject.SetActive(true);
        }

        //─────────────────────────────────────────────
        // PHYSICS RESET
        //─────────────────────────────────────────────
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

            // 🔹 Hiệu ứng va chạm
            SpawnHitEffect();

            // 🔹 Gây damage lan & lực đẩy
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
        // UTILITY
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
