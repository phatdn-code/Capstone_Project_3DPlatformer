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
        public Transform flameCastPoint;          // Điểm thực hiện Flame Thrower
        public Transform blastCastPoint;          // Điểm thực hiện Blast Attack
        public Transform[] meteorCastPoints;      // Các điểm di chuyển/strike của Meteor Attack
        public Transform[] meteorRainPoints;      // Các điểm spawn / vùng mưa Meteor

        [Header("Meteor Settings")]
        public float meteorHeightY = 6.3f;        // Độ cao Y khi Boss bay dùng Meteor

        [Header("Meteor Rain Settings")]
        public float meteorRainHeightY = 10f;     // Độ cao Y khi Boss dùng skill mưa Meteor

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
