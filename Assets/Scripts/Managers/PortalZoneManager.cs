using DG.Tweening;
using PLAYERTWO.PlatformerProject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalZoneManager : SingletonMonobehaviour<PortalZoneManager>
{
    //─────────────────────────────────────────────────────────────
    #region === INSPECTOR FIELDS ===

    [Header("Danh sách tất cả Portal Zone")]
    [SerializeField] private List<PortalZone> zones = new();

    [Header("Cài đặt Return Portal")]
    [SerializeField] private Portal returnPortal;
    [SerializeField] private Portal correctPortal;

    [Header("Cài đặt Transition")]
    [SerializeField] private float dissolveMoveDuration = 2f;

    [Header("Tham chiếu Boss")]
    [SerializeField] private BossCore boss;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === RUNTIME STATE ===

    private PortalZone currentZone;
    private DragonRobot dragonRobot;
    private Portal currentPortal;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === UNITY LIFECYCLE ===

    /// <summary>
    /// Chờ 1 frame để tất cả object Awake/Start xong,
    /// sau đó khởi tạo zone mặc định (zone có returnPortal).
    /// </summary>
    private IEnumerator Start()
    {
        dragonRobot = boss as DragonRobot;

        yield return new WaitForEndOfFrame();
        InitReturnZone();
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === INITIALIZATION ===

    /// <summary>
    /// Bật zone chứa returnPortal khi bắt đầu game.
    /// </summary>
    private void InitReturnZone()
    {
        TryActivateZoneByPortal(returnPortal);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === PUBLIC API ===

    /// <summary>
    /// Lấy zone hiện tại đang active (zone mà game đang sử dụng).
    /// </summary>
    public PortalZone CurrentZone => currentZone;

    /// <summary>
    /// Portal mà player vừa đi qua để kích hoạt zone hiện tại.
    /// </summary>
    public Portal CurrentPortal => currentPortal;

    /// <summary>
    /// Portal được đánh dấu là "đúng".
    /// </summary>
    public Portal CorrectPortal => correctPortal;

    /// <summary>
    /// True nếu portal hiện tại (portal vừa kích hoạt zone) là correctPortal.
    /// </summary>
    public bool IsCurrentZoneCorrectPortal()
    {
        return currentPortal != null && correctPortal != null && currentPortal == correctPortal;
    }

    /// <summary>
    /// Player đi qua portal → kích hoạt zone tương ứng.
    /// Dùng cho các Portal bình thường.
    /// </summary>
    public void ActivateZoneByPortal(Portal exitPortal)
    {
        TryActivateZoneByPortal(exitPortal);
    }

    /// <summary>
    /// Chạy hiệu ứng dissolve-plane + camera + lock/unlock player
    /// cho zone hiện tại.
    /// </summary>
    public void RunZoneTransition()
    {
        StartCoroutine(PlayZoneTransition());
    }

    /// <summary>
    /// Nếu exitPortal chính là returnPortal → chạy cutscene shuffle đặc biệt.
    /// </summary>
    public void TryRunReturnPortalCutscene(Portal exitPortal)
    {
        if (exitPortal == returnPortal)
            CutscenePortalShuffle.Instance.StartCutsceneFlow();
    }

    /// <summary>
    /// Trả về điểm mà Dragon nên quay mặt về trong currentZone.
    /// Nếu chưa có zone → fallback về Dragon hoặc PortalZoneManager.
    /// </summary>
    public Vector3 GetCurrentZoneFacingPoint()
    {
        // Nếu chưa có zone hiện tại
        if (currentZone == null)
        {
            // Nếu có Dragon thì cho nhìn về chính Dragon
            if (dragonRobot != null)
                return dragonRobot.transform.position;

            // Fallback cuối cùng: vị trí của PortalZoneManager
            return transform.position;
        }

        // Ưu tiên điểm target riêng cho portal/boss
        if (currentZone.portalTargetPoint != null)
            return currentZone.portalTargetPoint.position;

        // Nếu không có target point thì dùng vị trí portal
        if (currentZone.portal != null)
            return currentZone.portal.transform.position;

        // Cuối cùng: dùng vị trí zone
        return currentZone.transform.position;
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === PRIVATE ZONE LOGIC ===

    /// <summary>
    /// Tìm zone chứa portal truyền vào và kích hoạt zone đó.
    /// </summary>
    private void TryActivateZoneByPortal(Portal portal)
    {
        if (portal == null) return;

        currentPortal = portal;

        PortalZone targetZone = FindZoneByPortal(portal);

        if (targetZone == null)
        {
            Debug.LogWarning($"PortalZoneManager: No zone found for portal {portal.name}");
            return;
        }

        ActivateZone(targetZone);
    }

    /// <summary>
    /// Duyệt danh sách zones để tìm zone có portal khớp với tham số.
    /// </summary>
    private PortalZone FindZoneByPortal(Portal portal)
    {
        foreach (var zone in zones)
        {
            if (zone != null && zone.portal == portal)
                return zone;
        }

        return null;
    }

    /// <summary>
    /// Kích hoạt 1 zone duy nhất:
    /// - Cập nhật currentZone
    /// - Reset vị trí/rotation portal về initial (trừ returnPortal)
    /// - Tắt tất cả portal, chỉ bật zone target
    /// - Gửi boss (Dragon) tới bossEntryPoint nếu có
    /// </summary>
    private void ActivateZone(PortalZone targetZone)
    {
        currentZone = targetZone;

        foreach (var zone in zones)
        {
            if (zone == null) continue;

            bool isTarget = zone == targetZone;

            // Reset portal về vị trí gốc (ngoại trừ returnPortal)
            if (zone.portal != null)
            {
                if (zone.portal != returnPortal)
                {
                    zone.portal.transform.position = zone.initialPosition;
                    zone.portal.transform.rotation = zone.initialRotation;
                }

                zone.portal.gameObject.SetActive(false);
            }

            zone.gameObject.SetActive(isTarget);
        }

        // Gửi boss tới entryPoint của zone mới
        TryMoveBossToEntryPoint(targetZone);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === TRANSITION SEQUENCE ===

    /// <summary>
    /// Chạy sequence transition cho zone hiện tại:
    /// - Lock player
    /// - Di chuyển dissolve-plane
    /// - Bật portal
    /// - Trả camera về trạng thái cũ
    /// - Mở lại player
    /// </summary>
    private IEnumerator PlayZoneTransition()
    {
        if (currentZone == null)
        {
            Debug.LogWarning("PortalZoneManager: No current zone to run transition.");
            yield break;
        }

        PlayerHub.Instance.LockPlayer(true);

        // Đưa portal về vị trí spawn (portalTargetPoint) nếu có
        if (currentZone.portal != null && currentZone.portalTargetPoint != null)
        {
            currentZone.portal.transform.position = currentZone.portalTargetPoint.position;
            currentZone.portal.transform.rotation = currentZone.portalTargetPoint.rotation;
        }

        // Tạm tăng priority của camera zone để active
        var cam = currentZone.portalCamera;
        int oldPriority = cam.Priority;
        cam.Priority = 100;

        // Di chuyển dissolve-plane từ startY → endY
        if (currentZone.dissolvePlane != null)
        {
            Vector3 startPos = currentZone.dissolvePlane.position;
            startPos.y = currentZone.dissolveStartY;

            currentZone.dissolvePlane.position = startPos;
            currentZone.dissolvePlane.gameObject.SetActive(true);

            Tween t = currentZone.dissolvePlane
                .DOMoveY(currentZone.dissolveEndY, dissolveMoveDuration)
                .SetEase(Ease.InOutQuad);

            yield return t.WaitForCompletion();
        }

        // Dissolve hoàn tất → bật portal
        if (currentZone.portal != null)
            currentZone.portal.gameObject.SetActive(true);

        // Trả priority camera về giá trị cũ
        cam.Priority = oldPriority;

        PlayerHub.Instance.LockPlayer(false);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === PUBLIC HELPERS ===

    /// <summary>
    /// Trả về true nếu currentZone đang dùng returnPortal.
    /// Dùng cho boss để biết zone hiện tại có phải “return zone” hay không.
    /// </summary>
    public bool IsCurrentZoneReturnZone()
    {
        if (currentZone == null || returnPortal == null) return false;
        return currentZone.portal == returnPortal;
    }

    public bool IsCurrentZoneCorrectZone()
    {
        if (currentZone == null || correctPortal == null) return false;
        return currentZone.portal == correctPortal;
    }

    /// <summary>Trả về bossEntryPoint của currentZone (có thể null).</summary>
    public Transform GetCurrentZoneBossEntryPoint()
    {
        if (currentZone == null) return null;
        return currentZone.bossEntryPoint;
    }

    /// <summary>Trả về Transform flameCastPoint của currentZone (có thể null).</summary>
    public Transform GetCurrentZoneFlameCastPoint()
    {
        if (currentZone == null) return null;
        return currentZone.flameCastPoint;
    }

    /// <summary>Trả về Transform blastCastPoint của currentZone (có thể null).</summary>
    public Transform GetCurrentZoneBlastCastPoint()
    {
        if (currentZone == null) return null;
        return currentZone.blastCastPoint;
    }

    /// <summary>Trả về mảng Transform MeteorCastPoints của currentZone (có thể null).</summary>
    public Transform[] GetCurrentZoneMeteorCastPoints()
    {
        if (currentZone == null) return null;
        return currentZone.meteorCastPoints;
    }

    /// <summary>
    /// Trả về chiều cao Meteor (meteorHeightY) của currentZone.
    /// Nếu chưa có zone thì trả về 0.
    /// </summary>
    public float GetCurrentZoneMeteorHeightY()
    {
        if (currentZone == null)
            return 0f;

        return currentZone.meteorHeightY;
    }

    /// <summary>
    /// Lấy danh sách điểm spawn cho skill mưa Meteor của currentZone.
    /// </summary>
    public Transform[] GetCurrentZoneMeteorRainPoints()
    {
        if (currentZone == null) return null;
        return currentZone.meteorRainPoints;
    }

    /// <summary>
    /// Lấy độ cao Y mà boss đứng khi dùng skill mưa Meteor trong currentZone.
    /// </summary>
    public float GetCurrentZoneMeteorRainBossHeightY()
    {
        if (currentZone == null) return 0f;
        return currentZone.meteorRainHeightY;
    }

    #endregion


    //─────────────────────────────────────────────────────────────
    #region === BOSS HANDLING ===

    /// <summary>
    /// Nếu zone có bossEntryPoint và DragonRobot tồn tại,
    /// yêu cầu Dragon di chuyển tới điểm entry đó.
    /// </summary>
    private void TryMoveBossToEntryPoint(PortalZone zone)
    {
        if (zone == null || zone.bossEntryPoint == null) return;
        if (dragonRobot == null) return;

        dragonRobot.MoveToEntryPoint(zone.bossEntryPoint);
    }

    #endregion
    //─────────────────────────────────────────────────────────────
}
