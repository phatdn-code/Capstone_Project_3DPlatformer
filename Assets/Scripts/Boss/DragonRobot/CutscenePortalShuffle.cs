using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PLAYERTWO.PlatformerProject;

public class CutscenePortalShuffle : SingletonMonobehaviour<CutscenePortalShuffle>
{
    //─────────────────────────────────────────────────────────────
    #region === Inspector Fields ===

    [Header("Portal Group Settings")]
    [SerializeField] private Transform portalGroup;                   // Parent của toàn bộ portal
    [SerializeField] private List<PortalAuxiliary> portals;           // Danh sách portal
    [SerializeField] private PortalAuxiliary correctPortal;           // Portal đúng (dùng để highlight)

    [Header("Dissolve Plane Settings")]
    [SerializeField] private Transform dissolvePlane;                 // Plane dùng để cắt/dissolve
    [SerializeField] private float dissolvePlaneDuration = 5f;        // Thời gian plane rơi từ 24 → -2

    [Header("Shuffle Settings")]
    [SerializeField] private int shuffleCount = 5;                    // Số lần tráo portal
    [SerializeField] private float shuffleDuration = 1.2f;            // Thời gian mỗi cú tráo

    [Header("Portal Drop Settings")]
    [SerializeField] private float moveDuration = 1.0f;               // Thời gian hạ portalGroup xuống ngang player

    #endregion
    //─────────────────────────────────────────────────────────────

    //─────────────────────────────────────────────────────────────
    #region === Private Fields ===

    private CameraCutsceneController camController;

    private float originalPortalY;                                    // Y gốc của portalGroup
    private List<Vector3> originalPositions;                          // Lưu vị trí portal trước shuffle

    private const float PLANE_START_Y = 24f;
    private const float PLANE_END_Y = -2f;
    private const float TRIGGER_Y = 15f;

    private bool hasTriggeredHighlight = false;

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === Unity Lifecycle ===

    private void Start()
    {
        // Lấy camera controller
        camController = CameraCutsceneController.Instance;

        // Lưu vị trí Y của portalGroup
        if (portalGroup != null)
            originalPortalY = portalGroup.position.y;
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === CUTSCENE FLOW ===

    /// <summary>
    /// Public API để chạy CutsceneFlow từ bên ngoài.
    /// </summary>
    public void StartCutsceneFlow()
    {
        ResetAll();
        StartCoroutine(CutsceneFlow());
    }

    /// <summary>
    /// Chuỗi cutscene chính: khoá player → cam special → dissolve → highlight → shuffle → drop → energy → trả cam → mở player.
    /// </summary>
    private IEnumerator CutsceneFlow()
    {
        PlayerHub.Instance.LockPlayer(true);                                          // Khoá điều khiển
        yield return camController.FocusTo(BossCamType.Special);                      // Chuyển camera

        yield return LowerDissolvePlane();                                            // Rơi plane + trigger highlight
        correctPortal.HideHighlight();                                                // Tắt highlight trước shuffle

        yield return ShufflePortals();                                                // Shuffle

        yield return MovePortalGroupToPlayerHeight();                                 // Hạ group xuống

        foreach (var p in portals)                                                    // Bật hiệu ứng năng lượng
            p.ShowEnergy();

        yield return camController.ReleaseToPlayer();                                 // Trả camera lại

        PlayerHub.Instance.LockPlayer(false);                                         // Mở điều khiển
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === DISSOLVE PLANE LOGIC ===

    /// <summary>
    /// Hạ plane từ 24 xuống -2, và khi plane chạm Y=15 thì bắt đầu highlight chính xác portal đúng.
    /// </summary>
    private IEnumerator LowerDissolvePlane()
    {
        hasTriggeredHighlight = false;

        dissolvePlane.position = new Vector3(
            dissolvePlane.position.x,
            PLANE_START_Y,
            dissolvePlane.position.z
        );

        Tween t = dissolvePlane.DOMoveY(PLANE_END_Y, dissolvePlaneDuration)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                if (!hasTriggeredHighlight && dissolvePlane.position.y <= TRIGGER_Y)
                {
                    hasTriggeredHighlight = true;
                    correctPortal.ShowHighlight();
                }
            });

        yield return t.WaitForCompletion();
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === PORTAL DROP LOGIC ===

    /// <summary>
    /// Hạ toàn bộ portalGroup xuống độ cao của player.
    /// </summary>
    private IEnumerator MovePortalGroupToPlayerHeight()
    {
        float playerY = PlayerHub.Instance.transform.position.y;

        Vector3 targetPos = new Vector3(
            portalGroup.position.x,
            playerY + 2,
            portalGroup.position.z
        );

        portalGroup.DOMove(targetPos, moveDuration)
                   .SetEase(Ease.InOutQuad);

        yield return new WaitForSeconds(moveDuration);
    }

    #endregion
    //─────────────────────────────────────────────────────────────



    //─────────────────────────────────────────────────────────────
    #region === SHUFFLE LOGIC ===

    /// <summary>
    /// Lưu vị trí portal lúc ban đầu.
    /// </summary>
    private void CacheOriginalPositions()
    {
        originalPositions = new List<Vector3>(portals.Count);

        foreach (var p in portals)
            originalPositions.Add(p.transform.position);
    }

    /// <summary>
    /// Shuffle portal theo đường cong (1 portal đi lên, 1 portal đi xuống).
    /// </summary>
    private IEnumerator ShufflePortals()
    {
        CacheOriginalPositions();

        for (int i = 0; i < shuffleCount; i++)
        {
            int a = Random.Range(0, portals.Count);
            int b = Random.Range(0, portals.Count);
            while (b == a) b = Random.Range(0, portals.Count);

            PortalAuxiliary portalA = portals[a];
            PortalAuxiliary portalB = portals[b];

            Vector3 posA = portalA.transform.position;
            Vector3 posB = portalB.transform.position;

            Vector3 dir = (posB - posA).normalized;
            Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;

            float arcWidth = 2.5f;
            float arcHeight = 1.8f;

            Vector3 center = (posA + posB) * 0.5f;
            Vector3 midUp = center + perp * arcWidth + Vector3.up * arcHeight;
            Vector3 midDown = center - perp * arcWidth + Vector3.down * arcHeight;

            Vector3[] pathA = { posA, midUp, posB };
            Vector3[] pathB = { posB, midDown, posA };

            Tween tA = portalA.transform.DOPath(pathA, shuffleDuration, PathType.CatmullRom)
                                        .SetEase(Ease.InOutQuad);

            Tween tB = portalB.transform.DOPath(pathB, shuffleDuration, PathType.CatmullRom)
                                        .SetEase(Ease.InOutQuad);

            yield return tA.WaitForCompletion();

            // Swap vị trí trong list
            portals[a] = portalB;
            portals[b] = portalA;
        }
    }

    #endregion
    //─────────────────────────────────────────────────────────────

    #region === RESET LOGIC ===

    /// <summary>
    /// Reset toàn bộ hệ thống portal về trạng thái ban đầu để chạy lại cutscene.
    /// </summary>
    public void ResetAll()
    {
        // 1) Reset Y của PortalGroup
        if (portalGroup != null)
        {
            Vector3 pos = portalGroup.position;
            portalGroup.position = new Vector3(pos.x, originalPortalY, pos.z);
        }

        // 2) Reset dissolve plane về Y = PLANE_START_Y
        if (dissolvePlane != null)
        {
            Vector3 dp = dissolvePlane.position;
            dissolvePlane.position = new Vector3(dp.x, PLANE_START_Y, dp.z);
        }

        // 3) Reset highlight
        if (correctPortal != null)
            correctPortal.HideHighlight();

        // 4) Tắt energy tất cả portal
        foreach (var p in portals)
            p.HideEnergy();

        // 5) Reset vị trí từng portal
        if (originalPositions != null && originalPositions.Count == portals.Count)
        {
            for (int i = 0; i < portals.Count; i++)
                portals[i].transform.position = originalPositions[i];
        }

        // 6) Reset lại trigger để highlight lần sau
        hasTriggeredHighlight = false;

        // 7) Kill tất cả tween để tránh tween bị giữ lại
        DOTween.KillAll();
    }

    #endregion
    //─────────────────────────────────────────────────────────────
}
