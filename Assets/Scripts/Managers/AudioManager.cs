using UnityEngine;
using UnityEngine.Audio;

namespace PLAYERTWO.PlatformerProject
{
    public class AudioManager : SingletonMonobehaviour<AudioManager>
    {
        // VN: Tên parameter trong Audio Mixer.
        private const string VolumeParam = "VolumeVol";
        private const string MusicParam = "MusicVol";
        private const string SoundParam = "SoundVol";

        [Header("Music Sources")]
        [SerializeField] private AudioSource[] musics;

        [Header("Sound Sources")]
        [SerializeField] private AudioSource[] normalSounds;
        [SerializeField] private AudioSource[] storySounds;
        [SerializeField] private AudioSource[] volTitanSounds;
        [SerializeField] private AudioSource[] pyrodrakeSounds;

        [Header("Mixer Groups")]
        [SerializeField] private AudioMixerGroup volumeMixer;
        [SerializeField] private AudioMixerGroup musicMixer;
        [SerializeField] private AudioMixerGroup soundMixer;

        // VN: Giá trị volume hiện tại.
        private float volumeVol;
        private float musicVol;
        private float soundVol;

        /// <summary>
        /// VN: Phát nhạc theo index.
        /// </summary>
        public void PlayMusic(int musicIndex)
        {
            if (!TryGetAudioSource(musics, musicIndex, out AudioSource source))
                return;

            StopArray(musics);
            source.Play();
        }

        /// <summary>
        /// VN: Dừng toàn bộ nhạc.
        /// </summary>
        public void StopMusics()
        {
            StopArray(musics);
        }

        /// <summary>
        /// VN: Phát sound theo nhóm và index.
        /// </summary>
        public void PlaySound(SoundCategory category, int soundIndex)
        {
            AudioSource[] targetGroup = GetSoundGroup(category);

            if (!TryGetAudioSource(targetGroup, soundIndex, out AudioSource source))
                return;

            if (ShouldStopWholeGroupBeforePlay(category))
                StopArray(targetGroup);

            else source.Stop();

            source.Play();
        }

        /// <summary>
        /// VN: Dừng đúng 1 sound theo nhóm và index.
        /// </summary>
        public void StopSound(SoundCategory category, int soundIndex)
        {
            // VN: Lấy đúng mảng sound của nhóm cần dừng.
            AudioSource[] targetGroup = GetSoundGroup(category);

            // VN: Kiểm tra index hợp lệ và lấy đúng AudioSource cần dừng.
            if (!TryGetAudioSource(targetGroup, soundIndex, out AudioSource source))
                return;

            // VN: Chỉ dừng đúng sound này, không ảnh hưởng sound khác.
            source.Stop();
        }

        /// <summary>
        /// VN: Dừng toàn bộ sound của 1 nhóm.
        /// </summary>
        public void StopSoundGroup(SoundCategory category)
        {
            StopArray(GetSoundGroup(category));
        }

        /// <summary>
        /// VN: Dừng toàn bộ sound của tất cả nhóm.
        /// </summary>
        public void StopAllSounds()
        {
            StopArray(normalSounds);
            StopArray(storySounds);
            StopArray(volTitanSounds);
            StopArray(pyrodrakeSounds);
        }

        /// <summary>
        /// VN: Cập nhật volume tổng từ slider.
        /// </summary>
        public void SetVolumeSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.volumeVolSlider == null)
                return;

            volumeVol = SettingsManager.Instance.volumeVolSlider.value;
            SetMixerVolume(volumeMixer, VolumeParam, volumeVol);
        }

        /// <summary>
        /// VN: Cập nhật volume nhạc từ slider.
        /// </summary>
        public void SetMusicSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.musicVolSlider == null)
                return;

            musicVol = SettingsManager.Instance.musicVolSlider.value;
            SetMixerVolume(musicMixer, MusicParam, musicVol);
        }

        /// <summary>
        /// VN: Cập nhật volume sound từ slider.
        /// </summary>
        public void SetSoundSlider()
        {
            if (SettingsManager.Instance == null || SettingsManager.Instance.soundVolSlider == null)
                return;

            soundVol = SettingsManager.Instance.soundVolSlider.value;
            SetMixerVolume(soundMixer, SoundParam, soundVol);
        }

        /// <summary>
        /// VN: Lấy volume tổng hiện tại.
        /// </summary>
        public float GetVolumeVol()
        {
            return volumeVol;
        }

        /// <summary>
        /// VN: Lấy volume nhạc hiện tại.
        /// </summary>
        public float GetMusicVol()
        {
            return musicVol;
        }

        /// <summary>
        /// VN: Lấy volume sound hiện tại.
        /// </summary>
        public float GetSoundVol()
        {
            return soundVol;
        }

        /// <summary>
        /// VN: Lấy mảng sound theo nhóm.
        /// </summary>
        private AudioSource[] GetSoundGroup(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Normal:
                    return normalSounds;

                case SoundCategory.Story:
                    return storySounds;

                case SoundCategory.VoltitanBoss:
                    return volTitanSounds;

                case SoundCategory.PyrodrakeBoss:
                    return pyrodrakeSounds;

                default:
                    return null;
            }
        }

        /// <summary>
        /// VN: Xác định nhóm nào cần stop cả mảng trước khi phát.
        /// </summary>
        private bool ShouldStopWholeGroupBeforePlay(SoundCategory category)
        {
            switch (category)
            {
                case SoundCategory.Story:
                    return true;

                case SoundCategory.Normal:
                case SoundCategory.PyrodrakeBoss:
                default:
                    return false;
            }
        }

        /// <summary>
        /// VN: Dừng toàn bộ AudioSource trong mảng.
        /// </summary>
        private void StopArray(AudioSource[] sources)
        {
            if (sources == null)
                return;

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                    sources[i].Stop();
            }
        }

        /// <summary>
        /// VN: Gán giá trị volume vào Audio Mixer.
        /// </summary>
        private void SetMixerVolume(AudioMixerGroup mixerGroup, string parameterName, float value)
        {
            if (mixerGroup == null || mixerGroup.audioMixer == null)
                return;

            mixerGroup.audioMixer.SetFloat(parameterName, value);
        }

        /// <summary>
        /// VN: Lấy AudioSource hợp lệ theo index.
        /// </summary>
        private bool TryGetAudioSource(AudioSource[] sources, int index, out AudioSource source)
        {
            source = null;

            if (sources == null)
                return false;

            if (index < 0 || index >= sources.Length)
                return false;

            if (sources[index] == null)
                return false;

            source = sources[index];
            return true;
        }
    }
}