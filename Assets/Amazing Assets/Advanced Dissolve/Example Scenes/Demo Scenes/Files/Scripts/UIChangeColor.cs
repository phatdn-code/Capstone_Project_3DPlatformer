using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AmazingAssets.AdvancedDissolve.ExampleScripts
{
    public class UIChangeColor : MonoBehaviour
    {
        public AdvancedDissolvePropertiesController propertiesController;

        public Color color = new Color(0, 0.685f, 1, 1);
        float hue = 199f / 360f;


        [Space(10)]
        public Material[] diffuseMaterial;
        public Material[] emissionMaterial;
        public Light[] lights;

        // Use this for initialization
        void Start()
        {
            UpdateColors();
        }

        // Update is called once per frame
        void Update()
        { 
            if (ExampleInput.GetKeyLeftShift() || ExampleInput.GetKeyRightShift())
                UpdateColors();
        }

        void UpdateColors()
        {
            //Calculate color
            float s, v;
            Color.RGBToHSV(color, out hue, out s, out v);

            hue += ExampleInput.GetMouseScrollWheel() * 0.01f;
            if (hue > 1)
                hue -= 1;

            color = Color.HSVToRGB(hue, s, v);


            //Update Dissolve shaders color
            propertiesController.edgeBase.color = color;




            //Updating scene objects color, I know it is not optimized way.
            if (diffuseMaterial != null)
            {
                for (int i = 0; i < diffuseMaterial.Length; i++)
                {
                    if (diffuseMaterial[i] == null)
                        continue;

                    if (diffuseMaterial[i].HasProperty("_Color"))
                        diffuseMaterial[i].SetColor("_Color", GetColor(diffuseMaterial[i].GetColor("_Color")));

                    if (diffuseMaterial[i].HasProperty("_TintColor"))
                        diffuseMaterial[i].SetColor("_TintColor", GetColor(diffuseMaterial[i].GetColor("_TintColor")));

                    if (diffuseMaterial[i].HasProperty("_BaseColor"))
                        diffuseMaterial[i].SetColor("_BaseColor", GetColor(diffuseMaterial[i].GetColor("_BaseColor")));

                    if (diffuseMaterial[i].HasProperty("_UnlitColor"))
                        diffuseMaterial[i].SetColor("_UnlitColor", GetColor(diffuseMaterial[i].GetColor("_UnlitColor")));
                }
            }

            if (emissionMaterial != null)
            {
                for (int i = 0; i < emissionMaterial.Length; i++)
                {
                    if (emissionMaterial[i] == null)
                        continue;

                    if (emissionMaterial[i].HasProperty("_Color"))
                        emissionMaterial[i].SetColor("_Color", GetColor(emissionMaterial[i].GetColor("_Color")));

                    if (emissionMaterial[i].HasProperty("_EmissionColor"))
                        emissionMaterial[i].SetColor("_EmissionColor", GetColor(emissionMaterial[i].GetColor("_EmissionColor")));

                    if (emissionMaterial[i].HasProperty("_EmissiveColor"))
                        emissionMaterial[i].SetColor("_EmissiveColor", GetColor(emissionMaterial[i].GetColor("_EmissiveColor")));
                }
            }

            if (lights != null)
            {
                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] == null)
                        continue;

                    lights[i].color = GetColor(lights[i].color);
                }
            }

        }

        public Color GetColor(Color _color)
        {
            float h, s, v;
            Color.RGBToHSV(_color, out h, out s, out v);

            Color finColor = Color.HSVToRGB(hue, s, v);
            finColor.a = _color.a;

            return finColor;
        }
    }
}