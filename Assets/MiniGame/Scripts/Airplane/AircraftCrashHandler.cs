using UnityEngine;
using System.Collections;
using UnityStandardAssets.Utility;
using UnityStandardAssets.ImageEffects;
using System;

namespace MiniGame
{
	/// <summary>
	/// Detects when the airplane has crashed and controls the corresponding effects.
	/// </summary>
    public class AircraftCrashHandler : MonoBehaviour
    {
		/// <summary>
		/// Collision impulste which is considered enough for a crash, leads to respawning.
		/// </summary>
        public float crashImpulse = 10f;
		/// <summary>
		/// Collision impulse which is too big to ignore, but not enough for crash. Used for effects.
		/// </summary>
        public float hitImpulse = 5f;

		/// <summary>
		/// The sound of airplane crashing, which leads to being respawned.
		/// </summary>
        [Space]
        public AudioClip crash;
		/// <summary>
		/// The sound of airplane hitting something, not hard enough for a crash. Doesn't lead to respawning.
		/// </summary>
        public AudioClip hit;

        private AudioSource _soundSource;

        void Start()
        {
            _soundSource = GetComponent<AudioSource>();
        }

        public void OnCollisionEnter(Collision collision)
        {
            float collisionMagnitude = collision.impulse.magnitude;

            // Do not register small collisions with the take off platform.
            if (!collision.gameObject.CompareTag(GameTags.TakeOffPlatform))
            {
                if (collisionMagnitude > hitImpulse)
                {
                    RegisterHit();
                }
            }

            // If the collision is strong enough to reset the plane.
            if (collisionMagnitude > crashImpulse)
            {
                RegisterCrash();
            }
        }

        private void RegisterHit()
        {
            if (_soundSource != null && _soundSource.isActiveAndEnabled)
            {
                _soundSource.PlayOneShot(crash);
            }

            StartCoroutine(CollisionCameraAnimation());
        }

        private void RegisterCrash()
        {
            // Play hit sound fx.
            if (_soundSource != null)
            {
                _soundSource.PlayOneShot(hit);
            }

            // Clear trails of the plane.
            var trails = GetComponent<AircraftTrailController>();
            trails.DeactivateTrails();
            trails.ClearTrails();

            // Reset the plane.
            var reset = GetComponent<ObjectResetter>();
            if (reset != null)
            {
                reset.DelayedReset(0.4f);
            }
        }

        private IEnumerator CollisionCameraAnimation()
        {
            // Enable glitch component for short time.
            var cameraFx = GameObject.FindFirstObjectByType<NoiseAndScratches>();
            if (cameraFx != null) {
                cameraFx.enabled = true;

                yield return new WaitForSeconds(1.0f);

                cameraFx.enabled = false;
            }
        }

    }

}