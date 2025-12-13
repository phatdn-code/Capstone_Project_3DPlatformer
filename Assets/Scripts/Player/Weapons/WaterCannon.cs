using UnityEngine;
using PLAYERTWO.PlatformerProject;   // PlayerHub, PlayerInputManager

namespace PixPlays.ElementalVFX
{
    /// <summary>
    /// Khẩu cannon bắn đạn nước:
    /// - Đạn bay theo hướng nòng (_muzzleTransform.forward) rồi cong xuống vì gravity.
    /// - Nòng xoay bằng input Look (PlayerInputManager), có giới hạn pitch / yaw.
    /// - Chỉ bắn khi nhấn nút fire (tạm dùng Spin trong PlayerInputManager).
    /// </summary>
    public class WaterCannon : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === INSPECTOR FIELDS ===

        [Header("References")]
        [SerializeField] private WaterProjectile projectilePrefab;   // Prefab đạn nước
        private PlayerInputManager playerInput; // Input của player

        [Header("Tutorial (Trigger UI)")]
        [SerializeField] private GameObject tutorialArrow;           // Arrow hint (outside trigger)
        [SerializeField] private GameObject tutorialUI;              // Tutorial UI (inside trigger)
        [SerializeField] private bool arrowVisibleByDefault = true;  // Start state

        [Header("Muzzle VFX")]
        [SerializeField] private ParticleSystem muzzleCastEffect;   // Hiệu ứng ngay miệng súng

        [Header("Fire Settings")]
        [SerializeField] private float fireCooldown = 1.0f;         // Thời gian giữa 2 lần bắn

        [Header("Rotation Pivots")]
        [SerializeField] private Transform yawPivot;                // Pivot xoay ngang (Y) – thân turret
        [SerializeField] private Transform pitchPivot;              // Pivot xoay dọc (X) – nòng súng

        [Header("Rotation Settings")]
        [SerializeField] private float yawSpeed = 90f;              // Tốc độ xoay ngang (độ/giây)
        [SerializeField] private float pitchSpeed = 60f;            // Tốc độ xoay dọc (độ/giây)
        [SerializeField] private float minPitch = -5f;              // Giới hạn cúi xuống
        [SerializeField] private float maxPitch = 45f;              // Giới hạn ngẩng lên
        [SerializeField] private bool invertY = true;               // Đảo trục dọc nếu cần

        [Header("Yaw Clamp (Giới hạn xoay ngang)")]
        [SerializeField] private bool clampYaw = true;              // Bật/tắt giới hạn yaw
        [SerializeField] private float minYaw = -120f;              // Giới hạn quay trái
        [SerializeField] private float maxYaw = 120f;               // Giới hạn quay phải

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private Transform _muzzleTransform; // Transform miệng súng thực tế
        private float _lastFireTime;        // Thời điểm bắn gần nhất

        private float _currentYaw;          // Góc yaw hiện tại (độ, local)
        private float _currentPitch;        // Góc pitch hiện tại (độ, local)

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Khởi tạo input, pivot/muzzle và cache góc xoay ban đầu.</summary>
        private void Start()
        {
            SetupInputReference();
            SetupPivotsAndMuzzle();
            CacheInitialAngles();

            // Default tutorial state
            SetTutorialState(isInTrigger: false);
            if (!arrowVisibleByDefault)
            {
                // If you want UI hidden & arrow hidden by default
                if (tutorialArrow != null) tutorialArrow.SetActive(false);
                if (tutorialUI != null) tutorialUI.SetActive(false);
            }
        }

        /// <summary>Mỗi frame: xoay nòng theo input + xử lý input bắn.</summary>
        private void Update()
        {
            HandleRotationInput();
            HandleFireInput();
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === INIT HELPERS ===

        /// <summary>Lấy PlayerInputManager từ PlayerHub nếu chưa gán tay.</summary>
        private void SetupInputReference()
        {
            if (playerInput == null && PlayerHub.Instance != null)
                playerInput = PlayerHub.Instance.InputManager;
        }

        /// <summary>Đảm bảo có yawPivot, pitchPivot và xác định _muzzleTransform.</summary>
        private void SetupPivotsAndMuzzle()
        {
            // Fallback pivot nếu chưa gán
            yawPivot ??= transform;
            pitchPivot ??= transform;

            // Muzzle = transform VFX, nếu không có thì dùng pitchPivot
            _muzzleTransform = muzzleCastEffect != null
                ? muzzleCastEffect.transform
                : pitchPivot;
        }

        /// <summary>Cache góc local ban đầu làm gốc clamp yaw/pitch.</summary>
        private void CacheInitialAngles()
        {
            _currentYaw = NormalizeAngle(yawPivot.localEulerAngles.y);
            _currentPitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === TUTORIAL TRIGGER UI ===

        /// <summary>Show UI & hide arrow when player enters trigger.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            SetTutorialState(isInTrigger: true);
        }

        /// <summary>Hide UI & show arrow when player exits trigger.</summary>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            SetTutorialState(isInTrigger: false);
        }

        /// <summary>Toggle tutorial arrow/UI.</summary>
        private void SetTutorialState(bool isInTrigger)
        {
            if (tutorialUI != null)
                tutorialUI.SetActive(isInTrigger);

            if (tutorialArrow != null)
                tutorialArrow.SetActive(!isInTrigger && arrowVisibleByDefault);
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === FIRE LOGIC ===

        /// <summary>Đọc input fire từ PlayerInputManager rồi gọi Fire().</summary>
        private void HandleFireInput()
        {
            if (playerInput == null)
                return;

            // TẠM THỜI: dùng nút Spin làm fire (hãy map Spin = Left Mouse trong Input Actions)
            if (!playerInput.GetSpinDown())
                return;

            Fire();
        }

        /// <summary>Bắn 1 viên theo _muzzleTransform.forward, có cooldown.</summary>
        public void Fire()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("WaterCannon: Chưa gán projectilePrefab.");
                return;
            }

            // Check cooldown
            if (Time.time < _lastFireTime + fireCooldown)
                return;

            _lastFireTime = Time.time;

            Vector3 muzzlePos = _muzzleTransform.position;
            Vector3 muzzleDir = _muzzleTransform.forward;

            PlayMuzzleEffect();

            var projectile = Instantiate(
                projectilePrefab,
                muzzlePos,
                Quaternion.identity
            );

            projectile.LaunchForward(muzzlePos, muzzleDir);
        }

        /// <summary>Bật VFX ở miệng súng mỗi lần bắn.</summary>
        private void PlayMuzzleEffect()
        {
            if (muzzleCastEffect == null)
                return;

            muzzleCastEffect.gameObject.SetActive(true);
            muzzleCastEffect.transform.position = _muzzleTransform.position;
            muzzleCastEffect.transform.rotation = _muzzleTransform.rotation;

            muzzleCastEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleCastEffect.Play();
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === ROTATION LOGIC ===

        /// <summary>Đọc input Look từ PlayerInputManager rồi xoay yaw/pitch (có clamp).</summary>
        private void HandleRotationInput()
        {
            if (playerInput == null || yawPivot == null || pitchPivot == null)
                return;

            Vector2 look = playerInput.GetLookAxisRaw();
            if (look.sqrMagnitude < 0.0001f)
                return;

            float dt = Time.deltaTime;

            // Xoay ngang (yaw)
            _currentYaw += look.x * yawSpeed * dt;

            // Xoay dọc (pitch)
            float invert = invertY ? -1f : 1f;
            _currentPitch += look.y * pitchSpeed * dt * invert;

            // Clamp pitch
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // Clamp yaw nếu bật
            if (clampYaw)
                _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);

            // Apply ra pivot
            yawPivot.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }

        /// <summary>Chuẩn hóa góc về [-180, 180] cho dễ clamp.</summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        #endregion
        //────────────────────────────────────────────────────


        //────────────────────────────────────────────────────
        #region === GIZMOS DEBUG ===

        /// <summary>Vẽ gizmos hiển thị vị trí + hướng bắn của miệng súng.</summary>
        private void OnDrawGizmosSelected()
        {
            Transform gizmoMuzzle = muzzleCastEffect != null
                ? muzzleCastEffect.transform
                : (pitchPivot != null ? pitchPivot : transform);

            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(gizmoMuzzle.position, 0.1f);
            Gizmos.DrawLine(transform.position, gizmoMuzzle.position);

            Gizmos.color = Color.blue;
            Gizmos.DrawRay(gizmoMuzzle.position, gizmoMuzzle.forward * 3f);
        }

        #endregion
        //────────────────────────────────────────────────────
    }
}
