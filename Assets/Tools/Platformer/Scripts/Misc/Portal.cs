using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Collider))]
    [AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Portal")]
    public class Portal : MonoBehaviour
    {
        //─────────────────────────────────────────────────────────────
        #region === Inspector Fields ===

        [Tooltip("If true, the teleportation will trigger the flash effect form the Flash component.")]
        public bool useFlash = true;

        [Tooltip("The portal to teleport to.")]
        public Portal exit;

        [Tooltip("The forward offset to apply to the Player position when exiting the portal.")]
        public float exitOffset = 1f;

        [Tooltip(
            "If true, the Player will be rotated to face the opposite direction when exiting the portal."
            + "Only works when the Player is in side-scroller mode."
        )]
        public bool invertExitDirection;

        [Tooltip("If enabled, the Player will save this portal as the respawn point when entering.")]
        public bool saveRespawnPoint = false;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Runtime References ===

        protected Collider m_collider;

        protected PlayerCamera m_camera;
        protected PlayerCameraManager m_cameraManager;

        protected Player player;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Properties ===

        /// <summary>Returns the Portal global position.</summary>
        public Vector3 position => transform.position;

        /// <summary>Returns the Portal local forward direction.</summary>
        public Vector3 forward => transform.forward;

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Unity Callbacks ===

        protected virtual void Start()
        {
            CacheReferences();
            EnsureTriggerCollider();
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!exit || !other.TryGetComponent(out Player player))
                return;

            if (saveRespawnPoint)
            {
                PortalZoneManager.Instance.ActivateZoneByPortal(exit);
                player.SetRespawn(exit.position, exit.transform.rotation);
            }

            var offset = player.unsizedPosition - transform.position;
            var yOffset = Vector3.Dot(transform.up, offset);

            var localExitForward =
                Quaternion.FromToRotation(exit.transform.up, Vector3.up) * exit.forward;

            var lateralSpeed = player.lateralVelocity.magnitude;
            var verticalSpeed = player.verticalVelocity.y;

            if (player.IsSideScroller)
                player.pathForward = localExitForward * (invertExitDirection ? -1 : 1);

            player.transform.SetPositionAndRotation(
                exit.position + exit.transform.up * yOffset,
                exit.transform.rotation
            );

            player.FaceDirection(localExitForward);
            player.LockGravity();

            player.gravityField?.IgnoreCollider(player.controller);
            player.gravityField = null;

            var inputDirection = player.inputs.GetMovementCameraDirection();
            if (Vector3.Dot(inputDirection, localExitForward) < 0)
                player.FaceDirection(-localExitForward);

            player.transform.position += player.transform.forward * exit.exitOffset;
            player.lateralVelocity = player.localForward * lateralSpeed;
            player.verticalVelocity = Vector3.up * verticalSpeed;

            Physics.SyncTransforms();

            m_cameraManager?.ResetCurrentCamera();

            if (useFlash && Flash.instance)
                Flash.instance.Trigger();

            AudioManager.Instance.PlaySound(SoundCategory.Normal, 8);

            if (saveRespawnPoint)
                PortalZoneManager.Instance.TryRunReturnPortalCutscene(exit);
        }

        #endregion
        //─────────────────────────────────────────────────────────────


        //─────────────────────────────────────────────────────────────
        #region === Helpers ===

        private void CacheReferences()
        {
            player = PlayerHub.Instance.Player;

            m_collider = GetComponent<Collider>();

#if UNITY_6000_0_OR_NEWER
            m_camera = FindFirstObjectByType<PlayerCamera>();
            m_cameraManager = FindFirstObjectByType<PlayerCameraManager>();
#else
            m_camera = FindObjectOfType<PlayerCamera>();
            m_cameraManager = FindObjectOfType<PlayerCameraManager>();
#endif
        }

        private void EnsureTriggerCollider()
        {
            m_collider.isTrigger = true;
        }

        #endregion
        //─────────────────────────────────────────────────────────────
    }
}
