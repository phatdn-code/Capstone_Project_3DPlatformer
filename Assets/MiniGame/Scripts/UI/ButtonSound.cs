using UnityEngine;

/// <summary>
/// Simple script that plays a sound once a method is called. Useful for sounds happening on UI button clicks.
/// </summary>
public class ButtonSound : MonoBehaviour
{
    /// <summary>
    /// Name of GameObject holding an AudioSource for sound effects.
    /// </summary>
    public string audioSourceContainerName = "SoundFX";
    
    /// <summary>
    /// The sound to play whenever PlaySound() is called.
    /// </summary>
    public AudioClip sound;

    private AudioSource _audioSource;

    void Start()
    {
        var audioSourceGO = GameObject.Find(audioSourceContainerName);
        if (audioSourceGO != null)
        {
            _audioSource = audioSourceGO.GetComponent<AudioSource>();
        }
    }

    /// <summary>
	/// Play button sound once.
	/// </summary>
	public virtual void PlaySound()
    {
        if (_audioSource != null)
        {
            _audioSource.PlayOneShot(sound);
        }
    }

    /// <summary>
	/// Play button sound once. This overload is useful for toggle groups.
	/// </summary>
	/// <param name="play">If the sound should be played. This method does nothing if false is passed.</param>
	public virtual void PlaySound(bool activated)
    {
        if (activated)
        {
            PlaySound();
        }
    }
}
