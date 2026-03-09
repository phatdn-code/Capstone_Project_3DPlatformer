using UnityEngine;

/// <summary>
/// Depending if MOBILE_INPUT is defined or not, activates/deactivates standalone and mobile UI. 
/// Executes only in edit mode.
/// </summary>
[ExecuteInEditMode]
public class PlatformDependentActivator : MonoBehaviour
{
    public GameObject standalone;
    public GameObject mobile;

#if UNITY_EDITOR
    void OnGUI()
    {
#if MOBILE_INPUT
        if (mobile && !mobile.activeSelf)
        {
            mobile.SetActive(true);
        }
        if (standalone && standalone.activeSelf)
        {
            standalone.SetActive(false);
        }
#else
        if (standalone && !standalone.activeSelf)
        {
            standalone.SetActive(true);
        }
        if (mobile && mobile.activeSelf)
        {
            mobile.SetActive(false);
        }
#endif
    }
#endif
}
