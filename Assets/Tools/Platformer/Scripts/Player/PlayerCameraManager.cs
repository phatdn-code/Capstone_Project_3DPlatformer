using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Camera Manager")]
	public class PlayerCameraManager : MonoBehaviour
	{
		[Header("Cameras")]
		[Tooltip("The camera used for the Third Person mode.")]
		public CinemachineCamera thirdPersonCamera;

		[Tooltip("The camera used for the Side Scroller mode.")]
		public CinemachineCamera sideScrollerCamera;

		protected Camera m_mainCamera;
		protected CinemachineBrain m_cinemachineBrain;
		protected CinemachineCamera m_currentCamera;
		protected Dictionary<PlayerMovementMode, CinemachineCamera> m_cameras;

		protected PlayerCamera m_tempPlayerCamera;

		protected float m_initialBlendTime;

		public Player player => Level.instance.player;

		protected virtual void Start()
		{
			InitializeCallbacks();
			InitializeCameras();
			InitializeBrain();
			ActivateCurrentCamera();
		}

		protected virtual void InitializeCallbacks()
		{
			player.playerEvents.OnMovementModeChanged.AddListener(SwitchCamera);
			Level.instance.onPlayerChanged.AddListener(player =>
				player.playerEvents.OnMovementModeChanged.AddListener(SwitchCamera)
			);
		}

		protected virtual void InitializeCameras()
		{
			m_mainCamera = Camera.main;
			m_cameras = new()
			{
				{ PlayerMovementMode.ThirdPerson, thirdPersonCamera },
				{ PlayerMovementMode.SideScroller, sideScrollerCamera },
			};
		}

		protected virtual void InitializeBrain()
		{
			m_cinemachineBrain = m_mainCamera.GetComponent<CinemachineBrain>();
		}

		protected virtual void ActivateCurrentCamera()
		{
			m_initialBlendTime = m_cinemachineBrain.DefaultBlend.Time;
			m_cinemachineBrain.DefaultBlend.Time = 0f;
			SwitchCamera(player.movingMode);
			m_cinemachineBrain.DefaultBlend.Time = m_initialBlendTime;
		}

		protected virtual void DisableAll()
		{
			foreach (var camera in m_cameras.Values)
				camera.gameObject.SetActive(false);
		}

		protected virtual void SwitchCamera(PlayerMovementMode mode)
		{
			DisableAll();

			player.inputs.LockMovementDirection();

			if (m_cameras.TryGetValue(mode, out m_currentCamera))
			{
				m_currentCamera.gameObject.SetActive(true);

				if (m_currentCamera.TryGetComponent(out m_tempPlayerCamera))
					m_tempPlayerCamera.Reset();
			}
		}
	}
}
