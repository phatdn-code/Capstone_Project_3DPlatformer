using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Player/States/Spin Player State")]
	public class SpinPlayerState : PlayerState
	{
		protected override void OnEnter(Player player)
		{
            // Nếu spin trên không => tạo lực đẩy lên
            if (!player.isGrounded)
            {
                player.verticalVelocity = Vector3.up * player.stats.current.airSpinUpwardForce;
            }

            // Gọi sự kiện Spin (âm thanh, hiệu ứng)
            player.playerEvents.OnSpin.Invoke();

            // 🌀 Kiểm tra bomb gần player trong bán kính 2m
            Collider[] hits = Physics.OverlapSphere(player.transform.position, 2f);

            foreach (var hit in hits)
            {
                if (hit.CompareTag("Bomb"))
                {
                    var bomb = hit.GetComponent<BossBomb>();
                    if (bomb != null)
                    {
                        bomb.SendMessage("OnPlayerHitBomb", SendMessageOptions.DontRequireReceiver);
                        Debug.Log("💥 Player đánh trúng bomb! Bomb bị phản ngược về boss!");
                    }
                }
            }
        }

		protected override void OnExit(Player player) { }

		protected override void OnStep(Player player)
		{
			player.Gravity();
			player.SnapToGround();
			player.AirDive();
			player.StompAttack();
			player.AccelerateToInputDirection();

			if (timeSinceEntered >= player.stats.current.spinDuration)
			{
				if (player.isGrounded)
				{
					player.states.Change<IdlePlayerState>();
				}
				else
				{
					player.states.Change<FallPlayerState>();
				}
			}
		}

		public override void OnContact(Player player, Collider other) { }
	}
}
