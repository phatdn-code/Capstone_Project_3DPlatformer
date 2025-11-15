using UnityEngine;

// Use INab.Common namespace for InteractiveEffect
using INab.Common;

public class InteractiveDissolveAPI : MonoBehaviour
{
    // Declare a variable to hold the InteractiveEffect component
    public InteractiveEffect effect;

    public Vector3 initialPositionPlane;
    public Vector3 finalPositionPlane;

    public Vector3 initialScaleEllipse;
    public Vector3 finalScaleEllipse;


    // Method to play the dissolve effect
    public void PlayEffects()
    {
        effect.PlayEffect();
    }

    // Play the effect inversed without particles with a custom duration.
    public void ReverseEffects()
    {
        effect.ReverseEffect(1);
    }
    // Manually updates mask transform based on the effect lerp value.
    public void UpdateMaskTransformManual(float effectLerp)
    {
        effect.UpdateMaskTransform(effectLerp);
    }

    // Method to update the guide strength of the effect
    public void UpdateGuideStrength(float strength)
    {
        // Set the guide strength and pass new material properties (in out case: GuideStrength property) to the vfx graph settings
        effect.GuideStrength = strength;
        effect.PassMaterialPropertiesToGraph();
    }

    // Method to change the effect to the plane mask type
    public void UsePlaneMaskType()
    {
        // Change the mask type to plane
        effect.ChangeMaskType(InteractiveEffectMaskType.Plane);

        // Use only the position transform
        effect.usePositionTransform = true;
        effect.useScaleTransform = false;

        // Set initial and final positions for the plane mask
        effect.initialPosition = initialPositionPlane;
        effect.finalPosition = finalPositionPlane;
    }

    // Method to change the effect to the ellipse mask type
    public void UseEllipseMaskType()
    {
        // Change the mask type to ellipse
        effect.ChangeMaskType(InteractiveEffectMaskType.Ellipse);

        // Use only the scale transform
        effect.usePositionTransform = false;
        effect.useScaleTransform = true;

        // Set the local position of the ellipse mask (here we are setting the position to be in the center of the mesh we are dissolving)
        effect.mask.transform.localPosition = new Vector3(0, 1, 0);

        // Set initial and final scales for the ellipse mask
        effect.initialScale = initialScaleEllipse;
        effect.finalScale = finalScaleEllipse;
    }

    ////////////////////////////////////////////////////////////////////////////////////

    // Method to send play event to the visual effect
    public void SendPlayEvents()
    {
        effect.visualEffect.Play();
    }

    // Method to send stop event to the visual effect
    public void SendStopEvents()
    {
        effect.visualEffect.Stop();
    }


    // Method to get the renderer materials of the effect
    public void GetRendererMaterials()
    {
        effect.GetRendererMaterials();
    }

    // Refreshes all properties and keywords for the materials managed by this script.
    public void UpdateAndSetupEffect()
    {
        effect.UpdateAndSetupEffect();
    }

    
}
