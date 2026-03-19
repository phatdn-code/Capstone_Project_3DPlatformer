using PLAYERTWO.PlatformerProject;
using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    [SerializeField] private int sfxIndex;

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void OpenDoor()
    {
        if (doorAnimator.GetBool("isClosed") == false) return;
        doorAnimator.SetBool("isClosed", false);
        AudioManager.Instance.PlaySound(SoundCategory.Normal, sfxIndex);
    }

    public void CloseDoor()
    {
        if(doorAnimator.GetBool("isClosed") == true) return;
        doorAnimator.SetBool("isClosed", true);
        AudioManager.Instance.PlaySound(SoundCategory.Normal, sfxIndex);
    }
}
