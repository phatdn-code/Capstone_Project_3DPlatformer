using UnityEngine;
using UnityEngine.Audio;

namespace PLAYERTWO.PlatformerProject
{
    public class AudioManager : SingletonMonobehaviour<AudioManager>
    {
        // Tên parameter trong Audio Mixer
        private const string VolumeParam = "VolumeVol";
        private const string MusicParam = "MusicVol";
        private const string SoundParam = "SoundVol";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource[] musics;
        [SerializeField] private AudioSource[] sounds;
        [SerializeField] private AudioSource[] storyVoices;

        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup volumeMixer;
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup soundMixer;

        [Header("Current Values")]
        private float volumeVol;
        private float musicVol;
        private float soundVol;

        /// <summary>
        /// Phát 1 bản nhạc theo index.
        /// </summary>
        public void PlayMusic(int musicToPlay)
        {
            if (!IsValidIndex(musics, musicToPlay))
                return;

            StopMusics();
            musics[musicToPlay].Play();
        }

        /// <summary>
        /// Dừng toàn bộ nhạc nền.
        /// </summary>
        public void StopMusics()
        {
            for (int i = 0; i < musics.Length; i++)
            {
                if (musics[i] != null)
                    musics[i].Stop();
            }
        }

        /// <summary>
        /// Phát hiệu ứng âm thanh theo index.
        /// </summary>
        public void PlaySound(int soundToPlay)
        {
            if (!IsValidIndex(sounds, soundToPlay))
                return;

            AudioSource sound = sounds[soundToPlay];
            sound.Stop();
            sound.Play();
        }

        /// <summary>
        /// Phát voice story theo index page.
        /// </summary>
        public void PlayStoryVoice(int voiceIndex)
        {
            if (!IsValidIndex(storyVoices, voiceIndex))
                return;

            StopStoryVoices();
            storyVoices[voiceIndex].Play();
        }

        /// <summary>
        /// Dừng toàn bộ voice story.
        /// </summary>
        public void StopStoryVoices()
        {
            for (int i = 0; i < storyVoices.Length; i++)
            {
                if (storyVoices[i] != null)
                    storyVoices[i].Stop();
            }
        }

        /// <summary>
        /// Cập nhật âm lượng tổng từ slider.
        /// </summary>
        public void SetVolumeSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.volumeVolSlider == null)
                return;

            volumeVol = SettingsManager.Instance.volumeVolSlider.value;
            SetMixerVolume(volumeMixer, VolumeParam, volumeVol);
        }

        /// <summary>
        /// Cập nhật âm lượng nhạc từ slider.
        /// </summary>
        public void SetMusicSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.musicVolSlider == null)
                return;

            musicVol = SettingsManager.Instance.musicVolSlider.value;
            SetMixerVolume(musicMixer, MusicParam, musicVol);
        }

        /// <summary>
        /// Cập nhật âm lượng hiệu ứng từ slider.
        /// </summary>
        public void SetSoundSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.soundVolSlider == null)
                return;

            soundVol = SettingsManager.Instance.soundVolSlider.value;
            SetMixerVolume(soundMixer, SoundParam, soundVol);
        }

        /// <summary>
        /// Trả về âm lượng tổng hiện tại.
        /// </summary>
        public float GetVolumeVol()
        {
            return volumeVol;
        }

        /// <summary>
        /// Trả về âm lượng nhạc hiện tại.
        /// </summary>
        public float GetMusicVol()
        {
            return musicVol;
        }

        /// <summary>
        /// Trả về âm lượng hiệu ứng hiện tại.
        /// </summary>
        public float GetSoundVol()
        {
            return soundVol;
        }

        /// <summary>
        /// Gán giá trị âm lượng vào mixer.
        /// </summary>
        private void SetMixerVolume(AudioMixerGroup mixerGroup, string parameterName, float value)
        {
            if (mixerGroup == null || mixerGroup.audioMixer == null)
                return;

            mixerGroup.audioMixer.SetFloat(parameterName, value);
        }

        /// <summary>
        /// Kiểm tra index có hợp lệ không.
        /// </summary>
        private bool IsValidIndex(AudioSource[] audioSources, int index)
        {
            return audioSources != null &&
                   index >= 0 &&
                   index < audioSources.Length &&
                   audioSources[index] != null;
        }
    }
}