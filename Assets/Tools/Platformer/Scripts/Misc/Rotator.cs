using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
	[AddComponentMenu("PLAYER TWO/Platformer Project/Misc/Rotator")]
	public class Rotator : MonoBehaviour
	{
		public Space space = Space.Self;
		public Vector3 eulers = new Vector3(0, -180, 0);

		protected virtual void LateUpdate()
		{
			transform.Rotate(eulers * Time.deltaTime, space);
		}
	}
}
