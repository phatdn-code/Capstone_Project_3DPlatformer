using System.Collections.Generic;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[RequireComponent(typeof(Player))]
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Animator")]
	public class PlayerAnimator : MonoBehaviour
	{
		[System.Serializable]
		public class ForcedTransition
		{
			[Tooltip(
				"The index of the Player State from the Player State Manager that you want to force a transition from."
			)]
			public int fromStateId;

			[Tooltip(
				"The index of the layer from your Animator Controller that contains the target animation. (It's 0 if the animation is inside the 'Base Layer')"
			)]
			public int animationLayer;

			[Tooltip(
				"The name of the Animation State you want to play right after finishing the Player State from above."
			)]
			public string toAnimationState;
		}

		[Tooltip("Reference to the Animator component of the Player model.")]
		public Animator animator;

		[Header("Parameters Names")]
		[Tooltip("The name of the state parameter in the Animator Controller.")]
		public string stateName = "State";

		[Tooltip("The name of the last state parameter in the Animator Controller.")]
		public string lastStateName = "Last State";

		[Tooltip("The name of the lateral speed parameter in the Animator Controller.")]
		public string lateralSpeedName = "Lateral Speed";

		[Tooltip("The name of the vertical speed parameter in the Animator Controller.")]
		public string verticalSpeedName = "Vertical Speed";

		[Tooltip("The name of the ledge hanging speed parameter in the Animator Controller.")]
		public string ledgeHangingSpeed = "Ledge Hanging Speed";

		[Tooltip("The name of the lateral animation speed parameter in the Animator Controller.")]
		public string lateralAnimationSpeedName = "Lateral Animation Speed";

		[Tooltip("The name of the health parameter in the Animator Controller.")]
		public string healthName = "Health";

		[Tooltip("The name of the jump counter parameter in the Animator Controller.")]
		public string jumpCounterName = "Jump Counter";

		[Tooltip("The name of the is grounded parameter in the Animator Controller.")]
		public string isGroundedName = "Is Grounded";

		[Tooltip("The name of the is holding parameter in the Animator Controller.")]
		public string isHoldingName = "Is Holding";

		[Tooltip("The name of the on state changed trigger in the Animator Controller.")]
		public string onStateChangedName = "On State Changed";

		[Header("Settings")]
		[Tooltip("The minimum speed the lateral animation can reach.")]
		public float minLateralAnimationSpeed = 0.5f;

		[Tooltip("The list of forced transitions.")]
		public List<ForcedTransition> forcedTransitions;

		protected int m_stateHash;
		protected int m_lastStateHash;
		protected int m_lateralSpeedHash;
		protected int m_verticalSpeedHash;
		protected int m_ledgeHangingSpeedHash;
		protected int m_lateralAnimationSpeedHash;
		protected int m_healthHash;
		protected int m_jumpCounterHash;
		protected int m_isGroundedHash;
		protected int m_isHoldingHash;
		protected int m_onStateChangedHash;

		protected Dictionary<int, ForcedTransition> m_forcedTransitions;

		protected Player m_player;

		protected virtual void InitializePlayer()
		{
			m_player = GetComponent<Player>();
			m_player.states.events.onChange.AddListener(HandleForcedTransitions);
		}

		protected virtual void InitializeAnimator()
		{
			if (animator)
				return;

			LogAnimatorWarning();
			var root = m_player.skin ? m_player.skin : transform;
			animator = root.GetComponentInChildren<Animator>();
		}

		protected virtual void InitializeForcedTransitions()
		{
			m_forcedTransitions = new Dictionary<int, ForcedTransition>();

			foreach (var transition in forcedTransitions)
			{
				if (!m_forcedTransitions.ContainsKey(transition.fromStateId))
				{
					m_forcedTransitions.Add(transition.fromStateId, transition);
				}
			}
		}

		protected virtual void InitializeCallbacks()
		{
			m_player.health.onChange.AddListener(HandleHealthChange);
			m_player.states.events.onChange.AddListener(() =>
			{
				HandleStateChange();
				HandleForcedTransitions();
			});
		}

		protected virtual void InitializeParametersHash()
		{
			m_stateHash = Animator.StringToHash(stateName);
			m_lastStateHash = Animator.StringToHash(lastStateName);
			m_lateralSpeedHash = Animator.StringToHash(lateralSpeedName);
			m_verticalSpeedHash = Animator.StringToHash(verticalSpeedName);
			m_ledgeHangingSpeedHash = Animator.StringToHash(ledgeHangingSpeed);
			m_lateralAnimationSpeedHash = Animator.StringToHash(lateralAnimationSpeedName);
			m_healthHash = Animator.StringToHash(healthName);
			m_jumpCounterHash = Animator.StringToHash(jumpCounterName);
			m_isGroundedHash = Animator.StringToHash(isGroundedName);
			m_isHoldingHash = Animator.StringToHash(isHoldingName);
			m_onStateChangedHash = Animator.StringToHash(onStateChangedName);
		}

		protected virtual void HandleStateChange()
		{
			animator.SetInteger(m_stateHash, m_player.states.index);
			animator.SetInteger(m_lastStateHash, m_player.states.lastIndex);
			animator.SetTrigger(m_onStateChangedHash);
		}

		protected virtual void HandleHealthChange()
		{
			animator.SetInteger(m_healthHash, m_player.health.current);
		}

		protected virtual void HandleForcedTransitions()
		{
			var lastStateIndex = m_player.states.lastIndex;

			if (m_forcedTransitions.ContainsKey(lastStateIndex))
			{
				var layer = m_forcedTransitions[lastStateIndex].animationLayer;
				animator.Play(m_forcedTransitions[lastStateIndex].toAnimationState, layer);
			}
		}

		protected virtual void HandleAnimatorParameters()
		{
			var lateralSpeed = m_player.lateralVelocity.magnitude;
			var verticalSpeed = m_player.verticalVelocity.y;
			var lateralAnimationSpeed = Mathf.Max(
				minLateralAnimationSpeed,
				lateralSpeed / m_player.stats.current.topSpeed
			);
			var ledgeHangingSpeed = Vector3.Dot(m_player.lateralVelocity, m_player.localRight);

			animator.SetFloat(m_lateralSpeedHash, lateralSpeed);
			animator.SetFloat(m_verticalSpeedHash, verticalSpeed);
			animator.SetFloat(m_ledgeHangingSpeedHash, ledgeHangingSpeed);
			animator.SetFloat(m_lateralAnimationSpeedHash, lateralAnimationSpeed);
			animator.SetInteger(m_jumpCounterHash, m_player.jumpCounter);
			animator.SetBool(m_isGroundedHash, m_player.isGrounded);
			animator.SetBool(m_isHoldingHash, m_player.holding);
		}

		protected virtual void LogAnimatorWarning()
		{
			Debug.LogWarning(
				"PlayerAnimator: The animator reference is missing. "
					+ "The component will try to find it in the children of the player's skin. "
					+ "Manually assign it in the Inspector to avoid referencing the wrong animator.",
				this
			);
		}

		protected virtual void Start()
		{
			InitializePlayer();
			InitializeAnimator();
			InitializeForcedTransitions();
			InitializeParametersHash();
			InitializeCallbacks();
		}

		protected virtual void LateUpdate() => HandleAnimatorParameters();
	}
}
