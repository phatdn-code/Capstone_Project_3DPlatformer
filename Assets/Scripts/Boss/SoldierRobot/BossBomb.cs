using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// BossBomb — điều khiển bom: ném, fuse, nổ, pooling, animation event.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class BossBomb : MonoBehaviour
    {
        //─────────────────────────────────────────────
        // INSPECTOR FIELDS
        //─────────────────────────────────────────────
        [Header("Bomb Settings")]
        [SerializeField] private int damage = 20;
        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private float gravityDelay = 0.7f;
        [SerializeField] private float aimOffset = 1.5f;

        [Header("Fuse & Timing")]
        [SerializeField] private float fuseTime = 3f;
        [SerializeField] private float warningTime = 2f;

        [Header("Animation Settings (DOTween)")]
        [SerializeField] private float maxScale = 1.2f;
        [SerializeField] private float pulseDuration = 1f;
        [SerializeField] private float flashSpeed = 0.2f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private float explosionForce = 500f;
        [SerializeField] private SkinnedMeshRenderer bombRenderer;
        [SerializeField] private GameObject explosionEffect;

        //─────────────────────────────────────────────
        // RUNTIME FIELDS
        //─────────────────────────────────────────────
        private bool hasExploded;
        private bool hasLanded;
        private bool fuseStarted;
        private bool isFromPool;

        private Rigidbody rb;
        private Animator animator;
        private Player target;

        private Material runtimeMat;
        private string colorProp;
        private Color originalColor;

        private Tween pulseTween;
        private Tween flashTween;

        //─────────────────────────────────────────────
        // UNITY LIFECYCLE
        //─────────────────────────────────────────────
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            SetupMaterialReference();
            if (!isFromPool)
                SetupBombPhysics();
        }

        private void Update()
        {
            if (!hasExploded && !hasLanded)
                transform.Rotate(Vector3.up, 180f * Time.deltaTime);
        }

        private void OnDestroy()
        {
            CancelInvoke();
            pulseTween?.Kill();
            flashTween?.Kill();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (hasLanded || hasExploded) return;

            if (collision.gameObject.CompareTag("Ground"))
            {
                hasLanded = true;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;

                DoSquashAndStartFuse();
            }
        }

        //─────────────────────────────────────────────
        // PUBLIC API
        //─────────────────────────────────────────────
        public void SetupFromPool(Player newTarget)
        {
            isFromPool = true;
            target = newTarget;

            ResetBombState();

            Vector3 dir = GetLaunchDirection();
            LaunchBomb(dir);

            Invoke(nameof(EnableGravity), gravityDelay);
        }

        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            pulseTween?.Kill();
            flashTween?.Kill();

            if (runtimeMat != null && colorProp != null)
                runtimeMat.SetColor(colorProp, originalColor);

            animator?.SetTrigger("Explode");
            DealExplosionDamage();
            ApplyExplosionForce();
        }

        public void OnExplosionEvent()
        {
            if (explosionEffect != null)
            {
                var pooled = PoolManager.Instance.ReuseComponent(explosionEffect, transform.position, Quaternion.identity);
                if (pooled == null)
                    Debug.LogWarning("⚠️ PoolManager chưa có pool cho explosionEffect!");
            }

            if (isFromPool)
                gameObject.SetActive(false);
        }

        //─────────────────────────────────────────────
        // FUSE & WARNING LOGIC
        //─────────────────────────────────────────────
        private void DoSquashAndStartFuse()
        {
            transform.DOScale(new Vector3(1.4f, 0.8f, 1.4f), 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    transform.DOScale(Vector3.one, 0.15f).OnComplete(StartFuse);
                });
        }

        private void StartFuse()
        {
            if (fuseStarted) return;
            fuseStarted = true;

            float warningStartTime = fuseTime - warningTime;
            StartPulseAnimation(pulseDuration);

            if (warningStartTime > 0)
                Invoke(nameof(StartWarningAnimation), warningStartTime);

            Invoke(nameof(Explode), fuseTime);
        }

        private void StartWarningAnimation()
        {
            DoFlashWarning();
            StartPulseAnimation(pulseDuration * 0.3f);
        }

        private void StartPulseAnimation(float duration)
        {
            pulseTween?.Kill();
            pulseTween = transform.DOScale(maxScale, duration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void DoFlashWarning()
        {
            flashTween?.Kill();

            flashTween = DOTween.To(
                () => runtimeMat.GetColor(colorProp),
                c => runtimeMat.SetColor(colorProp, c),
                Color.red,
                flashSpeed
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
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
        // INTERNAL HELPERS
        //─────────────────────────────────────────────
        private void SetupMaterialReference()
        {
            runtimeMat = bombRenderer.material;
            colorProp = runtimeMat.HasProperty("_BaseColor") ? "_BaseColor" :
                        runtimeMat.HasProperty("_Color") ? "_Color" :
                        runtimeMat.HasProperty("_MainColor") ? "_MainColor" : null;

            originalColor = colorProp != null
                ? runtimeMat.GetColor(colorProp)
                : Color.white;
        }

        private void ResetBombState()
        {
            hasExploded = false;
            hasLanded = false;
            fuseStarted = false;

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            transform.localScale = Vector3.one;

            if (runtimeMat != null && colorProp != null)
                runtimeMat.SetColor(colorProp, originalColor);

            animator?.Rebind();
            CancelInvoke(nameof(Explode));
        }

        private Vector3 GetLaunchDirection()
        {
            if (target != null)
            {
                Vector3 offset = new(Random.Range(-aimOffset, aimOffset), 0, Random.Range(-aimOffset, aimOffset));
                return (target.transform.position + offset - transform.position).normalized;
            }

            return (transform.forward + new Vector3(Random.Range(-0.3f, 0.3f), 0, Random.Range(-0.3f, 0.3f))).normalized;
        }

        private void LaunchBomb(Vector3 dir)
        {
            rb.linearVelocity = dir * throwForce * speedMultiplier;
        }

        private void EnableGravity()
        {
            if (rb != null && !hasLanded)
                rb.useGravity = true;
        }

        private void SetupBombPhysics()
        {
            rb.isKinematic = false;
            rb.useGravity = true;

            Vector3 randomOffset = new(Random.Range(-0.5f, 0.5f), 0, Random.Range(-0.5f, 0.5f));
            Vector3 direction = (transform.forward + randomOffset).normalized;

            LaunchBomb(direction + Vector3.up * 0.6f);
        }
    }
}
