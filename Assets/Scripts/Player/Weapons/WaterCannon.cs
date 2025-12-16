using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// WaterCannon: điều khiển xoay + bắn (Projectile/Beam) khi player nhập chế độ cannon.
    /// Có hệ thống Energy + UI Slider + cơ chế LOCK khi cạn năng lượng.
    /// Có đổi màu fill theo mức năng lượng (DOTween).
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

        [Header("Energy UI")]
        [SerializeField] private Slider energySlider;
        [SerializeField] private bool showEnergyOnlyWhenControlling = true;

        [Header("Energy Fill Color")]
        [SerializeField] private Image energyFillImage;          // Image dùng để đổi màu fill
        [SerializeField] private float colorTweenDuration = 0.25f;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: SETTINGS ===

        [Header("Fire Settings")]
        [SerializeField] private CannonFireMode fireMode = CannonFireMode.Projectile;
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

        [Header("Energy Settings")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float energyRegenPerSecond = 15f;       // hồi bình thường
        [SerializeField] private float depletedRegenPerSecond = 10f;     // hồi chậm khi LOCK (cạn)
        [SerializeField] private float projectileEnergyCost = 20f;       // tốn mỗi viên
        [SerializeField] private float beamEnergyCostPerSecond = 25f;    // tốn mỗi giây giữ beam

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

        private float _energy;
        private bool _isDepletedLock;

        private Tween _fillColorTween;
        private Color _currentFillTargetColor;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        /// <summary>Khởi tạo: cache ref, setup UI/camera, init energy.</summary>
        private void Start()
        {
            CacheReferences();
            InitTutorialVisual();
            CacheInitialAngles();
            SetCannonCameraPriority(idlePriority);

            InitEnergy();
            SyncEnergyUI(true);
        }

        /// <summary>Loop: vào/thoát điều khiển; nếu đang điều khiển thì xoay+bắn; tick energy.</summary>
        private void Update()
        {
            HandleControlToggleInput();

            if (_isControllingCannon)
            {
                HandleRotationInput();
                HandleFireByMode();
            }

            TickEnergy(Time.deltaTime);
        }

        /// <summary>Khi disable: trả control, tắt beam, reset camera + UI.</summary>
        private void OnDisable()
        {
            if (_isControllingCannon && _playerHub != null)
                _playerHub.SetPlayerControlAndModel(false);

            _isControllingCannon = false;
            StopBeamIfNeeded();
            SetCannonCameraPriority(idlePriority);

            KillFillColorTween();
            SyncEnergyUI(true);
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

        /// <summary>Lưu góc ban đầu để clamp yaw/pitch đúng.</summary>
        private void CacheInitialAngles()
        {
            _currentYaw = NormalizeAngle(yawPivot.localEulerAngles.y);
            _currentPitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        }

        /// <summary>Init trạng thái UI tutorial ban đầu.</summary>
        private void InitTutorialVisual()
        {
            _isPlayerInTrigger = false;
            _isControllingCannon = false;
            _isBeamFiring = false;

            if (tutorialUI != null) tutorialUI.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
        }

        /// <summary>Khởi tạo energy về full và reset lock.</summary>
        private void InitEnergy()
        {
            maxEnergy = Mathf.Max(0f, maxEnergy);
            _energy = maxEnergy;
            _isDepletedLock = false;

            // Set màu fill ban đầu cho đúng ngay lập tức
            _currentFillTargetColor = GetEnergyColor(1f);
            if (energyFillImage != null)
                energyFillImage.color = _currentFillTargetColor;
        }

        /// <summary>Set priority camera cannon.</summary>
        private void SetCannonCameraPriority(int priority)
        {
            if (cannonCamera == null) return;
            cannonCamera.Priority = priority;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === TRIGGER UI ===

        /// <summary>Vào vùng: hiện tutorial UI, tắt arrow.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _isPlayerInTrigger = true;

            if (tutorialUI != null) tutorialUI.SetActive(true);
            if (tutorialArrow != null) tutorialArrow.SetActive(false);
        }

        /// <summary>Ra vùng: tắt tutorial UI và bật arrow nếu không điều khiển.</summary>
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

            if (_isControllingCannon) ExitCannonControl();
            else if (_isPlayerInTrigger) EnterCannonControl();
        }

        /// <summary>Vào điều khiển: khóa player, ưu tiên camera cannon.</summary>
        private void EnterCannonControl()
        {
            if (_playerHub != null)
                _playerHub.SetPlayerControlAndModel(true);

            _isControllingCannon = true;
            SetCannonCameraPriority(controllingPriority);

            SyncEnergyUI(true);
        }

        /// <summary>Thoát điều khiển: mở khóa player, tắt beam, reset camera + UI.</summary>
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

            SyncEnergyUI(true);
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

        /// <summary>Projectile: bấm một lần bắn một viên.</summary>
        private void HandleProjectileFire()
        {
            if (_cannonInput.GetFireDown())
                FireProjectile();
        }

        /// <summary>Beam: bấm để start, giữ để update, nhả để stop.</summary>
        private void HandleBeamFire()
        {
            if (_cannonInput.GetFireDown())
            {
                if (!CanFire()) return;            // đang LOCK thì cấm bắn
                if (!HasEnoughEnergy(0.01f)) return;

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
            {
                StopBeamIfNeeded();
            }
        }

        /// <summary>Bắn projectile (có cooldown + tốn energy + có LOCK khi cạn).</summary>
        public void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("WaterCannon: Chưa gán projectilePrefab.");
                return;
            }

            if (!CanFire()) return; // đang LOCK thì cấm bắn
            if (!HasEnoughEnergy(projectileEnergyCost)) return;
            if (Time.time < _lastFireTime + fireCooldown) return;

            _lastFireTime = Time.time;

            ConsumeEnergy(projectileEnergyCost);
            MarkDepletedIfNeeded(); // nếu cạn -> bật LOCK
            SyncEnergyUI();

            Vector3 muzzlePos = _muzzleTransform.position;
            Vector3 muzzleDir = _muzzleTransform.forward;

            PlayMuzzleEffect();

            var projectile = Instantiate(projectilePrefab, muzzlePos, Quaternion.identity);
            projectile.LaunchForward(muzzlePos, muzzleDir);
        }

        /// <summary>Tắt beam nếu đang bắn.</summary>
        private void StopBeamIfNeeded()
        {
            if (!_isBeamFiring) return;

            _isBeamFiring = false;

            if (beamVfx != null)
                beamVfx.StopAll();
        }

        /// <summary>Play VFX ở miệng súng.</summary>
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
        #region === ENERGY ===

        /// <summary>Tick energy: drain khi beam giữ; regen khi không drain; cạn thì LOCK tới khi full.</summary>
        private void TickEnergy(float dt)
        {
            if (maxEnergy <= 0f)
            {
                _energy = 0f;
                _isDepletedLock = true;
                StopBeamIfNeeded();
                SyncEnergyUI();
                return;
            }

            bool beamHeld = _isBeamFiring && _cannonInput != null && _cannonInput.GetFireHeld();

            // 1) Drain beam nếu đang bắn và không bị LOCK
            if (beamHeld && !_isDepletedLock)
            {
                ConsumeEnergy(beamEnergyCostPerSecond * dt);

                // Nếu cạn -> LOCK + stop beam ngay
                if (_energy <= 0f)
                {
                    _energy = 0f;
                    _isDepletedLock = true;
                    StopBeamIfNeeded();
                }
            }
            // 2) Regen (bình thường hoặc chậm nếu LOCK)
            else
            {
                float regenRate = _isDepletedLock ? depletedRegenPerSecond : energyRegenPerSecond;
                RegenerateEnergy(regenRate * dt);

                // Đang LOCK thì chỉ mở lại khi FULL
                if (_isDepletedLock && _energy >= maxEnergy)
                {
                    _energy = maxEnergy;
                    _isDepletedLock = false;
                }
            }

            SyncEnergyUI();
        }

        /// <summary>Cho phép bắn nếu không bị LOCK.</summary>
        private bool CanFire()
        {
            return !_isDepletedLock;
        }

        /// <summary>Đủ energy để trả cost không.</summary>
        private bool HasEnoughEnergy(float cost)
        {
            return _energy >= cost;
        }

        /// <summary>Trừ energy và clamp.</summary>
        private void ConsumeEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            _energy = Mathf.Clamp(_energy - amount, 0f, maxEnergy);
        }

        /// <summary>Cộng energy và clamp.</summary>
        private void RegenerateEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            _energy = Mathf.Clamp(_energy + amount, 0f, maxEnergy);
        }

        /// <summary>Nếu energy đã cạn thì bật LOCK.</summary>
        private void MarkDepletedIfNeeded()
        {
            if (_energy <= 0f)
            {
                _energy = 0f;
                _isDepletedLock = true;
            }
        }

        /// <summary>Đồng bộ slider (0..1) + show/hide + tween màu fill.</summary>
        private void SyncEnergyUI(bool force = false)
        {
            if (energySlider == null) return;

            float normalized = (maxEnergy <= 0f) ? 0f : (_energy / maxEnergy);
            energySlider.value = normalized;
            energySlider.gameObject.SetActive(true);

            UpdateEnergyFillColor(normalized, force);
        }

        /// <summary>Update màu fill theo mức năng lượng và tween cho mượt.</summary>
        private void UpdateEnergyFillColor(float normalized, bool force)
        {
            if (energyFillImage == null) return;

            Color target = GetEnergyColor(normalized);

            if (force)
            {
                KillFillColorTween();
                energyFillImage.color = target;
                _currentFillTargetColor = target;
                return;
            }

            // Tránh restart tween mỗi frame khi chưa đổi "ngưỡng màu"
            if (target == _currentFillTargetColor)
                return;

            _currentFillTargetColor = target;

            KillFillColorTween();
            _fillColorTween = energyFillImage
                .DOColor(target, colorTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>Lấy màu theo mức năng lượng: Full (xanh) / ~Half (vàng) / Near empty (đỏ).</summary>
        private Color GetEnergyColor(float normalized)
        {
            Color fullColor = HexToColor("72C8FA");
            Color halfColor = HexToColor("F2E63E");
            Color lowColor = HexToColor("F2443E");

            // Ngưỡng: bạn có thể chỉnh nếu muốn nhạy hơn
            if (normalized >= 0.66f) return fullColor;
            if (normalized >= 0.33f) return halfColor;
            return lowColor;
        }

        /// <summary>Chuyển HEX (RRGGBB) sang Color.</summary>
        private Color HexToColor(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            if (ColorUtility.TryParseHtmlString(hex, out Color c))
                return c;

            return Color.white;
        }

        /// <summary>Dọn tween màu fill để tránh chồng tween/leak.</summary>
        private void KillFillColorTween()
        {
            if (_fillColorTween != null && _fillColorTween.IsActive())
                _fillColorTween.Kill();

            _fillColorTween = null;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === ROTATION ===

        /// <summary>Đọc input Look để xoay yaw/pitch (có clamp).</summary>
        private void HandleRotationInput()
        {
            if (_cannonInput == null || yawPivot == null || pitchPivot == null)
                return;

            Vector2 look = _cannonInput.GetLookAxisRaw();
            if (look.sqrMagnitude < 0.0001f)
                return;

            float dt = Time.deltaTime;

            // Yaw
            _currentYaw += look.x * yawSpeed * dt;
            if (clampYaw)
                _currentYaw = Mathf.Clamp(_currentYaw, minYaw, maxYaw);

            // Pitch
            float pitchDelta = -look.y * pitchSpeed * dt;
            if (invertY) pitchDelta = -pitchDelta;

            _currentPitch += pitchDelta;
            _currentPitch = Mathf.Clamp(_currentPitch, minPitch, maxPitch);

            // Apply
            yawPivot.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        }

        /// <summary>Chuẩn hoá góc về [-180..180].</summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === GIZMOS ===

        /// <summary>Vẽ gizmo hướng miệng súng khi chọn object.</summary>
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
