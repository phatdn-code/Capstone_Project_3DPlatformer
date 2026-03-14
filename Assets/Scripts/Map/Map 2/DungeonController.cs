using UnityEngine;

public class DungeonController : MonoBehaviour
{
    [SerializeField] private GameObject cinemachine01;
    [SerializeField] private GameObject cinemachine02;


    public void SwitchCinemachine(GameObject cinemachine)
    {
        if (cinemachine01 != null)
        {
            cinemachine02 = cinemachine01;
            cinemachine01 = cinemachine;
            cinemachine01.SetActive(true);
            cinemachine02.SetActive(false);
        }
        else
        {
            cinemachine01 = cinemachine;
            cinemachine01.SetActive(true);
        }
    }
}
