using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

namespace INab.Demo
{
    public class ControlShowcase : MonoBehaviour
    {
        private int currentIndex = 0;
        public List<GameObject> effects;
        public int startingIndex = 0;

        public TextMeshProUGUI textMeshProUGUI;

        public bool automatic = false; 
        public float animationDuration = 4;
        [Range(-1,1)]
        public float offset = 0.2f;
        public int numberOfEffects = 30;

        [Space]
        public int automaticIndex = 0;
        public float time = 0;
        public float maxDuration = 0;
        public float motionTime = 0;
        public Animator currentAnimator;
        public bool updateAllNames = false;
        public bool customVideoShowcase = false;


        public void Start()
        {
            if (!customVideoShowcase)
            {
                foreach (GameObject effect in effects)
                {
                    effect.SetActive(false);
                }
            }

           
            currentIndex = startingIndex;

            // Activate the first effect
            if (effects.Count > 0)
            {
                ActivateEffect(currentIndex, currentIndex);
            }

            if (automatic)
            {
                PlayEffect();
                CalculateDuration(automaticIndex);
            }
        }

        void IncreaseAutomaticIndex()
        {
            automaticIndex++;
            if (automaticIndex >= effects.Count)
            {
                automaticIndex = 0;
            }
        }

        void CalculateDuration(int index)
        {
            maxDuration = 0; // Reset maxDuration before calculating
            var nextEffect = effects[index].GetComponent<ControlVFXGraphs>();
            foreach (var effect in nextEffect.effects)
            {
                maxDuration = Mathf.Max(maxDuration, effect.duration);
            }
            maxDuration = maxDuration + offset * maxDuration;
        }

        void PlayEffect()
        {
            GetControlVFXGraphs().RefreshEffects();
            GetControlVFXGraphs().PlayEffects();
        }

        void ReverseEffect()
        {
            GetControlVFXGraphs().RefreshEffects();
            GetControlVFXGraphs().ReverseEffects();
        }

        void Update()
        {
            motionTime += Time.deltaTime / animationDuration ;
            if(!customVideoShowcase) currentAnimator.SetFloat("MotionTime", motionTime);

            if (automatic)
            {
                time += Time.deltaTime;
                if (time > maxDuration)
                {
                    IncreaseAutomaticIndex();
                    CalculateDuration(automaticIndex);
                    ActivateNextEffect();
                    PlayEffect();
                    time = 0;
                }
            }

            if(customVideoShowcase)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1))
                {
                    PlayEffect();

                }
                if (Input.GetKeyDown(KeyCode.Alpha2))
                {
                    ActivateNextEffect();
                    PlayEffect();
                }
            }
            else
            {
                // Check for 'D' key press to activate the next effect
                if (Input.GetKeyDown(KeyCode.D))
                {
                    ActivateNextEffect();
                }
                // Check for 'A' key press to activate the previous effect
                if (Input.GetKeyDown(KeyCode.A))
                {
                    ActivatePreviousEffect();
                }

                if (Input.GetKeyDown(KeyCode.P))
                {
                    PlayEffect();
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    ReverseEffect();
                }
            }
        }


        private void OnValidate()
        {
            if(updateAllNames) UpdateAllNames();
        }

        public void UpdateAllNames()
        {
            foreach (GameObject effect in effects)
            {
                string newText = effect.GetComponent<ControlVFXGraphs>().effectToReplace.name;
                int lastSpaceIndex = effect.name.LastIndexOf(' ');
                newText = newText.Substring(0, lastSpaceIndex);
                effect.name = newText;
            }
        }

        void ActivateNextEffect()
        {
            int lastIndex = currentIndex;

            // Move to the next effect
            currentIndex++;
            if (currentIndex >= effects.Count)
            {
                currentIndex = 0; // Wrap around to the first effect
            }

            // Activate the new current effect
            if (currentIndex >= 0 && currentIndex < effects.Count)
            {
                ActivateEffect(currentIndex, lastIndex);
            }
        }

        void ActivatePreviousEffect()
        {
            int lastIndex = currentIndex;

            // Move to the previous effect
            currentIndex--;
            if (currentIndex < 0)
            {
                currentIndex = effects.Count - 1; // Wrap around to the last effect
            }

            // Activate the new current effect
            if (currentIndex >= 0 && currentIndex < effects.Count)
            {
                ActivateEffect(currentIndex, lastIndex);
            }
        }

        private ControlVFXGraphs GetControlVFXGraphs()
        {
            return effects[currentIndex].GetComponent<ControlVFXGraphs>();
        }

        private void UpdateUiEffectName()
        {
            string newText = (currentIndex + 1).ToString() + "/"+ numberOfEffects +" "+ GetControlVFXGraphs().gameObject.name;
            //if(currentIndex > 0)
            //{
            //    int lastSpaceIndex = newText.LastIndexOf(' ');
            //    newText = newText.Substring(0, lastSpaceIndex);
            //}

            textMeshProUGUI.text = newText;
        }

        private void ActivateEffect(int newIndex, int lastIndex)
        {
            if(!customVideoShowcase)
            {
                effects[lastIndex].SetActive(false);
                effects[newIndex].SetActive(true);
            }
           
            effects[newIndex].GetComponent<ControlVFXGraphs>().RefreshEffects();
            UpdateUiEffectName();
            if (customVideoShowcase)
                return;

            currentAnimator = effects[newIndex].GetComponent<ControlVFXGraphs>().effects[1].GetComponentInParent<Animator>();
            currentAnimator.SetFloat("MotionTime", motionTime);
        }
    }
}
