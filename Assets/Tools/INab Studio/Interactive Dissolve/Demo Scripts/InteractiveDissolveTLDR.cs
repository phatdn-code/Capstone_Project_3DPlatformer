using System.Collections.Generic;
using UnityEngine;

// Use INab.Common namespace for InteractiveEffect
using INab.Common;

public class InteractiveDissolveTLDR : MonoBehaviour
{
    // Declare a variable to hold the InteractiveEffect component
    public List<InteractiveEffect> interactiveEffects;

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            // Call the PlayEffect function when the 'P' key is pressed
            PlayEffectsInTheScene();
        }
    }

    public void PlayEffectsInTheScene()
    {
        // Call the PlayEffect method of the InteractiveEffects in the scene
        foreach (var effect in interactiveEffects)
        {
            if(effect) effect.PlayEffect();
        }
    }
}
