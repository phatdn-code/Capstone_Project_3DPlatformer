using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.Events;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMP_Blink : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField] private float totalDuration = 1f;
    [SerializeField] private int blinkCount = 5;

    public UnityEvent OnBlinkFinished;

    private TextMeshProUGUI textMesh;
    private Tween tween;

    private void Awake() => textMesh = GetComponent<TextMeshProUGUI>();

    public void PlayBlink()
    {
        if (tween != null && tween.IsPlaying()) return;

        textMesh.alpha = 1f;

        tween = textMesh
            .DOFade(0f, totalDuration / (blinkCount * 2))
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                textMesh.alpha = 1f;
                OnBlinkFinished?.Invoke();
            });
    }
}