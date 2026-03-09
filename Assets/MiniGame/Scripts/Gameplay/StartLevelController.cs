using UnityEngine;
using System.Collections;
using UnityStandardAssets.Cameras;

namespace MiniGame {
    /// <summary>
    /// Controls the sequence of events that happen once the user hits Play button (hiding menus, turning on/off
    /// components, etc).
    /// </summary>
    public class StartLevelController : MonoBehaviour {
        /// <summary>
        /// If we should skip the main menu and start the game right away (useful during development).
        /// </summary>
        public bool playOnStart = false;

        /// <summary>
        /// If pickup spheres should start growing when the gameplay starts.
        /// </summary>
        public bool enablePickupGrowingOnStart = true;

        /// <summary>
        /// When the gameplay starts, the camera's container (Pivot GameObject) will ease into this position.
        /// </summary>
        [Space] public Vector3 cameraPivotFinalPosition = new Vector3(0f, 10f, -15f);

        public bool levelStarted { get; private set; }

        /// <summary>
        /// The user-controlled airplane.
        /// </summary>
        private GameObject _airplane;

        private AutoCam _autoCam;
        private float _turnSpeed;
        private GameObject _pivot;
        private Vector3 _initPivotPos;

        private bool _isTweeningIn = false;
        private float _tweenInProgress;
        private float _tweenInStartTime;

        void Start() {
            // Find the Aeroplane's camera.
            _autoCam = FindFirstObjectByType<AutoCam>();
            _pivot = GameObject.Find("Pivot");

            // Find the user-controlled airplane.
            var userControl = FindFirstObjectByType<AircraftInputController>();
            if (userControl == null) {
                Debug.LogWarning(
                    "FLIGHT KIT StartLevelSequence: an AeroplaneUserControl component is missing in the scene");
                enabled = false;
                return;
            }

            _airplane = userControl.gameObject;

            // If there is no AutoCam, we're not on a level and need to turn off this component.
            if (_autoCam == null) {
                Debug.LogWarning("Can't find AutoCam component in the scene.");
                enabled = false;
                return;
            }

            // Turn off camera turning until start of the gameplay.
            _turnSpeed = _autoCam.TurnSpeed;
            _autoCam.TurnSpeed = 0;

            // Skip menu (for debug).
            if (playOnStart) {
                StartLevel();

                // Publish play event from code since it will not be published by Play button.
                var uiEvents = FindFirstObjectByType<UIEventsPublisher>();
                if (uiEvents != null) {
                    uiEvents.PublishPlay();
                }
            }
            else {
                UIEventsPublisher.OnPlayEvent += StartLevel;
            }
        }

        void OnDeactivate() {
            UIEventsPublisher.OnPlayEvent -= StartLevel;
        }

        void Update() {
            if (_isTweeningIn) {
                // Change values with smoothing.
                _tweenInProgress = Mathf.SmoothStep(0f, 1f, (Time.time - _tweenInStartTime) * 0.14f);
                _autoCam.TurnSpeed = _tweenInProgress * _turnSpeed;
                _pivot.transform.localPosition =
                    Vector3.Lerp(_initPivotPos, cameraPivotFinalPosition, _tweenInProgress);

                // If tweening finished.
                if (_tweenInProgress > 0.99f) {
                    _isTweeningIn = false;

                    _autoCam.TurnSpeed = _turnSpeed;
                    _pivot.transform.localPosition = cameraPivotFinalPosition;
                }
            }
        }

        virtual public void StartLevel() {
            if (this != null) {
                StartCoroutine(StartLevelCoroutine());
            }
        }

        private IEnumerator StartLevelCoroutine() {
            // Register level started.
            levelStarted = true;

            // Enable pause.
            var pause = FindFirstObjectByType<PauseController>();
            if (pause) pause.enabled = true;

            yield return new WaitForSeconds(playOnStart ? 0 : 3);

            // Turn off menu for performance if the menu exists.
            var gss = FindFirstObjectByType<MenuFadeInController>();
            if (gss != null) {
                GameObject mainMenu = gss.mainMenu;
                if (mainMenu != null) {
                    mainMenu.SetActive(false);
                }
            }

            // Camera turning fade in, camera pivot position tween.
            _initPivotPos = _pivot.transform.localPosition;
            _isTweeningIn = true;
            _tweenInStartTime = Time.time;
            _autoCam.enabled = true;

            yield return new WaitForSeconds(playOnStart ? 0 : 2);

            // Turn on controls.
            MonoBehaviour userControl = _airplane.GetComponent<AircraftInputController>();
            if (userControl == null) {
                Debug.LogError("Can't find AircraftInputController component on the airplane.");
                yield break;
            }

            userControl.enabled = true;

            yield return new WaitForSeconds(playOnStart ? 0 : 2);

            // Enable growing of pickup spheres.
            if (enablePickupGrowingOnStart) {
                CollectableStar.growingEnabled = true;
            }
        }
    }
}