using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

namespace PLAYERTWO.PlatformerProject
{
    [DisallowMultipleComponent]
    public class CameraCutsceneController : SingletonMonobehaviour<CameraCutsceneController>
    {
        //─────────────────────────────────────────────
        #region === Inspector References ===

        [Header("Boss Cameras")]
        [SerializeField] private CinemachineCamera bossCam;
        [SerializeField] private CinemachineCamera specialCam;

        [Header("Blend Settings")]
        [SerializeField] private float focusBlendTime = 1.5f;
        [SerializeField] private int activePriority = 100;
        [SerializeField] private int inactivePriority = 0;

        [Header("Smooth Blend Delay")]
        [SerializeField] private int keepPreviousFrames = 5;     // giữ camera cũ trong X frame

        #endregion
        //─────────────────────────────────────────────


        private CinemachineCamera previousCam;


        //─────────────────────────────────────────────
        #region === PUBLIC API ===

        /// <summary>
        /// Focus to a specific boss camera type with smooth blending.
        /// </summary>
        public IEnumerator FocusTo(BossCamType camType)
        {
            CinemachineCamera target = GetCameraByType(camType);

            if (target == null)
                yield break;

            var current = GetCurrentActiveCamera();

            if (current == bossCam || current == specialCam)
                previousCam = current;

            else previousCam = null;

            // Active target cam
            target.Priority = activePriority;

            // Hạ camera cũ sau vài frame nếu có
            if (previousCam != null && previousCam != target)
                StartCoroutine(LowerAfterFrames(previousCam, keepPreviousFrames));

            yield return new WaitForSeconds(focusBlendTime);
        }



        /// <summary>
        /// Return the view back to the previously active boss camera.
        /// </summary>
        public IEnumerator FocusBackToPrevious()
        {
            if (previousCam == null)
                yield break;

            previousCam.Priority = activePriority;
            yield return new WaitForSeconds(focusBlendTime);
        }


        /// <summary>
        /// Disable all boss cameras and return control fully to the player camera.
        /// </summary>
        public IEnumerator ReleaseToPlayer()
        {
            ResetAllImmediate();
            yield return null;
        }


        /// <summary>
        /// Assign a dynamic target (e.g., a bomb) to the special camera.
        /// </summary>
        public void AssignSpecialTarget(Transform target)
        {
            if (specialCam == null || target == null) return;

            specialCam.Follow = target;
            specialCam.LookAt = target;
        }

        /// <summary>
        /// Clear the dynamic target from the special camera.
        /// </summary>
        public void ClearSpecialTarget()
        {
            if (specialCam == null) return;

            specialCam.Follow = null;
            specialCam.LookAt = null;
        }

        #endregion
        //─────────────────────────────────────────────


        #region === PRIVATE METHODS ===

        /// <summary>
        /// Hạ priority camera cũ sau X frame (để blend mượt).
        /// </summary>
        private IEnumerator LowerAfterFrames(CinemachineCamera cam, int frames)
        {
            for (int i = 0; i < frames; i++)
                yield return null;

            if (cam != null)
                cam.Priority = inactivePriority;
        }


        /// <summary>
        /// Immediately reset all boss cameras to inactive priority.
        /// </summary>
        public void ResetAllImmediate()
        {
            if (bossCam) bossCam.Priority = inactivePriority;
            if (specialCam) specialCam.Priority = inactivePriority;
        }


        /// <summary>
        /// Return the currently active boss camera (if any).
        /// </summary>
        private CinemachineCamera GetCurrentActiveCamera()
        {
            if (bossCam != null && bossCam.Priority == activePriority) return bossCam;
            if (specialCam != null && specialCam.Priority == activePriority) return specialCam;
            return null;
        }


        /// <summary>
        /// Return the camera corresponding to the requested type.
        /// </summary>
        private CinemachineCamera GetCameraByType(BossCamType type)
        {
            switch (type)
            {
                case BossCamType.Boss: return bossCam;
                case BossCamType.Special: return specialCam;
                default: return null;
            }
        }

        #endregion
    }


    //─────────────────────────────────────────────
    public enum BossCamType
    {
        Boss,
        Special
    }
}
