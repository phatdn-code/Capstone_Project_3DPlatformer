using PLAYERTWO.PlatformerProject;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 💥 Xử lý logic cho viên đạn:
/// - Di chuyển theo hướng chỉ định.
/// - Tự tắt sau lifeTime (hoặc khi va chạm).
/// - Khi va chạm: spawn hitEffect từ PoolManager.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class BulletProjectile : MonoBehaviour
{
    //─────────────────────────────────────────────
    #region ✦ THÔNG SỐ CÀI ĐẶT ✦

    [Header("Cài đặt viên đạn")]
    [SerializeField] private float bulletSpeed = 25f;      // tốc độ bay
    [SerializeField] private float lifeTime = 5f;          // thời gian tồn tại
    [SerializeField] private int damage = 1;               // sát thương gây ra

    [Header("Hiệu ứng va chạm")]
    [SerializeField] private GameObject hitEffectPrefab;   // hiệu ứng khi trúng
    [SerializeField] private LayerMask hitLayers;          // lớp có thể trúng

    #endregion
    //─────────────────────────────────────────────
    #region ✦ BIẾN NỘI BỘ ✦

    private Rigidbody rb;
    private Tween deactivateTween;
    private bool hasHit = false;

    #endregion
    //─────────────────────────────────────────────
    #region ✦ VÒNG ĐỜI UNITY ✦

    private void Start()
    {
        // Khởi tạo rigidbody
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
    }

    private void OnEnable()
    {
        // Đảm bảo luôn tắt trọng lực & reset trạng thái
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        hasHit = false;

        // Hủy tween cũ (nếu còn)
        deactivateTween?.Kill();

        // ⏱ Sau lifeTime giây → tự tắt
        deactivateTween = DOVirtual.DelayedCall(lifeTime, DeactivateSelf)
            .SetLink(gameObject); // tween tự hủy khi object bị disable
    }

    private void OnDisable()
    {
        // Ngăn tween còn chạy khi object đã tắt
        deactivateTween?.Kill();
        if (rb != null)
            rb.linearVelocity = Vector3.zero;
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ KÍCH HOẠT BẮN ✦

    /// <summary>
    /// 🚀 Kích hoạt viên đạn theo hướng chỉ định.
    /// </summary>
    public void Fire(Vector3 direction)
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;

        // Giữ hướng bay ngang, không chúc xuống
        direction.y = 0f;
        rb.linearVelocity = direction.normalized * bulletSpeed;
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ XỬ LÝ VA CHẠM ✦

    /// <summary>
    /// 💢 Khi đạn chạm vào collider hợp lệ.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (hasHit) return;

        // Bỏ qua nếu collider không thuộc layer mục tiêu
        if (((1 << other.gameObject.layer) & hitLayers) == 0)
            return;

        hasHit = true;

        // 💥 Tạo hiệu ứng trúng (hit effect)
        if (hitEffectPrefab)
        {
            Vector3 hitPos = transform.position;
            Quaternion hitRot = Quaternion.LookRotation(-rb.linearVelocity.normalized);
            PoolManager.Instance.ReuseComponent(hitEffectPrefab, hitPos, hitRot);
        }

        // 🩸 Nếu trúng player → gây damage
        if (other.CompareTag(GameTags.Player) && other.TryGetComponent<Player>(out var player))
            player.ApplyDamage(damage, transform.position);

        // ⏸️ Tắt viên đạn (trả về pool)
        DeactivateSelf();
    }

    #endregion
    //─────────────────────────────────────────────
    #region ✦ TẮT VIÊN ĐẠN ✦

    /// <summary>
    /// 🔚 Tắt viên đạn (dùng cho pooling).
    /// </summary>
    private void DeactivateSelf()
    {
        gameObject.SetActive(false);
    }

    #endregion
}
