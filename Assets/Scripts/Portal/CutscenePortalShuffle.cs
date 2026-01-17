using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using PLAYERTWO.PlatformerProject;

/// <summary>
/// Handles portal shuffle cutscene logic (highlight → shuffle → drop).
/// </summary>
public class CutscenePortalShuffle : SingletonMonobehaviour<CutscenePortalShuffle>
{
    //─────────────────────────────────────────────────────────────
    #region === INSPECTOR REFERENCES ===

    [Header("Portal References")]
    [SerializeField] private Transform portalGroup;                 // Parent of all portals
    [SerializeField] private List<PortalAuxiliary> portals;         // All portals
    [SerializeField] private PortalAuxiliary correctPortal;         // Correct portal (highlighted)

    [Header("Dissolve Plane")]
    [SerializeField] private Transform dissolvePlane;
    [SerializeField] private float dissolvePlaneDuration = 5f;

    [Header("Shuffle Settings")]
    [SerializeField] private int shuffleCount = 5;
    [SerializeField] private float shuffleDuration = 1.2f;

    [Header("Portal Drop")]
    [SerializeField] private float moveDuration = 1.0f;

    [Header("Boss Reference")]
    [SerializeField] private BossHealth bossHealth;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === RUNTIME STATE ===

    private CameraCutsceneController camController;

    private float originalPortalY;                  // Original Y of portal group
    private List<Vector3> originalPositions;        // Cached portal positions

    private bool hasTriggeredHighlight;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === CONSTANTS ===

    private const float PLANE_START_Y = 24f;
    private const float PLANE_END_Y = -2f;
    private const float PLANE_TRIGGER_Y = 15f;

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === UNITY LIFECYCLE ===

    /// <summary>
    /// Cache references and initial values.
    /// </summary>
    private void Start()
    {
        camController = CameraCutsceneController.Instance;

        if (portalGroup != null)
            originalPortalY = portalGroup.position.y;
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === CUTSCENE FLOW ===

    /// <summary>
    /// Public entry point to play the cutscene.
    /// </summary>
    public void StartCutsceneFlow()
    {
        ResetAll();
        StartCoroutine(CutsceneFlow());
    }

    /// <summary>
    /// Main cutscene sequence.
    /// </summary>
    private IEnumerator CutsceneFlow()
    {
        PlayerHub.Instance.LockPlayer(true);
        yield return camController.FocusTo(BossCamType.Special);

        yield return LowerDissolvePlane();
        correctPortal.HideHighlight();

        yield return ShufflePortals();
        yield return MovePortalGroupToPlayerHeight();

        foreach (var p in portals)
            p.ShowEnergy();

        yield return camController.ReleaseToPlayer();
        PlayerHub.Instance.LockPlayer(false);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === DISSOLVE PLANE ===

    /// <summary>
    /// Drops the dissolve plane and triggers portal highlight.
    /// </summary>
    private IEnumerator LowerDissolvePlane()
    {
        hasTriggeredHighlight = false;

        dissolvePlane.position = new Vector3(
            dissolvePlane.position.x,
            PLANE_START_Y,
            dissolvePlane.position.z
        );

        Tween tween = dissolvePlane.DOMoveY(PLANE_END_Y, dissolvePlaneDuration)
            .SetEase(Ease.Linear)
            .OnUpdate(() =>
            {
                if (!hasTriggeredHighlight && dissolvePlane.position.y <= PLANE_TRIGGER_Y)
                {
                    hasTriggeredHighlight = true;
                    correctPortal.ShowHighlight();
                }
            });

        yield return tween.WaitForCompletion();
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === PORTAL DROP ===

    /// <summary>
    /// Moves portal group down to player height.
    /// </summary>
    private IEnumerator MovePortalGroupToPlayerHeight()
    {
        float playerY = PlayerHub.Instance.transform.position.y;

        Vector3 targetPos = new Vector3(
            portalGroup.position.x,
            playerY + 2f,
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
    /// Cache initial portal positions.
    /// </summary>
    private void CacheOriginalPositions()
    {
        originalPositions = new List<Vector3>(portals.Count);

        foreach (var p in portals)
            originalPositions.Add(p.transform.position);
    }

    /// <summary>
    /// Shuffle portals using curved swap paths.
    /// </summary>
    private IEnumerator ShufflePortals()
    {
        CacheOriginalPositions();

        float duration = shuffleDuration;

        // Faster shuffle in Phase 2+
        if (bossHealth != null && bossHealth.currentPhase >= 1)
            duration *= 0.75f;

        for (int i = 0; i < shuffleCount; i++)
        {
            int a = Random.Range(0, portals.Count);
            int b = Random.Range(0, portals.Count);
            while (b == a) b = Random.Range(0, portals.Count);

            SwapPortals(portals[a], portals[b], duration);
            yield return new WaitForSeconds(duration);

            (portals[a], portals[b]) = (portals[b], portals[a]);
        }
    }

    /// <summary>
    /// Animate a curved swap between two portals.
    /// </summary>
    private void SwapPortals(PortalAuxiliary a, PortalAuxiliary b, float duration)
    {
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        Vector3 dir = (posB - posA).normalized;
        Vector3 perp = Vector3.Cross(dir, Vector3.forward).normalized;

        Vector3 center = (posA + posB) * 0.5f;

        Vector3 midUp = center + perp * 2.5f + Vector3.up * 1.8f;
        Vector3 midDown = center - perp * 2.5f + Vector3.down * 1.8f;

        a.transform.DOPath(new[] { posA, midUp, posB }, duration, PathType.CatmullRom)
                   .SetEase(Ease.InOutQuad);

        b.transform.DOPath(new[] { posB, midDown, posA }, duration, PathType.CatmullRom)
                   .SetEase(Ease.InOutQuad);
    }

    #endregion
    //─────────────────────────────────────────────────────────────


    //─────────────────────────────────────────────────────────────
    #region === RESET ===

    /// <summary>
    /// Reset portals and cutscene state.
    /// </summary>
    public void ResetAll()
    {
        if (portalGroup != null)
        {
            Vector3 pos = portalGroup.position;
            portalGroup.position = new Vector3(pos.x, originalPortalY, pos.z);
        }

        if (dissolvePlane != null)
        {
            Vector3 pos = dissolvePlane.position;
            dissolvePlane.position = new Vector3(pos.x, PLANE_START_Y, pos.z);
        }

        correctPortal?.HideHighlight();

        foreach (var p in portals)
            p.HideEnergy();

        if (originalPositions != null && originalPositions.Count == portals.Count)
        {
            for (int i = 0; i < portals.Count; i++)
                portals[i].transform.position = originalPositions[i];
        }

        hasTriggeredHighlight = false;
    }

    #endregion
    //─────────────────────────────────────────────────────────────
}
