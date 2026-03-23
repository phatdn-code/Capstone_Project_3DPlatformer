using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using PLAYERTWO.PlatformerProject;

[RequireComponent(typeof(TextMeshProUGUI))]
public class TMP_Blink : MonoBehaviour
{
    [Header("Blink Settings")]
    [SerializeField, Min(0.1f)] private float totalDuration = 1f;
    [SerializeField, Min(1)] private int blinkCount = 5;

    [Header("Events")]
    public UnityEvent OnBlinkFinished;

    private TextMeshProUGUI _textMesh;
    private Tween _blinkTween;

    private bool _hasPlayed;

    /// <summary>
    /// VN: Lấy sẵn component cần dùng khi object được tạo.
    /// </summary>
    private void Awake()
    {
        _textMesh = GetComponent<TextMeshProUGUI>();
    }

    /// <summary>
    /// VN: Dọn tween khi object bị hủy để tránh rác hoặc lỗi tham chiếu.
    /// </summary>
    private void OnDestroy()
    {
        KillBlinkTween();
    }

    /// <summary>
    /// VN: Chạy hiệu ứng nhấp nháy đúng 1 lần duy nhất.
    /// </summary>
    public void PlayBlink()
    {
        if (!CanPlayBlink())
            return;

        _hasPlayed = true;

        PrepareTextForBlink();
        StartBlinkTween();
        PlayBlinkSound();
    }

    /// <summary>
    /// VN: Kiểm tra điều kiện trước khi cho phép chạy blink.
    /// </summary>
    private bool CanPlayBlink()
    {
        if (_hasPlayed)
            return false;

        if (_textMesh == null)
            return false;

        if (_blinkTween != null && _blinkTween.IsActive() && _blinkTween.IsPlaying())
            return false;

        return true;
    }

    /// <summary>
    /// VN: Đưa text về trạng thái ban đầu trước khi blink.
    /// </summary>
    private void PrepareTextForBlink()
    {
        _textMesh.alpha = 1f;
    }

    /// <summary>
    /// VN: Tạo và chạy tween nhấp nháy cho text.
    /// </summary>
    private void StartBlinkTween()
    {
        float fadeDuration = totalDuration / (blinkCount * 2f);

        _blinkTween = _textMesh
            .DOFade(0f, fadeDuration)
            .SetLoops(blinkCount * 2, LoopType.Yoyo)
            .SetEase(Ease.Linear)
            .OnComplete(HandleBlinkCompleted);
    }

    /// <summary>
    /// VN: Xử lý sau khi hiệu ứng blink chạy xong.
    /// </summary>
    private void HandleBlinkCompleted()
    {
        _textMesh.alpha = 1f;
        OnBlinkFinished?.Invoke();
    }

    /// <summary>
    /// VN: Phát âm thanh khi bắt đầu blink.
    /// </summary>
    private void PlayBlinkSound()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlaySound(SoundCategory.Normal, 7);
    }

    /// <summary>
    /// VN: Hủy tween hiện tại nếu còn tồn tại.
    /// </summary>
    private void KillBlinkTween()
    {
        if (_blinkTween == null || !_blinkTween.IsActive())
            return;

        _blinkTween.Kill();
        _blinkTween = null;
    }
}