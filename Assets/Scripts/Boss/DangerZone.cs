using UnityEngine;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// AOE đơn giản:
    /// - Chờ warningDuration
    /// - Tắt vòng DangerZone
    /// - Spawn hiệu ứng nổ
    /// - (optional) Gây damage lên player
    /// - SetActive(false) để PoolManager tái sử dụng
    /// </summary>
    public class DangerZone : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("Visual Roots")]
        [SerializeField] private GameObject dangerZoneVisualRoot;   // VFX vòng DangerZone (telegraph)

        [Header("Explosion / VFX")]
        [SerializeField] private GameObject aoeExplosionEffect;     // VFX nổ

        [Header("Explosion Settings")]
        [SerializeField] private int damage = 20;
        [SerializeField] private float radius = 3f;
        [SerializeField] private float warningDuration = 1.5f;
        [SerializeField] private float damageDelay = 0.25f;

        [Header("Position Settings")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private float groundOffset = 0.05f;

        [Header("Damage Control")]
        [SerializeField] private bool enableDamage = true;          // false → chỉ VFX, không gây damage

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private Coroutine explosionRoutine;
        private BossCore boss;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            boss = FindFirstObjectByType<BossCore>();
        }

        private void OnEnable()
        {
            // Khi object được reuse từ pool → bật lại vòng DangerZone
            if (dangerZoneVisualRoot != null)
                dangerZoneVisualRoot.SetActive(true);

            SnapToGround();
            explosionRoutine = StartCoroutine(ExplodeRoutine());
        }

        private void OnDisable()
        {
            if (explosionRoutine != null)
                StopCoroutine(explosionRoutine);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CONFIGURATION ===

        /// <summary>
        /// Thiết lập thông số AOE khi spawn từ PoolManager.
        /// </summary>
        public void Configure(float radiusOverride, float warnDuration, int dmg)
        {
            radius = radiusOverride;
            warningDuration = warnDuration;
            damage = dmg;
        }

        #endregion

        //─────────────────────────────────────────────
        #region === POSITION / GROUND SNAP ===

        private void SnapToGround()
        {
            if (!snapToGround) return;

            Vector3 start = transform.position + Vector3.up * 5f;
            if (Physics.Raycast(start, Vector3.down, out var hit, 10f, ~0, QueryTriggerInteraction.Ignore))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y + groundOffset;
                transform.position = pos;
            }

            else Debug.LogWarning($"⚠️ Không tìm thấy mặt đất cho DangerZone tại {transform.position}");
        }

        #endregion

        //─────────────────────────────────────────────
        #region === EXPLOSION LOGIC ===

        private IEnumerator ExplodeRoutine()
        {
            yield return new WaitForSeconds(warningDuration);
            Explode();
        }

        private void Explode()
        {
            // Tắt vòng DangerZone (telegraph) khi nổ
            if (dangerZoneVisualRoot != null)
                dangerZoneVisualRoot.SetActive(false);

            // Spawn VFX nổ
            if (aoeExplosionEffect != null)
            {
                var pooledFx = PoolManager.Instance.ReuseComponent(
                    aoeExplosionEffect, transform.position, Quaternion.identity
                );

                if (pooledFx != null)
                    pooledFx.gameObject.SetActive(true);
            }

            // Nếu boss này không dùng kiểu damage này → chỉ VFX rồi tắt object
            if (!enableDamage)
            {
                gameObject.SetActive(false);
                return;
            }

            // Gây damage sau 1 khoảng delay
            StartCoroutine(DelayedDamageRoutine(damageDelay));
        }

        private IEnumerator DelayedDamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            Collider[] cols = Physics.OverlapSphere(transform.position, radius);

            foreach (var c in cols)
            {
                if (!c.CompareTag(GameTags.Player)) continue;

                if (c.TryGetComponent<Player>(out var player))
                {
                    if (boss != null && !boss.IsInCutscene)
                        player.ApplyDamage(damage, transform.position);
                }
            }

            gameObject.SetActive(false);
        }

        #endregion

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
