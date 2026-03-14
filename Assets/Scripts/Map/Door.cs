using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void OpenDoor()
    {
        if (doorAnimator.GetBool("isClosed") == false) return;
        doorAnimator.SetBool("isClosed", false);
    }

    public void CloseDoor()
    {
        if(doorAnimator.GetBool("isClosed") == true) return;
        doorAnimator.SetBool("isClosed", true);
    }
}
