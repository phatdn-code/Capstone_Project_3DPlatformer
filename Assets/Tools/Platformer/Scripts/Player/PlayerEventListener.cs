using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/Player Event Listener")]
	public class PlayerEventListener : MonoBehaviour
	{
		public Player player;
		public PlayerEvents events;

		protected virtual void InitializePlayer()
		{
			if (!player)
			{
				player = GetComponentInParent<Player>();
			}
		}

		protected virtual void InitializeCallbacks()
		{
			player.playerEvents.OnJump.AddListener(() => events.OnJump.Invoke());
			player.playerEvents.OnHurt.AddListener(() => events.OnHurt.Invoke());
			player.playerEvents.OnDie.AddListener(() => events.OnDie.Invoke());
			player.playerEvents.OnSpin.AddListener(() => events.OnSpin.Invoke());
			player.playerEvents.OnPickUp.AddListener(() => events.OnPickUp.Invoke());
			player.playerEvents.OnThrow.AddListener(() => events.OnThrow.Invoke());
			player.playerEvents.OnStompStarted.AddListener(() => events.OnStompStarted.Invoke());
			player.playerEvents.OnStompFalling.AddListener(() => events.OnStompFalling.Invoke());
			player.playerEvents.OnStompLanding.AddListener(() => events.OnStompLanding.Invoke());
			player.playerEvents.OnStompEnding.AddListener(() => events.OnStompEnding.Invoke());
			player.playerEvents.OnLedgeGrabbed.AddListener(() => events.OnLedgeGrabbed.Invoke());
			player.playerEvents.OnLedgeClimbing.AddListener(() => events.OnLedgeClimbing.Invoke());
			player.playerEvents.OnAirDive.AddListener(() => events.OnAirDive.Invoke());
			player.playerEvents.OnBackflip.AddListener(() => events.OnBackflip.Invoke());
		}

		protected virtual void Start()
		{
			InitializePlayer();
			InitializeCallbacks();
		}
	}
}
