using UnityEngine;
using DG.Tweening;

public class BossEnergyPumpEffect : MonoBehaviour
{
    [Header("Morph Pulse Settings")]
    [SerializeField] private float squishAmount = 0.15f;      // 🔹 Giảm nén ngang
    [SerializeField] private float stretchAmount = 0.25f;     // 🔹 Giảm phồng dọc
    [SerializeField] private float pulseDuration = 0.45f;     // 🔹 Nhanh, nhịp vừa phải
    [SerializeField] private Ease pulseEase = Ease.InOutSine; // 🔹 Nhịp mượt đều

    [Header("Continuous Pulse Control")]
    [SerializeField] private bool continuousPulse = false;    // 🔁 Lặp liên tục
    [SerializeField] private float pauseBetweenPulses = 0.03f; // 🔹 Nghỉ cực ngắn giữa nhịp

    private Vector3 baseScale;
    private Sequence pulseSequence;

    private void Start()
    {
        baseScale = transform.localScale;
    }

    //─────────────────────────────────────────────
    // Hiệu ứng "bơm năng lượng" – dao động nhẹ, liên tục
    //─────────────────────────────────────────────
    public void PlayMorph()
    {
        StopActiveTween();

        if (continuousPulse)
        {
            pulseSequence = DOTween.Sequence()
                // Co nhẹ
                .Append(transform.DOScale(
                    new Vector3(baseScale.x * (1 + squishAmount),
                                baseScale.y * (1 - squishAmount),
                                baseScale.z * (1 + squishAmount)),
                    pulseDuration * 0.5f
                ).SetEase(pulseEase))
                // Phồng nhẹ
                .Append(transform.DOScale(
                    new Vector3(baseScale.x * (1 - 0.05f),
                                baseScale.y * (1 + stretchAmount),
                                baseScale.z * (1 - 0.05f)),
                    pulseDuration * 0.5f
                ).SetEase(pulseEase))
                // 🔁 Lặp mãi với nhịp cực mượt
                .SetLoops(-1, LoopType.Yoyo)
                .SetDelay(pauseBetweenPulses)
                .SetUpdate(true);
        }

        else
        {
            // Một nhịp duy nhất (nếu không bật continuous)
            pulseSequence = DOTween.Sequence()
                .Append(transform.DOScale(
                    new Vector3(baseScale.x * (1 + squishAmount),
                                baseScale.y * (1 - squishAmount),
                                baseScale.z * (1 + squishAmount)),
                    pulseDuration * 0.4f
                ).SetEase(Ease.InCubic))
                .Append(transform.DOScale(
                    new Vector3(baseScale.x * (1 - 0.05f),
                                baseScale.y * (1 + stretchAmount),
                                baseScale.z * (1 - 0.05f)),
                    pulseDuration * 0.6f
                ).SetEase(Ease.OutElastic))
                .OnComplete(() =>
                {
                    transform.DOScale(baseScale, 0.25f).SetEase(Ease.OutBack);
                });
        }

        pulseSequence.Play();
    }

    //─────────────────────────────────────────────
    // Cập nhật sóng năng lượng (liên quan tới phần hồi máu)
    //─────────────────────────────────────────────
    public void UpdateMorphProgress(float t)
    {
        float wave = Mathf.Sin(t * Mathf.PI * 4f);
        float yStretch = 1 + wave * stretchAmount * 0.4f;
        float xzSquish = 1 - wave * squishAmount * 0.3f;

        transform.localScale = new Vector3(
            baseScale.x * xzSquish,
            baseScale.y * yStretch,
            baseScale.z * xzSquish
        );
    }

    //─────────────────────────────────────────────
    // Trở về trạng thái ban đầu
    //─────────────────────────────────────────────
    public void RevertMorph()
    {
        StopActiveTween();
        transform.DOScale(baseScale, 0.25f).SetEase(Ease.OutBack);
    }

    private void StopActiveTween()
    {
        if (pulseSequence != null && pulseSequence.IsActive())
            pulseSequence.Kill();
    }
}
