using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class UIInput : MonoBehaviour
    {
        public AdvancedDissolveGeometricCutoutController geometricCutoutController;

        public Text fpsText;
        float deltaTime = 0.0f;
        bool displayFPS;

        public GameObject menu;



#if ENABLE_INPUT_SYSTEM
        Key keyF = Key.F;
        Key keyV = Key.V;
        Key keyN = Key.N;
        Key keyI = Key.I;
        Key keyH = Key.H;
#else
        KeyCode keyF = KeyCode.F;
        KeyCode keyV = KeyCode.V;
        KeyCode keyN = KeyCode.N;
        KeyCode keyI = KeyCode.I;
        KeyCode keyH = KeyCode.H;
#endif



        // Update is called once per frame
        void Update()
        {
            //FPS
            if (ExampleInput.GetKeyDown(keyF))
                displayFPS = !displayFPS;

            //VSync
            if (ExampleInput.GetKeyDown(keyV))
                QualitySettings.vSyncCount = (QualitySettings.vSyncCount == 0) ? 1 : 0;

            //Noise
            if (ExampleInput.GetKeyDown(keyN))
                geometricCutoutController.noise = (geometricCutoutController.noise > 0) ? 0 : 1;

            //Invert
            if (ExampleInput.GetKeyDown(keyI))
                geometricCutoutController.invert = !geometricCutoutController.invert;

            //Show/Hide Menu
            if (ExampleInput.GetKeyDown(keyH))
                menu.SetActive(!menu.activeSelf);


            UpdateFPS();
        }


        void UpdateFPS()
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;


            if (displayFPS)
            {
                float fps = 1.0f / deltaTime;
                fpsText.text = (int)fps + " fps";
            }
            else
                fpsText.text = string.Empty;
        }
    }
}