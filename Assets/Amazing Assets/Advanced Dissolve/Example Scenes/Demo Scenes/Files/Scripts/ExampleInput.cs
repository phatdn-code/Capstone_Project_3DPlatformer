using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif


namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    static public class ExampleInput
    {
#if ENABLE_INPUT_SYSTEM
        static public bool GetKeyDown(Key key)
        {
            return Keyboard.current[key].wasPressedThisFrame;
        }

        static public bool GetKey(Key key)
        {
            return Keyboard.current[key].isPressed;
        }

        static public bool GetKeyLeftControl()
        {
            return Keyboard.current[Key.LeftCtrl].isPressed;
        }

        static public bool GetKeyRightControl()
        {
            return Keyboard.current[Key.RightCtrl].isPressed;
        }

        static public bool GetKeyLeftShift()
        {
            return Keyboard.current[Key.LeftShift].isPressed;
        }

        static public bool GetKeyRightShift()
        {
            return Keyboard.current[Key.RightShift].isPressed;
        }

        static public bool GetKeyUp(Key key)
        {
            return Keyboard.current[key].wasReleasedThisFrame;
        }


        static public bool GetMouseButton(int index)
        {
            switch (index)
            {
                case 0: return Mouse.current.leftButton.isPressed;
                case 1: return Mouse.current.rightButton.isPressed;

                default: Debug.LogError($"Undefined mouse button index [{index}]"); return false;
            };
        }
        static public bool GetMouseButtonUp(int index)
        {
            switch (index)
            {
                case 0: return Mouse.current.leftButton.wasReleasedThisFrame;
                case 1: return Mouse.current.rightButton.wasReleasedThisFrame;

                default: Debug.LogError($"Undefined mouse button index [{index}]"); return false;
            };
        }

        static public float GetMouseX()
        {
            return Mouse.current.delta.ReadValue().x / 10;
        }

        static public float GetMouseY()
        {
            return Mouse.current.delta.ReadValue().y / 10;
        }

        static public float GetMouseScrollWheel()
        {
            return Mathf.Clamp(Mouse.current.scroll.ReadValue().y, -1, 1);
        }

        static public float GetMouseScrollDeltaY()
        {
            return Mathf.Clamp(Mouse.current.scroll.ReadValue().y, -1, 1);
        }

        static public Vector3 MousePosition()
        {
            return Mouse.current.position.ReadValue();
        }
#else
        static public bool GetKeyDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        static public bool GetKey(KeyCode key)
        {
            return Input.GetKey(key);
        }

        static public bool GetKeyLeftControl()
        {
            return Input.GetKey(KeyCode.LeftControl);
        }

        static public bool GetKeyRightControl()
        {
            return Input.GetKey(KeyCode.RightControl);
        }

        static public bool GetKeyLeftShift()
        {
            return Input.GetKey(KeyCode.LeftShift);
        }

        static public bool GetKeyRightShift()
        {
            return Input.GetKey(KeyCode.RightShift);
        }

        static public bool GetKeyUp(KeyCode key)
        {
            return Input.GetKeyUp(key);
        }


        static public bool GetMouseButton(int index)
        {
            return Input.GetMouseButton(index);
        }

        static public bool GetMouseButtonUp(int index)
        {
            return Input.GetMouseButtonUp(index);
        }

        static public float GetMouseX()
        {
            return Input.GetAxis("Mouse X");
        }

        static public float GetMouseY()
        {
            return Input.GetAxis("Mouse Y");
        }

        static public float GetMouseScrollWheel()
        {
            return Input.GetAxis("Mouse ScrollWheel") * 10;
        }

        static public float GetMouseScrollDeltaY()
        {
            return Input.mouseScrollDelta.y;
        }

        static public Vector3 MousePosition()
        {
            return Input.mousePosition;
        }
#endif
    }
}