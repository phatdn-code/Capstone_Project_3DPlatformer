using UnityEngine;
using Unity.Cinemachine;

namespace PLAYERTWO.PlatformerProject
{
    public class PortalZone : MonoBehaviour
    {
        [Header("Portal Reference")]
        public Portal portal;

        [Header("Zone Camera")]
        public CinemachineCamera portalCamera;

        [Header("Portal Return Point")]
        public Transform portalTargetPoint;

        [Header("Dissolve Plane Settings")]
        public Transform dissolvePlane;
        public float dissolveStartY = 24f;
        public float dissolveEndY = -2f;

        [Header("Boss Entry Point")]
        public Transform bossEntryPoint;

        [Header("Boss Skill Points")]
        public Transform flameCastPoint;      // Điểm thực hiện Flame Thrower
        public Transform blastCastPoint;      // Điểm thực hiện Blast Attack

        [Header("Initial Portal Transform (runtime)")]
        [HideInInspector] public Vector3 initialPosition;
        [HideInInspector] public Quaternion initialRotation;

        private void Start()
        {
            if (portal == null) return;

            initialPosition = portal.transform.position;
            initialRotation = portal.transform.rotation;
        }
    }
}
