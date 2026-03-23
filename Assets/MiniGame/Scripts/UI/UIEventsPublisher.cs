using PLAYERTWO.PlatformerProject;
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
            Game.LockCursor(true);
            if (OnPlayEvent != null)
            {
                OnPlayEvent();
            }
        }

    }
}