using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class BossBomb : MonoBehaviour
    {
        //─────────────────────────────────────────────
        // INSPECTOR FIELDS
        [Header("Bomb Settings")]
        [SerializeField] private int playerDamage = 1;      // Damage gây cho Player
        [SerializeField] private int bossDamage = 10;       // Damage gây cho Boss khi bị phản
        [SerializeField] private float throwForce = 12f;
        [SerializeField] private float speedMultiplier = 1.5f;
        [SerializeField] private float gravityDelay = 0.7f;
        [SerializeField] private float aimOffset = 1.5f;

        [Header("Fuse & Timing")]
        [SerializeField] private float fuseTime = 3f;
        [SerializeField] private float warningTime = 2f;

        [Header("Explosion Settings")]
        [SerializeField] private float explosionRadius = 6f;
        [SerializeField] private float explosionForce = 500f;
        [SerializeField] private SkinnedMeshRenderer bombRenderer;
        [SerializeField] private GameObject explosionEffect;
        [SerializeField] private GameObject explosionBossEffect;

        [Header("Reflect Settings")]
        [SerializeField] private GameObject reflectHitEffect;
        [SerializeField] private GameObject stunEffect;
        [SerializeField] private float stunDuration = 0.8f;
        [SerializeField] private float reflectArcHeight = 3f;
        [SerializeField] private float reflectDuration = 1.2f;

        [Header("Animation Settings (DOTween)")]
        [SerializeField] private float maxScale = 1.2f;
        [SerializeField] private float pulseDuration = 1f;
        [SerializeField] private float flashSpeed = 0.2f;

        //─────────────────────────────────────────────
        // RUNTIME
        private bool hasExploded;
        private bool hasLanded;
        private bool fuseStarted;
        private bool isFromPool;
        private bool isReflected;

        private SoldierRobot ownerBoss;
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

            if (stunEffect != null)
                stunEffect.SetActive(false);
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

            // 🔹 Khi bomb rơi xuống đất
            if (collision.gameObject.CompareTag("Ground"))
            {
                hasLanded = true;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
                DoSquashAndStartFuse();
            }

            // 🔹 Nếu bomb đụng boss -> nổ ngay
            if (collision.gameObject.TryGetComponent<SoldierRobot>(out var boss))
                OnHitBoss(boss);
        }

        //─────────────────────────────────────────────
        // SETUP
        public void SetupFromPool(Player newTarget, SoldierRobot boss)
        {
            isFromPool = true;
            target = newTarget;
            ownerBoss = boss;
            ResetBombState();

            Vector3 dir = GetLaunchDirection();
            LaunchBomb(dir);
            Invoke(nameof(EnableGravity), gravityDelay);
        }

        private void SetupMaterialReference()
        {
            runtimeMat = bombRenderer.material;
            colorProp = runtimeMat.HasProperty("_BaseColor") ? "_BaseColor"
                : runtimeMat.HasProperty("_Color") ? "_Color"
                : runtimeMat.HasProperty("_MainColor") ? "_MainColor"
                : null;

            originalColor = colorProp != null ? runtimeMat.GetColor(colorProp) : Color.white;
        }

        private void ResetBombState()
        {
            hasExploded = false;
            hasLanded = false;
            fuseStarted = false;
            isReflected = false;

            rb.isKinematic = false;
            rb.useGravity = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.localScale = Vector3.one;

            if (runtimeMat != null && colorProp != null)
                runtimeMat.SetColor(colorProp, originalColor);

            animator?.Rebind();
            CancelInvoke(nameof(Explode));

            if (stunEffect != null)
                stunEffect.SetActive(false);
        }

        //─────────────────────────────────────────────
        // FUSE & WARNING LOGIC
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
                Color.red, flashSpeed)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        //─────────────────────────────────────────────
        // REFLECT (PHẢN BOMB)
        public void OnPlayerHitBomb()
        {
            if (hasExploded || !hasLanded) return;

            Debug.Log("💥 Bomb bị player đánh trúng! Phản về boss!");
            isReflected = true;

            CancelInvoke(nameof(Explode));
            fuseStarted = false;

            animator?.SetTrigger("TakeDown");
            rb.isKinematic = true;
            rb.useGravity = false;

            if (reflectHitEffect != null)
                PoolManager.Instance.ReuseComponent(reflectHitEffect, transform.position, Quaternion.identity);

            if (stunEffect != null)
                stunEffect.SetActive(true);

            var boss = ownerBoss;

            if (boss == null)
            {
                Debug.LogWarning("⚠️ Không tìm thấy SoldierRobot để phản bomb!");
                return;
            }

            // Quỹ đạo bay cong về boss
            Vector3 start = transform.position;
            Vector3 end = boss.transform.position + Vector3.up * 1.5f;
            Vector3 mid = (start + end) / 2f + Vector3.up * reflectArcHeight;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOPath(new Vector3[] { start, mid, end }, reflectDuration, PathType.CatmullRom)
                .SetEase(Ease.InOutSine))
                .OnComplete(() =>
                {
                    OnHitBoss(boss);
                });
        }

        private void OnHitBoss(SoldierRobot boss)
        {
            if (hasExploded) return;

            if (boss.TryGetComponent<BossHealth>(out var bossHealth))
            {
                bossHealth.TakeDamage(bossDamage);
                Debug.Log($"💥 Bomb phản trúng Boss, gây {bossDamage} sát thương!");
            }

            if (explosionBossEffect != null)
                PoolManager.Instance.ReuseComponent(explosionBossEffect, transform.position, Quaternion.identity);

            Explode();
        }

        //─────────────────────────────────────────────
        // EXPLOSION LOGIC
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
            if (explosionEffect != null && !isReflected)
                PoolManager.Instance.ReuseComponent(explosionEffect, transform.position, Quaternion.identity);

            if (isFromPool)
                gameObject.SetActive(false);
        }

        private void DealExplosionDamage()
        {
            if (isReflected) return;

            HashSet<Player> damagedPlayers = new HashSet<Player>();
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var col in colliders)
            {
                if (col.CompareTag(GameTags.Player) && col.TryGetComponent<Player>(out var player))
                {
                    if (damagedPlayers.Contains(player)) continue;
                    damagedPlayers.Add(player);
                    player.ApplyDamage(playerDamage, transform.position);
                }
            }
        }

        private void ApplyExplosionForce()
        {
            if (isReflected) return;

            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var c in colliders)
            {
                if (!c.CompareTag(GameTags.Player)) continue;

                if (c.TryGetComponent<Rigidbody>(out var prb))
                {
                    Vector3 dir = (c.transform.position - transform.position).normalized;
                    float forceMul = 1f - (Vector3.Distance(transform.position, c.transform.position) / explosionRadius);
                    prb.AddForce(dir * explosionForce * forceMul, ForceMode.Impulse);
                }
            }
        }

        //─────────────────────────────────────────────
        // PHYSICS & LAUNCH HELPERS
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

        //─────────────────────────────────────────────
        // GIZMOS DEBUG VISUALIZATION
        //─────────────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Vẽ phạm vi nổ (vàng)
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
    }
}
