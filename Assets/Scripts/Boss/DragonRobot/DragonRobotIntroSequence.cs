using System.Collections;
using UnityEngine;
using DG.Tweening;
using Unity.Cinemachine;
using PLAYERTWO.PlatformerProject;

namespace PLAYERTWO.PlatformerProject
{
    [RequireComponent(typeof(Collider))]
    public class DragonRobotIntroSequence : MonoBehaviour
    {
        [Header("Boss Settings")]
        [SerializeField] private BossCore bossCore;              // Boss
        [SerializeField] private Transform bossStartPoint;       // Vị trí boss xuất hiện ban đầu
        [SerializeField] private Transform bossCombatEntryPoint;        // Vị trí boss bay đến
        [SerializeField] private bool triggerOnce = true;

        [Header("Camera Settings")]
        [SerializeField] private CinemachineCamera bossIntroCamera;
        [SerializeField] private int cameraHighPriority = 100;
        [SerializeField] private int cameraNormalPriority = 0;

        [Header("Movement Settings")]
        [SerializeField] private float flyDuration = 2.0f;
        [SerializeField] private Ease flyEase = Ease.InOutQuad;

        private bool hasTriggered;

        //─────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            if (bossCore != null)
                bossCore.gameObject.SetActive(false);

            if (bossIntroCamera != null)
                bossIntroCamera.Priority = cameraNormalPriority;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            if (hasTriggered && triggerOnce) return;

            hasTriggered = true;
            StartCoroutine(RunSequence());
        }

        #endregion
        //─────────────────────────────────────────────



        //─────────────────────────────────────────────
        #region === INTRO SEQUENCE ===

        private IEnumerator RunSequence()
        {
            // 1) Lock player
            PlayerHub.Instance.LockPlayer(true);

            // 2) Camera intro ON
            if (bossIntroCamera != null)
                bossIntroCamera.Priority = cameraHighPriority;

            // 3) Turn boss ON
            bossCore.gameObject.SetActive(true);

            // 4) Đặt boss về vị trí gốc *trước khi bay*
            if (bossStartPoint != null)
            {
                bossCore.transform.SetPositionAndRotation(
                    bossStartPoint.position,
                    bossStartPoint.rotation
                );
            }

            // 5) Boss bay lên vị trí target
            if (bossCombatEntryPoint != null)
            {
                Tween flyTween = bossCore.transform
                    .DOMove(bossCombatEntryPoint.position, flyDuration)
                    .SetEase(flyEase);

                yield return flyTween.WaitForCompletion();
            }

            // 6) Tắt camera intro
            if (bossIntroCamera != null)
                bossIntroCamera.Priority = cameraNormalPriority;

            // 7) Bắt đầu cutscene shuffle
            CutscenePortalShuffle.Instance.StartCutsceneFlow();
        }

        #endregion
        //─────────────────────────────────────────────
    }
}
