using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : SingletonMonobehaviour<SettingsManager>
{
    [Header("Volume Sliders")]
    public Slider volumeVolSlider;
    public Slider musicVolSlider;
    public Slider soundVolSlider;

    /// <summary>
    /// Khởi tạo dữ liệu khi bắt đầu scene.
    /// </summary>
    private void Start()
    {
        Initialize();
    }

    /// <summary>
    /// Gán giá trị hiện tại từ AudioManager vào các slider.
    /// </summary>
    private void Initialize()
    {
        if (AudioManager.Instance == null)
            return;

        if (volumeVolSlider != null)
            volumeVolSlider.value = AudioManager.Instance.GetVolumeVol();

        if (musicVolSlider != null)
            musicVolSlider.value = AudioManager.Instance.GetMusicVol();

        if (soundVolSlider != null)
            soundVolSlider.value = AudioManager.Instance.GetSoundVol();
    }

    /// <summary>
    /// Gửi giá trị slider âm lượng tổng sang AudioManager.
    /// </summary>
    public void SetVolumeSlider()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetVolumeSlider();
    }

    /// <summary>
    /// Gửi giá trị slider âm lượng nhạc sang AudioManager.
    /// </summary>
    public void SetMusicSlider()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetMusicSlider();
    }

    /// <summary>
    /// Gửi giá trị slider âm lượng hiệu ứng sang AudioManager.
    /// </summary>
    public void SetSoundSlider()
    {
        if (AudioManager.Instance == null)
            return;

        AudioManager.Instance.SetSoundSlider();
    }
}