using UnityEngine;

public class DungeonController : MonoBehaviour
{
    public GameObject Cinemachine01;
    public GameObject Cinemachine02;
    public int StarCout = 2;


    public void SwitchCinemachine(GameObject cinemachine)
    {
        if (Cinemachine01 != null)
        {
            Cinemachine02 = Cinemachine01;
            Cinemachine01 = cinemachine;
            Cinemachine01.SetActive(true);
            Cinemachine02.SetActive(false);
        }
        else
        {
            Cinemachine01 = cinemachine;
            Cinemachine01.SetActive(true);
        }
    }
}
