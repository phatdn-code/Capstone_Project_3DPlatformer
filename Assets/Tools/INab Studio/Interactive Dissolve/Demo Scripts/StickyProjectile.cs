using UnityEngine;

// We need to use INab.Common
using INab.Common;

namespace INab.InteractiveDissolveDemo
{
    public class StickyProjectile : MonoBehaviour
    {
        public LayerMask mask;

        private void OnCollisionEnter(Collision other)
        {
            // Checking whether projectile hit object in layer mask
            if (mask != (mask | (1 << other.gameObject.layer))) return;

            // Trying to find InteractiveEffect in parent
            InteractiveEffect effect = other.gameObject.GetComponentInParent<InteractiveEffect>();

            // Trying to find InteractiveEffect in children
            if (effect == null) effect = other.gameObject.GetComponentInChildren<InteractiveEffect>();

            // Trying to find InteractiveEffect when its attached to a skinned character root game object with animator
            var animator = other.gameObject.GetComponentInParent<Animator>();
            if (effect == null) effect = animator.GetComponentInChildren<InteractiveEffect>();

            // If we found InteractiveEffect
            if (effect != null)
            {
                // We are attaching the object that got hit. (makes sure that mask follows bones of skinned mesh renderers)
                Transform maskTransform = effect.mask.transform;
                maskTransform.parent = other.gameObject.transform;

                // Changing the mask position to the hit poit.
                maskTransform.position = other.GetContact(0).point;

                // Making sure that we are using ellipse mask type.
                effect.ChangeMaskType(InteractiveEffectMaskType.Ellipse);

                // We set the mask position to a fixed local position of hit point. Now we only want to control its scale.
                effect.usePositionTransform = false;
                effect.useScaleTransform = true;

                // Initial scale of the mask (when the renderer is not dissolved; effect state value is equal to 0)
                effect.initialScale = Vector3.zero;

                // Caluclating maximum radius of the mask scale based on the renderer bounds.
                //=============================================================================
                Renderer renderer = effect.meshRenderer;
                Bounds bounds = renderer.bounds;
                Vector3 point = other.GetContact(0).point;
                Vector3 center = bounds.center;

                Vector3 diff = point - center;
                float distanceToPoint = diff.magnitude;
                float radius = distanceToPoint + bounds.extents.magnitude;
               
                var scale = 1f / maskTransform.parent.lossyScale.x;
                radius *= scale;
                radius *= 1.35f;
                //=============================================================================

                // Final scale of the mask (when the renderer is fully dissolved)
                effect.finalScale = new Vector3(radius, radius, radius);

                // Playing the effect
                effect.PlayEffect();

                // Destroing the sticjky projectile
                Destroy(gameObject);
            }
            else
            {
                Debug.Log("No interactive effect detected");
            }
        }

    }
}