using DG.Tweening;
using PLAYERTWO.PlatformerProject;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortalZoneManager : SingletonMonobehaviour<PortalZoneManager>
{
    [Header("All Portal Zones")]
    public List<PortalZone> zones = new List<PortalZone>();

    [Header("Current Zone (runtime)")]
    private PortalZone currentZone;

    [Header("Return Portal Settings")]
    public Portal returnPortal;

    [Header("Transition Settings")]
    [SerializeField] private float dissolveMoveDuration = 2f;



    //─────────────────────────────────────────────────────────────
    #region === UNITY LIFECYCLE ===

    private IEnumerator Start()
    {
        // Đợi 1 frame để mọi object trong scene kịp Awake/Start
        yield return new WaitForEndOfFrame();

        InitReturnZone();
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === INITIALIZATION ===

    /// <summary>
    /// Khi game bắt đầu → kích hoạt zone chứa returnPortal.
    /// (Dùng chung logic qua TryActivateZoneByPortal)
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
    /// Kích hoạt zone dựa trên portal player vừa đi qua.
    /// (Dùng chung logic qua TryActivateZoneByPortal)
    /// </summary>
    public void ActivateZoneByPortal(Portal exitPortal)
    {
        TryActivateZoneByPortal(exitPortal);
    }

    /// <summary>
    /// Chạy dissolve-plane transition.
    /// </summary>
    public void RunZoneTransition()
    {
        StartCoroutine(PlayZoneTransition());
    }

    /// <summary>
    /// Nếu portal bằng returnPortal → chạy cutscene shuffle.
    /// </summary>
    public void TryRunReturnPortalCutscene(Portal exitPortal)
    {
        if (exitPortal != returnPortal) return;

        CutscenePortalShuffle.Instance.StartCutsceneFlow();
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === PRIVATE HELPERS ===

    /// <summary>
    /// Hàm chung: nhận portal → tìm zone → nếu thấy thì ActivateZone().
    /// Dùng cho InitReturnZone() và ActivateZoneByPortal().
    /// </summary>
    private void TryActivateZoneByPortal(Portal portal)
    {
        if (portal == null) return;

        PortalZone targetZone = FindZoneByPortal(portal);

        if (targetZone == null)
        {
            Debug.LogWarning($"PortalZoneManager: No zone found for portal {portal.name}");
            return;
        }

        ActivateZone(targetZone);
    }


    /// <summary>Tìm zone chứa portal.</summary>
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
    /// Kích hoạt đúng 1 zone, reset portal về vị trí gốc, tắt portal và tắt các zone khác.
    /// </summary>
    private void ActivateZone(PortalZone targetZone)
    {
        currentZone = targetZone;

        foreach (var zone in zones)
        {
            if (zone == null) continue;

            bool isTarget = (zone == targetZone);

            // Reset portal về initialPosition (ngoại trừ returnPortal)
            if (zone.portal != null)
            {
                if (zone.portal != returnPortal)
                {
                    zone.portal.transform.position = zone.initialPosition;
                    zone.portal.transform.rotation = zone.initialRotation;
                }

                // Luôn tắt portal trước transition
                zone.portal.gameObject.SetActive(false);
            }

            // Chỉ bật zone cần thiết
            zone.gameObject.SetActive(isTarget);
        }
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === TRANSITION SEQUENCE ===

    private IEnumerator PlayZoneTransition()
    {
        if (currentZone == null)
        {
            Debug.LogWarning("PortalZoneManager: No current zone to run transition.");
            yield break;
        }

        // Khóa player
        PlayerHub.Instance.LockPlayer(true);

        // Đưa portal về portalTargetPoint (khác với initialPosition)
        if (currentZone.portal != null && currentZone.portalTargetPoint != null)
        {
            currentZone.portal.transform.position = currentZone.portalTargetPoint.position;
            currentZone.portal.transform.rotation = currentZone.portalTargetPoint.rotation;
        }

        // Ưu tiên camera zone
        var cam = currentZone.portalCamera;
        int oldPriority = cam.Priority;
        cam.Priority = 100;

        // Chuẩn bị dissolve-plane
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

        // Sau dissolve → bật portal
        if (currentZone.portal != null)
            currentZone.portal.gameObject.SetActive(true);

        // Trả lại priority camera
        cam.Priority = oldPriority;

        // Mở khóa player
        PlayerHub.Instance.LockPlayer(false);
    }

    #endregion
    //─────────────────────────────────────────────────────────────
}
