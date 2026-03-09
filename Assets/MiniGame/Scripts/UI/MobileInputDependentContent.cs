using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace MiniGame
{
#if UNITY_EDITOR

    [ExecuteInEditMode]
#endif

    /// <summary>
    /// Depending if MOBILE_INPUT is defined or not, activates/deactivates specified objects and components. 
    /// Executes only in edit mode.
    /// </summary>
    public class MobileInputDependentContent : MonoBehaviour
    {
        private enum InputMode
        {
            StandaloneInput,
            MobileInput
        }

        [SerializeField] private InputMode _inputMode;
        [SerializeField] private GameObject[] _content = new GameObject[0];
        [SerializeField] private MonoBehaviour[] _monoBehaviours = new MonoBehaviour[0];
        [SerializeField] private bool _childrenOfThisObject;

#if !UNITY_EDITOR
	void OnEnable()
	{
		CheckEnableContent();
	}
#endif

#if UNITY_EDITOR

        private void OnEnable()
        {
            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Update;
        }


        private void Update()
        {
            CheckEnableContent();
        }
#endif


        private void CheckEnableContent()
        {
#if (MOBILE_INPUT)
		if (_inputMode == InputMode.MobileInput)
		{
			EnableContent(true);
		} else {
			EnableContent(false);
		}
#endif

#if !(MOBILE_INPUT)
        if (_inputMode == InputMode.MobileInput)
        {
            EnableContent(false);
        }
        else
        {
            EnableContent(true);
        }
#endif
        }


        private void EnableContent(bool enabled)
        {
            if (_content.Length > 0)
            {
                foreach (var g in _content)
                {
                    if (g != null)
                    {
                        g.SetActive(enabled);
                    }
                }
            }
            if (_childrenOfThisObject)
            {
                foreach (Transform t in transform)
                {
                    t.gameObject.SetActive(enabled);
                }
            }
            if (_monoBehaviours.Length > 0)
            {
                foreach (var monoBehaviour in _monoBehaviours)
                {
                    monoBehaviour.enabled = enabled;
                }
            }
        }
    }
}