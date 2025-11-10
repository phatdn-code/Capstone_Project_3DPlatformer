using PLAYERTWO.PlatformerProject;
using System.Collections;
using UnityEngine;

/// <summary>
/// 👹 AI quái phụ Phase 2:
/// - Bay quanh vòng chiến đấu và bắn về tâm.
/// - Không dùng NavMesh.
/// - Animation điều khiển qua Bool: IsRunning, IsShooting.
/// - Khi bắn: IsRunning = false, IsShooting = true.
/// - Khi nghỉ: IsRunning = true, IsShooting = false.
/// - Sử dụng PoolManager để spawn bullet & flash.
/// </summary>
public class SupportGunnerAI : MonoBehaviour
{
    //─────────────────────────────────────────────
    #region ✦ THAM CHIẾU INSPECTOR ✦

    [Header("Tham chiếu đối tượng")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Animator animator;

    [Header("Cài đặt di chuyển")]
    [SerializeField] private float moveSpeed = 10f;        // tốc độ chạy vào vòng
    [SerializeField] private float orbitSpeed = 5f;        // tốc độ bay quanh vòng
    [SerializeField] private float orbitSmooth = 8f;       // độ mượt khi di chuyển
    [SerializeField] private float orbitHeightOffset = 0.5f; // độ cao khi bay
    [SerializeField] private float orbitDistanceOffset = 2f; // khoảng cách bay cách vòng tròn

    [Header("Cài đặt chiến đấu")]
    [SerializeField] private float fireRate = 0.3f;        // tốc độ bắn
    [SerializeField] private float burstDuration = 3f;     // thời gian bắn liên tục
    [SerializeField] private float restDuration = 2.5f;    // thời gian nghỉ giữa các đợt bắn

    #endregion
    //─────────────────────────────────────────────
    #region ✦ BIẾN NỘI BỘ ✦

    private Vector3 orbitCenter;
    private float orbitRadius;
    private bool isActive;
    private bool isOrbiting;
    private bool isShooting;
    private bool isRunning;

    // Lưu vị trí và hướng ban đầu để về sau quay lại
    private Vector3 idlePosition;
    private Quaternion idleRotation;

    private readonly int hashIsRunning = Animator.StringToHash("IsRunning");
    private readonly int hashIsShooting = Animator.StringToHash("IsShooting");

    #endregion
    //─────────────────────────────────────────────
    #region ✦ UNITY LIFECYCLE ✦

    private void Start()
    {
        // ✅ Lưu vị trí và rotation ngay khi start
        idlePosition = transform.position;
        idleRotation = transform.rotation;
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ KÍCH HOẠT AI ✦

    /// <summary>
    /// 🔹 Gọi khi boss chuyển sang Phase 2.
    /// </summary>
    public void ActivateGunner(Vector3 center, float radius)
    {
        if (isActive) return;

        orbitCenter = center;
        orbitRadius = radius + orbitDistanceOffset; // ✅ bay xa hơn khỏi tâm
        isActive = true;

        StartCoroutine(EnterArenaRoutine());
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ VÀO VÒNG CHIẾN ✦

    private IEnumerator EnterArenaRoutine()
    {
        SetRunning(true);
        SetShooting(false);

        Vector3 dirToCenter = (transform.position - orbitCenter).normalized;
        Vector3 target = orbitCenter + dirToCenter * orbitRadius;
        target.y = orbitCenter.y + orbitHeightOffset;

        while (Vector3.Distance(transform.position, target) > 0.2f)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 10f);
            yield return null;
        }

        yield return new WaitForSeconds(0.3f);

        isOrbiting = true;
        SetRunning(true);

        StartCoroutine(OrbitMovementRoutine());
        StartCoroutine(FireCycleRoutine());
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ CHUYỂN ĐỘNG BAY QUANH VÒNG ✦

    private IEnumerator OrbitMovementRoutine()
    {
        float currentAngle = Mathf.Atan2(transform.position.z - orbitCenter.z, transform.position.x - orbitCenter.x);

        while (isOrbiting)
        {
            currentAngle -= orbitSpeed * Time.deltaTime / orbitRadius;

            Vector3 targetPos = orbitCenter + new Vector3(Mathf.Cos(currentAngle), 0f, Mathf.Sin(currentAngle)) * orbitRadius;
            targetPos.y = orbitCenter.y + orbitHeightOffset;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * orbitSmooth);

            Vector3 dirFromCenter = (transform.position - orbitCenter).normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, dirFromCenter).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(tangent), Time.deltaTime * 8f);

            yield return null;
        }
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ CHU KỲ BẮN ✦

    private IEnumerator FireCycleRoutine()
    {
        while (isOrbiting)
        {
            SetRunning(false);
            SetShooting(true);

            float timer = 0f;
            while (timer < burstDuration)
            {
                FireBullet();
                timer += fireRate;
                yield return new WaitForSeconds(fireRate);
            }

            SetShooting(false);
            SetRunning(true);

            yield return new WaitForSeconds(restDuration);
        }
    }

    private void FireBullet()
    {
        if (!bulletPrefab || !firePoint) return;

        float randomOffset = Random.Range(-5f, 5f);
        Quaternion spread = Quaternion.Euler(0, randomOffset, 0);
        Vector3 dir = spread * (orbitCenter - firePoint.position).normalized;

        // 🔸 Spawn bullet từ PoolManager
        Component bulletComp = PoolManager.Instance.ReuseComponent(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        if (bulletComp != null && bulletComp.TryGetComponent(out BulletProjectile bullet))
            bullet.Fire(dir);

        // 🔸 Spawn muzzle flash
        if (muzzleFlashPrefab)
        {
            Component flash = PoolManager.Instance.ReuseComponent(muzzleFlashPrefab, firePoint.position, Quaternion.LookRotation(dir));
            if (flash != null)
                StartCoroutine(DisableAfterSeconds(flash.gameObject, 0.1f));
        }
    }

    private IEnumerator DisableAfterSeconds(GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);
        obj.SetActive(false);
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ QUẢN LÝ ANIMATION ✦

    private void SetRunning(bool value)
    {
        if (isRunning == value) return;
        isRunning = value;
        animator?.SetBool(hashIsRunning, value);
    }

    private void SetShooting(bool value)
    {
        if (isShooting == value) return;
        isShooting = value;
        animator?.SetBool(hashIsShooting, value);
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ RETURN TO IDLE (FINAL PHASE END) ✦

    /// <summary>
    /// Cho quái phụ dừng combat và chạy về vị trí ban đầu (idle).
    /// </summary>
    public IEnumerator ReturnToIdlePoint()
    {
        // Dừng mọi hành động
        isOrbiting = false;
        StopAllCoroutines();

        SetShooting(false);
        SetRunning(true);

        while (Vector3.Distance(transform.position, idlePosition) > 0.2f)
        {
            Vector3 dir = (idlePosition - transform.position).normalized;
            transform.position += dir * moveSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 10f
            );
            yield return null;
        }

        // Đứng yên đúng rotation cũ
        transform.rotation = idleRotation;
        SetRunning(false);
    }

    #endregion
}
