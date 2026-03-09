using UnityEngine;
using MiniGame;

namespace MiniGame
{
    public class CameraFOV : MonoBehaviour
    {
        public float maxFovChangeFactor = 1.2f;

        private GameObject _airplane;
        private Camera _mainCamera;
        private Rigidbody _airplaneRigidBody;
        private float _baseFov;
        private float _maxFovIncrease;
        private float _maxSpeedSqr;

        void Start()
        {
            // Find the user-controlled airplane.
            var userControl = GameObject.FindFirstObjectByType<AircraftInputController>();
            if (userControl == null)
            {
                Debug.LogError("FLIGHT KIT StartLevelSequence: an AeroplaneUserControl component is missing in the scene");
	            return;
            }
            _airplane = userControl.gameObject;

            _mainCamera = Camera.main;
            _baseFov = _mainCamera.fieldOfView;
            _maxFovIncrease = _baseFov * (maxFovChangeFactor - 1);

            _airplaneRigidBody = _airplane.GetComponent<Rigidbody>();
            _maxSpeedSqr = _airplane.GetComponent<FlightDynamicsController>().MaxSpeed;
            _maxSpeedSqr *= _maxSpeedSqr;
        }

        void FixedUpdate()
        {
            _mainCamera.fieldOfView = _baseFov + (_airplaneRigidBody.linearVelocity.sqrMagnitude / _maxSpeedSqr) * _maxFovIncrease;
        }
    }
}