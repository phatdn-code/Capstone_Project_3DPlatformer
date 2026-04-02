using PLAYERTWO.PlatformerProject;
using System.Collections;
using UnityEngine;

public class SupportGunnerAI : MonoBehaviour
{
    [Header("Tham chiếu đối tượng")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private GameObject muzzleFlashPrefab;
    [SerializeField] private Animator animator;

    [Header("Cài đặt di chuyển")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float orbitSpeed = 5f;
    [SerializeField] private float orbitSmooth = 8f;
    [SerializeField] private float orbitHeightOffset = 0.5f;
    [SerializeField] private float orbitDistanceOffset = 2f;

    [Header("Cài đặt chiến đấu")]
    [SerializeField] private float fireRate = 0.3f;
    [SerializeField] private float burstDuration = 3f;
    [SerializeField] private float restDuration = 2.5f;

    private Vector3 orbitCenter;
    private float orbitRadius;
    private bool isActive;
    private bool isOrbiting;
    private bool isShooting;
    private bool isRunning;

    // VN: Cờ để biết sound bắn loop đang phát hay chưa.
    private bool isFireLoopPlaying;
    private bool wasPausedLastFrame;

    private Vector3 idlePosition;
    private Quaternion idleRotation;

    private readonly int hashIsRunning = Animator.StringToHash("IsRunning");
    private readonly int hashIsShooting = Animator.StringToHash("IsShooting");
    private readonly int hashIsDead = Animator.StringToHash("Death");

    private void Start()
    {
        idlePosition = transform.position;
        idleRotation = transform.rotation;
    }

    private void Update()
    {
        bool isPaused = LevelPauser.instance != null && LevelPauser.instance.paused;

        // Vừa pause -> tắt sound ngay
        if (isPaused && !wasPausedLastFrame)
            StopFireLoopSound();

        // Vừa unpause -> nếu AI vẫn đang trong trạng thái bắn thì bật sound lại
        else if (!isPaused && wasPausedLastFrame)
        {
            if (isOrbiting && isShooting)
                PlayFireLoopSound();
        }

        wasPausedLastFrame = isPaused;
    }

    private void OnDisable()
    {
        StopFireLoopSound();
    }

    private void OnDestroy()
    {
        StopFireLoopSound();
    }

    private void PlayFireLoopSound()
    {
        if (isFireLoopPlaying) return;

        AudioManager.Instance?.PlaySound(SoundCategory.VoltitanBoss, 3);
        isFireLoopPlaying = true;
    }

    private void StopFireLoopSound()
    {
        if (!isFireLoopPlaying) return;

        AudioManager.Instance?.StopSound(SoundCategory.VoltitanBoss, 3);
        isFireLoopPlaying = false;
    }

    public void ActivateGunner(Vector3 center, float radius)
    {
        if (isActive) return;

        orbitCenter = center;
        orbitRadius = radius + orbitDistanceOffset;
        isActive = true;

        StartCoroutine(EnterArenaRoutine());
    }

    private IEnumerator EnterArenaRoutine()
    {
        // VN: Reset sound phòng trường hợp lần trước bị ngắt giữa chừng.
        StopFireLoopSound();

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

    private IEnumerator FireCycleRoutine()
    {
        while (isOrbiting)
        {
            SetRunning(false);
            SetShooting(true);

            PlayFireLoopSound();

            float timer = 0f;
            while (timer < burstDuration)
            {
                FireBullet();
                timer += fireRate;
                yield return new WaitForSeconds(fireRate);
            }

            SetShooting(false);
            SetRunning(true);
            StopFireLoopSound();

            yield return new WaitForSeconds(restDuration);
        }

        // VN: Nếu thoát khỏi vòng bắn thì đảm bảo sound cũng tắt.
        StopFireLoopSound();
    }

    private void FireBullet()
    {
        if (!bulletPrefab || !firePoint) return;

        float randomOffset = Random.Range(-5f, 5f);
        Quaternion spread = Quaternion.Euler(0, randomOffset, 0);
        Vector3 dir = spread * (orbitCenter - firePoint.position).normalized;

        Component bulletComp = PoolManager.Instance.ReuseComponent(bulletPrefab, firePoint.position, Quaternion.LookRotation(dir));
        if (bulletComp != null && bulletComp.TryGetComponent(out BulletProjectile bullet))
            bullet.Fire(dir);

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

    public void PlayDeath()
    {
        // VN: Chết thì tắt sound bắn ngay.
        StopFireLoopSound();
        animator?.SetTrigger(hashIsDead);
    }

    public IEnumerator ReturnToIdlePoint()
    {
        moveSpeed = 14;
        isOrbiting = false;
        StopAllCoroutines();

        SetShooting(false);
        SetRunning(true);
        StopFireLoopSound();

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

        transform.rotation = idleRotation;
        SetRunning(false);
    }
}