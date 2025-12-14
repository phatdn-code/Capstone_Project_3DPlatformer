using Unity.Cinemachine;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// WaterCannon: điều khiển xoay + bắn (Projectile/Beam) khi player nhập chế độ cannon.
    /// </summary>
    [RequireComponent(typeof(CannonInput))]
    public class WaterCannon : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR: REFERENCES ===

        [Header("Projectile")]
        [SerializeField] private WaterProjectile projectilePrefab;

        [Header("Beam")]
        [SerializeField] private BeamVfx beamVfx;

        [Header("Muzzle VFX")]
        [SerializeField] private ParticleSystem muzzleCastEffect;

        [Header("Rotation Pivots")]
        [SerializeField] private Transform yawPivot;
        [SerializeField] private Transform pitchPivot;

        [Header("Cinemachine Camera")]
        [Tooltip("VirtualCamera dùng khi điều khiển cannon.")]
        [SerializeField] private CinemachineVirtualCameraBase cannonCamera;
        [SerializeField] private int controllingPriority = 100;
        [SerializeField] private int idlePriority = 0;

        [Header("Tutorial (Trigger UI)")]
        [SerializeField] private GameObject tutorialArrow;
        [SerializeField] private GameObject tutorialUI;
        [SerializeField] private bool arrowVisibleByDefault = true;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: SETTINGS ===

        [Header("Fire Settings")]
        [SerializeField] private float fireCooldown = 1.0f;

        [Header("Rotation Settings")]
        [SerializeField] private float yawSpeed = 90f;
        [SerializeField] private float pitchSpeed = 60f;
        [SerializeField] private float minPitch = -5f;
        [SerializeField] private float maxPitch = 45f;
        [SerializeField] private bool invertY = true;

        [Header("Yaw Clamp")]
        [SerializeField] private bool clampYaw = true;
        [SerializeField] private float minYaw = -120f;
        [SerializeField] private float maxYaw = 120f;

        [Header("Fire Mode")]
        [SerializeField] private CannonFireMode fireMode = CannonFireMode.Projectile;

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private PlayerHub _playerHub;
        private CannonInput _cannonInput;

        private Transform _muzzleTransform;
        private float _lastFireTime;

        private float _currentYaw;
        private float _currentPitch;

        private bool _isPlayerInTrigger;
        private bool _isControllingCannon;

        private bool _isBeamFiring;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Khởi tạo: cache, setup pivot/muzzle, UI, camera.</summary>
        private void Start()
        {
            CacheReferences();
            InitTutorialVisual();
            CacheInitialAngles();
            SetCannonCameraPriority(idlePriority);
        }

        /// <summary>Loop: toggle control, rồi xoay + bắn khi đang điều khiển.</summary>
        private void Update()
        {
            HandleControlToggleInput();

            if (!_isControllingCannon)
                return;

            HandleRotationInput();
            HandleFireByMode();
        }

        /// <summary>Tắt object: đảm bảo trả quyền điều khiển + tắt beam + reset camera.</summary>
        private void OnDisable()
        {
            if (_isControllingCannon && _playerHub != null)
                _playerHub.SetPlayerControlAndModel(false);

            _isControllingCannon = false;
            StopBeamIfNeeded();
            SetCannonCameraPriority(idlePriority);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === INIT / CACHE ===

        /// <summary>Cache input/playerhub + setup pivot/muzzle.</summary>
        private void CacheReferences()
        {
            _cannonInput = GetComponent<CannonInput>();
            if (_cannonInput == null)
                Debug.LogError("WaterCannon: Không tìm thấy CannonInput trên cùng GameObject.");

            _playerHub = PlayerHub.Instance;
            if (_playerHub == null)
                Debug.LogWarning("WaterCannon: Không tìm thấy PlayerHub trong scene.");

            yawPivot ??= transform;
            pitchPivot ??= transform;

            _muzzleTransform = muzzleCastEffect != null ? muzzleCastEffect.transform : pitchPivot;
        }

        /// <summary>Lưu góc ban đầu để clamp đúng.</summary>
        private void CacheInitialAngles()
        {
            _currentYaw = NormalizeAngle(yawPivot.localEulerAngles.y);
            _currentPitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        }

        /// <summary>Set UI/arrow trạng thái ban đầu.</summary>
        private void InitTutorialVisual()
        {
            _isPlayerInTrigger = false;
            _isControllingCannon = false;
            _isBeamFiring = false;

            if (tutorialUI != null) tutorialUI.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
        }

        /// <summary>Đổi priority camera cannon.</summary>
        private void SetCannonCameraPriority(int priority)
        {
            if (cannonCamera == null) return;
            cannonCamera.Priority = priority;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === TRIGGER UI ===

        /// <summary>Vào vùng: hiện UI hướng dẫn, tắt arrow.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _isPlayerInTrigger = true;
            if (tutorialUI != null) tutorialUI.SetActive(true);
            if (tutorialArrow != null) tutorialArrow.SetActive(false);
        }

        /// <summary>Ra vùng: tắt UI, bật arrow (nếu không điều khiển).</summary>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _isPlayerInTrigger = false;

            if (_isControllingCannon)
                return;

            if (tutorialUI != null) tutorialUI.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === CONTROL TOGGLE ===

        /// <summary>Nhấn Interact để vào/thoát điều khiển cannon.</summary>
        private void HandleControlToggleInput()
        {
            if (_cannonInput == null) return;
            if (!_cannonInput.GetInteractDown()) return;

            if (_isControllingCannon)
            {
                ExitCannonControl();
                return;
            }

            if (_isPlayerInTrigger)
                EnterCannonControl();
        }

        /// <summary>Vào điều khiển: khóa player + ưu tiên camera cannon.</summary>
        private void EnterCannonControl()
        {
            if (_playerHub != null)
                _playerHub.SetPlayerControlAndModel(true);

            _isControllingCannon = true;
            SetCannonCameraPriority(controllingPriority);
        }

        /// <summary>Thoát điều khiển: mở khóa player + reset UI/camera.</summary>
        private void ExitCannonControl()
        {
            if (_playerHub != null)
                _playerHub.SetPlayerControlAndModel(false);

            _isControllingCannon = false;
            StopBeamIfNeeded();
            SetCannonCameraPriority(idlePriority);

            if (!_isPlayerInTrigger)
            {
                if (tutorialUI != null) tutorialUI.SetActive(false);
                if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
            }
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === FIRE ===

        /// <summary>Bắn theo mode: Projectile hoặc Beam.</summary>
        private void HandleFireByMode()
        {
            if (_cannonInput == null) return;

            switch (fireMode)
            {
                case CannonFireMode.Projectile:
                    HandleProjectileFire();
                    break;

                case CannonFireMode.Beam:
                    HandleBeamFire();
                    break;
            }
        }

        /// <summary>Projectile: bấm 1 phát bắn 1 viên.</summary>
        private void HandleProjectileFire()
        {
            if (_cannonInput.GetFireDown())
                FireProjectile();
        }

        /// <summary>Beam: giữ để bắn, nhả để tắt.</summary>
        private void HandleBeamFire()
        {
            if (_cannonInput.GetFireDown())
            {
                _isBeamFiring = true;
                PlayMuzzleEffect();

                if (beamVfx != null)
                    beamVfx.StartBeam();
            }

            if (_isBeamFiring && _cannonInput.GetFireHeld())
            {
                if (beamVfx != null)
                    beamVfx.UpdateBeam();
            }

            if (_isBeamFiring && _cannonInput.GetFireUp())
                StopBeamIfNeeded();
        }

        /// <summary>Bắn đạn có cooldown.</summary>
        public void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("WaterCannon: Chưa gán projectilePrefab.");
                return;
            }

            if (Time.time < _lastFireTime + fireCooldown)
                return;

            _lastFireTime = Time.time;

            Vector3 muzzlePos = _muzzleTransform.position;
            Vector3 muzzleDir = _muzzleTransform.forward;

            PlayMuzzleEffect();

            var projectile = Instantiate(projectilePrefab, muzzlePos, Quaternion.identity);
            projectile.LaunchForward(muzzlePos, muzzleDir);
        }

        /// <summary>Tắt beam nếu đang bắn.</summary>
        private void StopBeamIfNeeded()
        {
            if (!_isBeamFiring)
                return;

            _isBeamFiring = false;

            if (beamVfx != null)
                beamVfx.StopAll();
        }

        /// <summary>Play VFX miệng súng.</summary>
        private void PlayMuzzleEffect()
        {
            if (muzzleCastEffect == null) return;

            muzzleCastEffect.gameObject.SetActive(true);
            muzzleCastEffect.transform.position = _muzzleTransform.position;
            muzzleCastEffect.transform.rotation = _muzzleTransform.rotation;

            muzzleCastEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleCastEffect.Play();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === ROTATION ===

        /// <summary>Đọc Look để xoay yaw/pitch (có clamp).</summary>
        private void HandleRotationInput()
        {
            if (_cannonInput == null || yawPivot == null || pitchPivot == null)
                return;

            Vector2 look = _cannonInput.GetLookAxisRaw();
            if (look.sqrMagnitude < 0.0001f)
                return;

            float dt = Time.deltaTime;

            _currentYaw += look.x * yawSpeed * dt;

            float pitchDelta = -look.y * pitchSpeed * dt;
            if (invertY) pitchDelta = -pitchDelta;

            _currentPitch += pitchDelta;
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            if (clampYaw)
                _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);

            yawPivot.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }

        /// <summary>Chuẩn hoá góc về [-180, 180].</summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === GIZMOS ===

        /// <summary>Vẽ gizmo hướng miệng súng.</summary>
        private void OnDrawGizmosSelected()
        {
            Transform gizmoMuzzle =
                muzzleCastEffect != null ? muzzleCastEffect.transform :
                (pitchPivot != null ? pitchPivot : transform);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(gizmoMuzzle.position, 0.1f);
            Gizmos.DrawLine(transform.position, gizmoMuzzle.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(gizmoMuzzle.position, gizmoMuzzle.forward * 3f);
        }

        #endregion
    }
}
