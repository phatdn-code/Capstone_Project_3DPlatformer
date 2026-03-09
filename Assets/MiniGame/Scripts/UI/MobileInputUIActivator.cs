using UnityEngine;

namespace MiniGame
{
	/// <summary>
	/// Listens to mobile controls scheme change events and turns on/off corresponding UI elements.
	/// </summary>
    public class MobileInputUIActivator : MonoBehaviour
    {
        public GameObject[] tiltUIElements;
        public GameObject[] touchUIElements;
        
        private bool _isTiltUiMode;

        void Start()
        {
            ControlSettingsManager.OnTiltEnabledEvent += HandleTiltEnabled;
            ControlSettingsManager.OnTiltDisabledEvent += HandleTiltDisabled;
            
            // Initial state check
            if (ControlSettingsManager.IsTiltEnabled)
            {
                HandleTiltEnabled();
            }
            else
            {
                HandleTiltDisabled();
            }
            
            // We want to activate UI no sooner than gameplay starts.
            UIEventsPublisher.OnPlayEvent += UpdateUI;
        }

        void OnDisable()
        {
            ControlSettingsManager.OnTiltEnabledEvent -= HandleTiltEnabled;
            ControlSettingsManager.OnTiltDisabledEvent -= HandleTiltDisabled;
            
            UIEventsPublisher.OnPlayEvent -= UpdateUI;
        }

        private void HandleTiltEnabled()
        {
            _isTiltUiMode = true;
            UpdateUI();
        }

        private void HandleTiltDisabled()
        {
            _isTiltUiMode = false;
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (_isTiltUiMode)
            {
                foreach (var go in touchUIElements)
                {
                    go.SetActive(false);
                }
                foreach (var go in tiltUIElements)
                {
                    go.SetActive(true);
                }
            }
            else
            {     
                foreach (var go in tiltUIElements)
                {
                    go.SetActive(false);
                }

                foreach (var go in touchUIElements)
                {
                    go.SetActive(true);
                }
            }
        }
    }
}

