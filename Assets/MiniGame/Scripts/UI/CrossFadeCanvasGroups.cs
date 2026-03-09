using UnityEngine;

namespace MiniGame
{
	public class CrossFadeCanvasGroups : MonoBehaviour
	{
		public CanvasGroup fromGroup;
		public CanvasGroup toGroup;

		public float speed = 1f;

		public virtual void Activate()
		{
			toGroup.gameObject.SetActive(true);
			toGroup.alpha = 0f;
			toGroup.interactable = true;

			fromGroup.interactable = false;

			StartCoroutine(Fader.FadeAlpha(fromGroup, false, speed, () => { fromGroup.gameObject.SetActive(false); }));
			StartCoroutine(Fader.FadeAlpha(toGroup, true, speed));
		}
	}
}