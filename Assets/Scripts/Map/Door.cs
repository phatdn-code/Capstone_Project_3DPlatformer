using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioClip doorSfx;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void OpenDoor()
    {
        if (doorAnimator.GetBool("isClosed") == false) return;
        doorAnimator.SetBool("isClosed", false);
        if(doorAudioSource != null && doorSfx != null)
        {
            doorAudioSource.PlayOneShot(doorSfx);
        }
    }

    public void CloseDoor()
    {
        if(doorAnimator.GetBool("isClosed") == true) return;
        doorAnimator.SetBool("isClosed", true);
        if (doorAudioSource != null && doorSfx != null)
        {
            doorAudioSource.PlayOneShot(doorSfx);
        }
    }
}
