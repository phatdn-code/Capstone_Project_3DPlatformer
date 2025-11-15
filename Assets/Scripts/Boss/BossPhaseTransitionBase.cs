using UnityEngine;
using System.Collections;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Base class for handling boss phase transition behaviors.
    /// Each boss can attach a different transition type.
    /// </summary>
    public abstract class BossPhaseTransitionBase : MonoBehaviour
    {
        public abstract IEnumerator ExecuteTransition(int nextPhase);
    }
}
