using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class BossTelegraphIndicator : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private Color color = new Color(1f, 0f, 0f, 1f);
    [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.75f;

    [Header("Pulse")]
    [SerializeField] private bool pulse = true;
    [SerializeField] private float pulseSpeed = 6f;
    [SerializeField] private float pulseAmount = 0.25f;

    [Header("Ground Offset")]
    [SerializeField] private float yOffset = 0.03f; // tăng nếu bị nhấp nháy

    private Renderer rend;
    private MaterialPropertyBlock mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP Unlit

    private void Start()
    {
        rend = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    private void LateUpdate()
    {
        float a = baseAlpha;

        if (pulse)
        {
            float s = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f; // 0..1
            a *= Mathf.Lerp(1f - pulseAmount, 1f + pulseAmount, s);
        }

        ApplyColor(new Color(color.r, color.g, color.b, a));
    }

    /// <summary>Set quad size in meters (width = X, length = Y).</summary>
    public void SetSize(float width, float length)
    {
        transform.localScale = new Vector3(width, length, 1f);
    }

    public void SetColor(Color c) => color = c;
    public void SetBaseAlpha(float a) => baseAlpha = Mathf.Clamp01(a);

    /// <summary>Place on ground with a small offset to avoid z-fighting.</summary>
    public void SetWorldPose(Vector3 groundPos, Quaternion rot)
    {
        groundPos.y += yOffset;
        transform.SetPositionAndRotation(groundPos, rot);
    }

    private void ApplyColor(Color c)
    {
        rend.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        rend.SetPropertyBlock(mpb);
    }
}
