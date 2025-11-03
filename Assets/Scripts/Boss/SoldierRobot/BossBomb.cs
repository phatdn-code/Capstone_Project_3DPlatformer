using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Animator))]
    public class BossBomb : MonoBehaviour
    {
        //─────────────────────────────────────────────
        #region === INSPECTOR SETTINGS ===

        [Header("Bomb Settings")]
        [SerializeField] private int playerDamage = 1;
        [SerializeField] private int bossDamage = 10;
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

        #endregion

        //─────────────────────────────────────────────
        #region === RUNTIME VARIABLES ===

        private bool hasExploded;
        private bool hasLanded;
        private bool fuseStarted;
        private bool isFromPool;
        private bool isReflected;
        private bool isChasingPlayer;

        private float chaseSpeed = 5f;

        #endregion

        //─────────────────────────────────────────────
        #region === COMPONENT REFERENCES ===

        private SoldierRobot ownerBoss;
        private Rigidbody rb;
        private Animator animator;
        private Player target;
        private Material runtimeMat;
        private string colorProp;
        private Color originalColor;
        private Tween pulseTween;
        private Tween flashTween;

        #endregion

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
        }

        private void Start()
        {
            SetupMaterialReference();
            if (!isFromPool) SetupBombPhysics();
            if (stunEffect != null) stunEffect.SetActive(false);
        }

        private void Update()
        {
            // Xoay bomb khi đang bay
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
            // Bỏ qua nếu đã chạm đất hoặc đã nổ
            if (hasLanded || hasExploded) return;

            if (collision.gameObject.CompareTag("Ground"))
            {
                hasLanded = true;
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = false;

                // Nếu boss ở Phase 2 -> rượt + đếm fuse
                if (ownerBoss != null && ownerBoss.bossHealth.currentPhase >= 1)
                    StartChasingPlayer();

                DoSquashAndStartFuse();
            }


            // Va boss
            if (collision.gameObject.TryGetComponent<SoldierRobot>(out var boss))
                OnHitBoss(boss);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === INITIALIZATION / SETUP ===

        /// <summary>Thiết lập khi bomb được spawn từ pool</summary>
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

        /// <summary>Khởi tạo tham chiếu material runtime</summary>
        private void SetupMaterialReference()
        {
            runtimeMat = bombRenderer.material;
            colorProp = runtimeMat.HasProperty("_BaseColor") ? "_BaseColor"
                : runtimeMat.HasProperty("_Color") ? "_Color"
                : runtimeMat.HasProperty("_MainColor") ? "_MainColor"
                : null;

            originalColor = colorProp != null ? runtimeMat.GetColor(colorProp) : Color.white;
        }

        /// <summary>Reset lại trạng thái của bomb trước khi dùng</summary>
        private void ResetBombState()
        {
            hasExploded = false;
            hasLanded = false;
            fuseStarted = false;
            isReflected = false;

            if (ownerBoss != null && ownerBoss.bossHealth.currentPhase < 2)
                rb.useGravity = false;

            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            transform.localScale = Vector3.one;

            if (runtimeMat != null && colorProp != null)
                runtimeMat.SetColor(colorProp, originalColor);

            animator?.Rebind();
            CancelInvoke(nameof(Explode));
            if (stunEffect != null) stunEffect.SetActive(false);
        }

        #endregion

        //─────────────────────────────────────────────
        #region === CHASING PLAYER (PHASE 2) ===

        /// <summary>Bắt đầu rượt theo player khi boss sang phase 2</summary>
        private void StartChasingPlayer()
        {
            if (target == null || isChasingPlayer) return;
            isChasingPlayer = true;
            animator?.SetBool("IsMove", true);
            StartCoroutine(ChasePlayerRoutine());
        }

        /// <summary>Coroutine di chuyển bomb rượt player</summary>
        private IEnumerator ChasePlayerRoutine()
        {
            while (isChasingPlayer && target != null && !hasExploded)
            {
                Vector3 dir = (target.transform.position - transform.position);
                dir.y = 0f;
                dir.Normalize();
                rb.linearVelocity = dir * chaseSpeed;

                // hướng mặt về phía player
                if (dir.sqrMagnitude > 0.001f)
                {
                    Quaternion lookRot = Quaternion.LookRotation(dir, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 5f);
                }

                yield return null;
            }

            animator?.SetBool("IsMove", false);
        }

        /// <summary>Dừng rượt đuổi player</summary>
        private void StopChasing()
        {
            if (!isChasingPlayer) return;
            isChasingPlayer = false;
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;

            animator?.SetBool("IsRunning", false);
            StopCoroutine(ChasePlayerRoutine());
        }

        #endregion

        //─────────────────────────────────────────────
        #region === FUSE & WARNING ===

        /// <summary>Hiệu ứng dằn nhẹ trước khi bắt đầu fuse</summary>
        private void DoSquashAndStartFuse()
        {
            transform.DOScale(new Vector3(1.4f, 0.8f, 1.4f), 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() => transform.DOScale(Vector3.one, 0.15f).OnComplete(StartFuse));
        }

        /// <summary>Bắt đầu đếm ngược fuse nổ</summary>
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

        /// <summary>Bắt đầu cảnh báo (nhấp nháy đỏ)</summary>
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

        #endregion

        //─────────────────────────────────────────────
        #region === REFLECT / PLAYER HIT ===

        /// <summary>Khi bomb bị player đánh trúng</summary>
        public void OnPlayerHitBomb()
        {
            if (hasExploded || !hasLanded) return;

            StopChasing();
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
            if (boss == null) return;

            // Quỹ đạo phản công về boss
            Vector3 start = transform.position;
            Vector3 end = boss.transform.position + Vector3.up * 1.5f;
            Vector3 mid = (start + end) / 2f + Vector3.up * reflectArcHeight;

            Sequence seq = DOTween.Sequence();
            seq.Append(transform.DOPath(new Vector3[] { start, mid, end }, reflectDuration, PathType.CatmullRom)
                .SetEase(Ease.InOutSine))
                .OnComplete(() => OnHitBoss(boss));
        }

        /// <summary>Khi bomb va trúng boss</summary>
        private void OnHitBoss(SoldierRobot boss)
        {
            if (hasExploded) return;

            if (boss.TryGetComponent<BossHealth>(out var bossHealth))
                bossHealth.TakeDamage(bossDamage);

            if (explosionBossEffect != null)
                PoolManager.Instance.ReuseComponent(explosionBossEffect, transform.position, Quaternion.identity);

            Explode();
        }

        #endregion

        //─────────────────────────────────────────────
        #region === EXPLOSION ===

        /// <summary>Kích nổ bomb</summary>
        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            pulseTween?.Kill();
            flashTween?.Kill();

            if (runtimeMat != null && colorProp != null)
                runtimeMat.SetColor(colorProp, originalColor);

            isChasingPlayer = false;
            animator?.SetBool("IsMove", false);
            animator?.SetTrigger("Explode");

            DealExplosionDamage();
            ApplyExplosionForce();
        }

        /// <summary>Gọi từ animation event khi nổ</summary>
        public void OnExplosionEvent()
        {
            if (explosionEffect != null && !isReflected)
                PoolManager.Instance.ReuseComponent(explosionEffect, transform.position, Quaternion.identity);

            if (isFromPool)
                StartCoroutine(DisableAfterDelay(0.2f));
        }

        private IEnumerator DisableAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            gameObject.SetActive(false);
        }

        /// <summary>Gây sát thương quanh bomb</summary>
        private void DealExplosionDamage()
        {
            if (isReflected) return;
            if (ownerBoss != null && !ownerBoss.IsAlive) return;

            HashSet<Player> damagedPlayers = new HashSet<Player>();
            Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (var col in cols)
            {
                if (col.CompareTag(GameTags.Player) && col.TryGetComponent<Player>(out var player))
                {
                    if (damagedPlayers.Contains(player)) continue;
                    damagedPlayers.Add(player);
                    player.ApplyDamage(playerDamage, transform.position);
                }
            }
        }

        /// <summary>Tác lực nổ đẩy player ra xa</summary>
        private void ApplyExplosionForce()
        {
            if (isReflected) return;
            if (ownerBoss != null && !ownerBoss.IsAlive) return;

            Collider[] cols = Physics.OverlapSphere(transform.position, explosionRadius);
            foreach (var c in cols)
            {
                if (!c.CompareTag(GameTags.Player)) continue;

                if (c.TryGetComponent<Rigidbody>(out var prb))
                {
                    Vector3 dir = (c.transform.position - transform.position).normalized;
                    float mul = 1f - (Vector3.Distance(transform.position, c.transform.position) / explosionRadius);
                    prb.AddForce(dir * explosionForce * mul, ForceMode.Impulse);
                }
            }
        }

        #endregion

        //─────────────────────────────────────────────
        #region === UTILITIES ===

        public void ForceDisableFromBossDeath()
        {
            if (!gameObject.activeInHierarchy) return;
            hasExploded = true;
            CancelInvoke();
            pulseTween?.Kill();
            flashTween?.Kill();
            gameObject.SetActive(false);
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
            Vector3 dir = (transform.forward + randomOffset).normalized;
            LaunchBomb(dir + Vector3.up * 0.6f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
#endif
        #endregion
    }
}
