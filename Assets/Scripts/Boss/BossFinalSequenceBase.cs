using PLAYERTWO.PlatformerProject;
using System.Collections;
using UnityEngine;

public abstract class BossFinalSequenceBase : MonoBehaviour
{
    private bool isRunning;

    public void RunSequence(BossLinker linker)
    {
        if (isRunning) return;
        isRunning = true;
        linker.StartCoroutine(ExecuteFinalSequence(linker));
    }

    public abstract IEnumerator ExecuteFinalSequence(BossLinker linker);
}
