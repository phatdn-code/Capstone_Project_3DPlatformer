using UnityEngine;

namespace MiniGame
{
	/// <summary>
	/// Publishes OnPlayEvent when user starts playing a level.
	/// </summary>
	public class UIEventsPublisher : MonoBehaviour {
        public static event GameEventActions.SimpleAction OnPlayEvent;

        public virtual void PublishPlay()
        {
            if (OnPlayEvent != null)
            {
                OnPlayEvent();
            }
        }

    }
}