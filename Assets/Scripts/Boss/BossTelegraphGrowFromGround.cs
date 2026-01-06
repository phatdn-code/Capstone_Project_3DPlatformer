using System.Collections;
using UnityEngine;

public class BossTelegraphGrowFromGround : MonoBehaviour
{
    [Header("Indicator (Scene Object)")]
    [SerializeField] private BossTelegraphIndicator indicator;

    [Header("Start Point (on ground)")]
    [SerializeField] private Transform startPoint;

    [Header("Size (meters)")]
    [SerializeField] private float targetLength = 10f;
    [SerializeField] private float width = 3f;

    [Header("Timing")]
    [SerializeField] private float growTime = 0.25f;
    [SerializeField] private bool holdUntilStop = true;

    [Header("Ground")]
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private float raycastHeight = 6f;

    [Header("Fade In While Growing")]
    [SerializeField] private bool fadeIn = true;
    [SerializeField, Range(0f, 1f)] private float minAlphaWhenStart = 0.2f;

    private Coroutine routine;

    // Cache rotation gốc của indicator (giữ y nguyên)
    private Quaternion initialRotation;

    private void Awake()
    {
        if (indicator == null)
        {
            Debug.LogError($"{nameof(BossTelegraphGrowFromGround)}: indicator is NULL.");
            return;
        }

        initialRotation = indicator.transform.rotation;
        indicator.gameObject.SetActive(false);
    }

    public void PlayTelegraph()
    {
        if (indicator == null) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoGrow());
    }

    public void StopTelegraph()
    {
        if (routine != null) StopCoroutine(routine);
        routine = null;

        if (indicator != null)
            indicator.gameObject.SetActive(false);
    }

    private IEnumerator CoGrow()
    {
        // đảm bảo rotation đúng ban đầu ngay khi bật
        indicator.transform.rotation = initialRotation;

        UpdatePoseGrow(0.001f); // set vị trí trước, tránh pop
        indicator.gameObject.SetActive(true);

        float t = 0f;
        while (t < growTime)
        {
            float k = Mathf.Clamp01(t / growTime);
            k = EaseOutCubic(k);

            float currentLen = Mathf.Lerp(0f, targetLength, k);

            if (fadeIn)
            {
                float a = Mathf.Lerp(minAlphaWhenStart, 1f, k);
                indicator.SetBaseAlpha(a * 0.75f);
            }

            UpdatePoseGrow(currentLen);

            t += Time.deltaTime;
            yield return null;
        }

        UpdatePoseGrow(targetLength);

        if (!holdUntilStop)
        {
            indicator.gameObject.SetActive(false);
            routine = null;
            yield break;
        }

        while (true)
        {
            UpdatePoseGrow(targetLength);
            yield return null;
        }
    }

    private void UpdatePoseGrow(float currentLen)
    {
        if (startPoint == null) return;

        // chỉ tính vị trí, KHÔNG động vào rotation
        Vector3 fwd = startPoint.forward;
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.0001f) fwd = transform.forward;
        fwd.Normalize();

        Vector3 startGround = ProjectToGround(startPoint.position);

        Vector3 centerProbe = startGround + fwd * (currentLen * 0.5f);
        Vector3 centerGround = ProjectToGround(centerProbe);

        indicator.SetSize(width, Mathf.Max(0.001f, currentLen));

        // set position thôi, giữ nguyên rotation ban đầu
        indicator.transform.position = centerGround + Vector3.up * 0.03f; // hoặc dùng yOffset của indicator nếu muốn
        indicator.transform.rotation = initialRotation;
    }

    private Vector3 ProjectToGround(Vector3 pos)
    {
        Vector3 start = pos + Vector3.up * raycastHeight;
        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, raycastHeight * 3f, groundMask))
            return hit.point;

        return pos;
    }

    private static float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        return 1f - Mathf.Pow(1f - x, 3f);
    }
}
