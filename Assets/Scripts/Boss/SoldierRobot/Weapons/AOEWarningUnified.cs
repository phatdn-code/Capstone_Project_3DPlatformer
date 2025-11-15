using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    public enum AOEMode { Phase2, Phase3_Inner, Phase3_Outer }

    /// <summary>
    /// Script AOE thống nhất: hiển thị cảnh báo, gây damage và tái sử dụng qua PoolManager.
    /// Dùng cho các Phase 2 & 3 trong boss fight.
    /// </summary>
    public class AOEWarningUnified : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("General Settings")]
        [SerializeField] private AOEMode mode = AOEMode.Phase2;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private GameObject aoeExplosionEffect;
        [SerializeField] private LayerMask playerMask;

        [Header("Explosion Settings")]
        [SerializeField] private int damage = 20;
        [SerializeField] private float radius = 3f;
        [SerializeField] private float warningDuration = 1.5f;

        [Header("Visual Settings")]
        [SerializeField] private Color startColor = Color.red;
        [SerializeField] private Color endColor = Color.white;
        [SerializeField] private float pulseScale = 1.2f;
        [SerializeField] private float pulseSpeed = 0.3f;
        [SerializeField] private float shrinkScale = 0.6f;
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private float groundOffset = 0.05f;
        [SerializeField] private float damageDelay = 0.25f;

        private Material runtimeMat;
        private Coroutine explosionRoutine;
        private BossCore boss;

        #endregion

        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            // Khởi tạo rigidbody
            boss = FindFirstObjectByType<BossCore>();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CONFIGURATION ===

        /// <summary>
        /// ⚙️ Thiết lập thông số AOE khi spawn từ PoolManager.
        /// </summary>
        public void Configure(AOEMode newMode, float radiusOverride, float warnDuration, int dmg)
        {
            mode = newMode;
            radius = radiusOverride;
            warningDuration = warnDuration;
            damage = dmg;
        }

        #endregion
        //─────────────────────────────────────────────
        #region === UNITY EVENTS ===

        /// <summary>
        /// 🔄 Khi object được kích hoạt: reset hiệu ứng & chọn chế độ AOE tương ứng.
        /// </summary>
        private void OnEnable()
        {
            ResetVisualState();
            SnapToGround();

            switch (mode)
            {
                case AOEMode.Phase2:
                    SetupPhase2Effect();
                    break;
                case AOEMode.Phase3_Inner:
                case AOEMode.Phase3_Outer:
                    SetupPhase3Effect();
                    break;
            }

            explosionRoutine = StartCoroutine(ExplodeRoutine());
        }

        /// <summary>
        /// 🧹 Khi object bị tắt: dọn tween và coroutine tránh leak bộ nhớ.
        /// </summary>
        private void OnDisable()
        {
            DOTween.Kill(runtimeMat);
            transform.DOKill();

            if (explosionRoutine != null)
                StopCoroutine(explosionRoutine);
        }

        /// <summary>
        /// 📏 Căn chỉnh vị trí AOE xuống mặt đất.
        /// </summary>
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
            else
            {
                Debug.LogWarning($"⚠️ Không tìm thấy mặt đất cho AOEWarning tại {transform.position}");
            }
        }

        #endregion
        //─────────────────────────────────────────────
        #region === VISUAL EFFECTS ===

        /// <summary>
        /// 🎨 Reset màu và scale ban đầu cho hiệu ứng.
        /// </summary>
        private void ResetVisualState()
        {
            if (meshRenderer != null)
            {
                runtimeMat = meshRenderer.material;
                runtimeMat.color = startColor;
            }

            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// 💥 Hiệu ứng cảnh báo cho Phase 2 (nhấp nháy đỏ).
        /// </summary>
        private void SetupPhase2Effect()
        {
            if (runtimeMat != null)
                runtimeMat.DOColor(endColor, pulseSpeed).SetLoops(-1, LoopType.Yoyo);

            transform.DOScale(Vector3.one * pulseScale, pulseSpeed)
                     .SetLoops(-1, LoopType.Yoyo);
        }

        /// <summary>
        /// 🌪 Hiệu ứng cảnh báo cho Phase 3 (co nhỏ và nhạt dần).
        /// </summary>
        private void SetupPhase3Effect()
        {
            if (runtimeMat != null)
                runtimeMat.DOColor(endColor, warningDuration);

            transform.DOScale(Vector3.one * shrinkScale, warningDuration)
                     .SetEase(Ease.InOutQuad);
        }

        #endregion
        //─────────────────────────────────────────────
        #region === EXPLOSION LOGIC ===

        /// <summary>
        /// ⏱ Đếm ngược thời gian cảnh báo rồi kích hoạt vụ nổ.
        /// </summary>
        private IEnumerator ExplodeRoutine()
        {
            yield return new WaitForSeconds(warningDuration);
            Explode();
        }

        /// <summary>
        /// 💣 Gây damage trễ sau khi hiện effect nổ (pooling).
        /// </summary>
        private void Explode()
        {
            DOTween.Kill(runtimeMat);
            transform.DOKill();

            // 🔸 Spawn hiệu ứng nổ ngay lập tức
            if (aoeExplosionEffect != null)
            {
                Component pooledFx = PoolManager.Instance.ReuseComponent(
                    aoeExplosionEffect, transform.position, Quaternion.identity
                );
                if (pooledFx != null)
                    pooledFx.gameObject.SetActive(true);
            }

            // 🔸 Gây damage trễ và chỉ tắt object sau khi hoàn tất
            StartCoroutine(DelayedDamageRoutine(damageDelay));
        }

        /// <summary>
        /// ⏱ Đợi delay trước khi gây damage thực tế.
        /// </summary>
        private IEnumerator DelayedDamageRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);

            // ⚡ Gây damage sau khi đợi
            Collider[] cols = Physics.OverlapSphere(transform.position, radius, playerMask);
            foreach (var c in cols)
            {
                if (c.CompareTag(GameTags.Player) && c.TryGetComponent<Player>(out var player))
                    if (boss != null && !boss.IsInCutscene)
                        player.ApplyDamage(damage, transform.position);
            }

            // ⚡ Sau khi gây damage xong mới tắt object để PoolManager tái sử dụng
            gameObject.SetActive(false);
        }

        #endregion
        //─────────────────────────────────────────────
#if UNITY_EDITOR
        /// <summary>
        /// 🧭 Vẽ vòng tròn debug trong Editor để thấy phạm vi nổ.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, radius);
        }
#endif
    }
}
