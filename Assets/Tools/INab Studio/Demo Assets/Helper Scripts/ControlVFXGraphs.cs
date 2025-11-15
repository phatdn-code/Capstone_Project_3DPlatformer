using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using INab.Common;

namespace INab.Demo
{
    [ExecuteAlways]
    public class ControlVFXGraphs : MonoBehaviour
    {
        public VisualEffectAsset effectToReplace;
        public List<InteractiveEffect> effects = new List<InteractiveEffect>();

        public bool automaticPlayOnStart = false;
        public bool automaticPlayLoopOnStart = false;

        private void Start()
        {
            if(automaticPlayOnStart)
            {
                PlayEffects();
            }

            if (automaticPlayLoopOnStart)
            {
                StartCoroutines();
            }

            foreach (InteractiveEffect effect in effects)
            {
                if(effect == null)
                {
                    effects.Remove(effect);
                }
            }
        }

        private void OnValidate()
        {
            if (effectToReplace != null)
            {
                foreach (InteractiveEffect effect in effects)
                {
                    effect.visualEffect.visualEffectAsset = effectToReplace;
                }
            }

        }

        public void StartCoroutines()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.EditorCoroutine = effect.StartCoroutine(effect.AutoEffectCoroutine());
            }
        }

        public void StopCoroutines()
        {
            foreach (InteractiveEffect ourTarget in effects)
            {
                if (ourTarget.EditorCoroutine != null)
                {
                    ourTarget.StopCoroutine(ourTarget.EditorCoroutine);
                    ourTarget.StopAllCoroutines();
                }
                ourTarget.EditorCoroutine = null;
            }
        }

        public void SendPlayEvents()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.visualEffect.Play();
            }
        }

        public void SendStopEvents() 
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.visualEffect.Stop();
            }
        }

        public void PlayEffects()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.PlayEffect();
            }
        }

        public void ReverseEffects()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.ReverseEffect(1);
            }
        }

        public void GetRendererMaterials()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.GetRendererMaterials();
            }
        }

        public void RefreshEffects()
        {
            foreach (InteractiveEffect effect in effects)
            {
                effect.UpdateAndSetupEffect();
            }
        }
    }
}