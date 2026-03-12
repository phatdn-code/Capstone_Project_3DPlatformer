using UnityEngine;

public class Door : MonoBehaviour
{
    [SerializeField] private Animator doorAnimator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    
    public void OpenDoor()
    {
        doorAnimator.SetBool("isClosed", false);
    }

    public void CloseDoor()
    {
        doorAnimator.SetBool("isClosed", true);
    }
}
