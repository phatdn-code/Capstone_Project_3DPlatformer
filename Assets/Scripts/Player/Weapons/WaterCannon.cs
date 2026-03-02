using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// WaterCannon: xoay + bắn (Projectile/Beam) khi Player vào chế độ cannon.
    /// Có Energy + UI Slider + cơ chế LOCK khi cạn năng lượng + tween đổi màu fill.
    /// </summary>
    [RequireComponent(typeof(CannonInput))]
    public class WaterCannon : MonoBehaviour
    {
        //────────────────────────────────────────────────────
        #region === ODIN HELPERS ===

        private bool IsProjectileMode() => fireMode == CannonFireMode.Projectile;
        private bool IsBeamMode() => fireMode == CannonFireMode.Beam;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: FIRE MODE ===

        [TitleGroup("Fire Mode", Alignment = TitleAlignments.Centered)]
        [EnumToggleButtons]
        [HideLabel]
        [SerializeField] private CannonFireMode fireMode = CannonFireMode.Projectile;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: REFERENCES ===

        [TitleGroup("References", Alignment = TitleAlignments.Centered)]

        [ShowIf(nameof(IsProjectileMode))]
        [FoldoutGroup("References/Projectile", Expanded = true)]
        [SerializeField] private WaterProjectile projectilePrefab;

        [ShowIf(nameof(IsBeamMode))]
        [FoldoutGroup("References/Beam", Expanded = true)]
        [SerializeField] private BeamVfx beamVfx;

        [FoldoutGroup("References/Muzzle VFX", Expanded = true)]
        [SerializeField] private ParticleSystem muzzleCastEffect;

        [FoldoutGroup("References/Rotation Pivots", Expanded = true)]
        [SerializeField] private Transform yawPivot;

        [FoldoutGroup("References/Rotation Pivots", Expanded = true)]
        [SerializeField] private Transform pitchPivot;

        [FoldoutGroup("References/Cinemachine Camera", Expanded = true)]
        [SerializeField] private CinemachineVirtualCameraBase cannonCamera;

        [FoldoutGroup("References/Cinemachine Camera", Expanded = true)]
        [SerializeField] private int controllingPriority = 100;

        [FoldoutGroup("References/Cinemachine Camera", Expanded = true)]
        [SerializeField] private int idlePriority = 0;

        [FoldoutGroup("References/Tutorial UI", Expanded = true)]
        [SerializeField] private GameObject tutorialArrow;

        [FoldoutGroup("References/Tutorial UI", Expanded = true)]
        [SerializeField] private GameObject tutorialUI;

        [FoldoutGroup("References/Tutorial UI", Expanded = true)]
        [SerializeField] private bool arrowVisibleByDefault = true;

        [FoldoutGroup("References/Energy UI", Expanded = true)]
        [SerializeField] private Slider energySlider;

        [FoldoutGroup("References/Energy UI", Expanded = true)]
        [SerializeField] private bool showEnergyOnlyWhenControlling = true;

        [FoldoutGroup("References/Energy UI", Expanded = true)]
        [SerializeField] private Image energyFillImage;

        [FoldoutGroup("References/Energy UI", Expanded = true)]
        [SerializeField] private float colorTweenDuration = 0.25f;

        #endregion

        //────────────────────────────────────────────────────
        #region === INSPECTOR: SETTINGS ===

        [TitleGroup("Settings", Alignment = TitleAlignments.Centered)]

        [ShowIf(nameof(IsProjectileMode))]
        [FoldoutGroup("Settings/Fire", Expanded = true)]
        [SerializeField] private float fireCooldown = 1.0f;

        [FoldoutGroup("Settings/Rotation", Expanded = true)]
        [SerializeField] private float yawSpeed = 90f;

        [FoldoutGroup("Settings/Rotation", Expanded = true)]
        [SerializeField] private float pitchSpeed = 60f;

        [FoldoutGroup("Settings/Rotation", Expanded = true)]
        [SerializeField] private float minPitch = -15f;

        [FoldoutGroup("Settings/Rotation", Expanded = true)]
        [SerializeField] private float maxPitch = 45f;

        [FoldoutGroup("Settings/Rotation", Expanded = true)]
        [SerializeField] private bool invertY = true;

        [FoldoutGroup("Settings/Yaw Clamp", Expanded = true)]
        [SerializeField] private bool clampYaw = true;

        [ShowIf(nameof(clampYaw))]
        [FoldoutGroup("Settings/Yaw Clamp", Expanded = true)]
        [SerializeField] private float minYaw = -120f;

        [ShowIf(nameof(clampYaw))]
        [FoldoutGroup("Settings/Yaw Clamp", Expanded = true)]
        [SerializeField] private float maxYaw = 120f;

        [FoldoutGroup("Settings/Energy", Expanded = true)]
        [SerializeField] private float maxEnergy = 100f;

        [FoldoutGroup("Settings/Energy", Expanded = true)]
        [SerializeField] private float energyRegenPerSecond = 15f;       // hồi bình thường

        [FoldoutGroup("Settings/Energy", Expanded = true)]
        [SerializeField] private float depletedRegenPerSecond = 10f;     // hồi chậm khi LOCK

        [ShowIf(nameof(IsProjectileMode))]
        [FoldoutGroup("Settings/Energy", Expanded = true)]
        [SerializeField] private float projectileEnergyCost = 20f;       // tốn mỗi viên

        [ShowIf(nameof(IsBeamMode))]
        [FoldoutGroup("Settings/Energy", Expanded = true)]
        [SerializeField] private float beamEnergyCostPerSecond = 25f;    // tốn mỗi giây giữ beam

        #endregion

        //────────────────────────────────────────────────────
        #region === RUNTIME STATE ===

        private const float kLookDeadZoneSqr = 0.0001f;
        private const float kMinBeamStartEnergy = 0.01f;

        private PlayerHub _playerHub;
        private CannonInput _input;

        private Transform _muzzle;
        private float _nextFireTime;

        private float _yaw;
        private float _pitch;

        private bool _isPlayerInTrigger;
        private bool _isControlling;
        private bool _isBeamFiring;

        private float _energy;
        private bool _isDepletedLock;

        private Tween _fillColorTween;
        private Color _fillColorTarget;

        #endregion

        //────────────────────────────────────────────────────
        #region === UNITY LIFECYCLE ===

        private void Start()
        {
            CacheReferences();
            CacheInitialAngles();

            SetControlling(false, false);
            SetTutorialState(inTrigger: false);

            InitEnergy();
            SyncEnergyUI(force: true);

            SubscribePlayerDie();
        }

        private void Update()
        {
            HandleControlToggleInput();

            if (_isControlling)
            {
                HandleRotationInput();
                HandleFireByMode();
            }

            TickEnergy(Time.deltaTime);
        }

        private void OnEnable()
        {
            _playerHub ??= PlayerHub.Instance;
            SubscribePlayerDie();
        }

        private void OnDisable()
        {
            UnsubscribePlayerDie();

            if (_isControlling)
                ExitCannonControl();

            StopBeamIfNeeded();
            SetCameraPriority(idlePriority);

            KillFillColorTween();
            SyncEnergyUI(force: true);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === INIT / CACHE ===

        /// <summary>VN: Cache input/hub/pivot/muzzle để dùng nhanh và tránh null.</summary>
        private void CacheReferences()
        {
            _input = GetComponent<CannonInput>();
            if (_input == null)
                Debug.LogError("WaterCannon: Không tìm thấy CannonInput.");

            _playerHub = PlayerHub.Instance;
            if (_playerHub == null)
                Debug.LogWarning("WaterCannon: Không tìm thấy PlayerHub trong scene.");

            yawPivot ??= transform;
            pitchPivot ??= transform;

            _muzzle = muzzleCastEffect != null ? muzzleCastEffect.transform : pitchPivot;
        }

        /// <summary>VN: Lưu góc ban đầu để clamp yaw/pitch đúng theo local.</summary>
        private void CacheInitialAngles()
        {
            _yaw = NormalizeAngle(yawPivot.localEulerAngles.y);
            _pitch = NormalizeAngle(pitchPivot.localEulerAngles.x);
        }

        /// <summary>VN: Khởi tạo Energy về full và reset LOCK + set màu fill ban đầu.</summary>
        private void InitEnergy()
        {
            maxEnergy = Mathf.Max(0f, maxEnergy);
            _energy = maxEnergy;
            _isDepletedLock = false;

            _fillColorTarget = GetEnergyColor(1f);
            if (energyFillImage != null)
                energyFillImage.color = _fillColorTarget;
        }

        /// <summary>VN: Set priority camera cannon theo trạng thái.</summary>
        private void SetCameraPriority(int priority)
        {
            if (cannonCamera != null)
                cannonCamera.Priority = priority;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === PLAYER DIE EVENT ===

        /// <summary>VN: Đăng ký OnDie của player để auto thoát cannon khi chết.</summary>
        private void SubscribePlayerDie()
        {
            var player = _playerHub != null ? _playerHub.Player : null;
            var eventsRef = player != null ? player.playerEvents : null;
            if (eventsRef == null) return;

            // Tránh add trùng.
            eventsRef.OnDie?.RemoveListener(OnPlayerDied);
            eventsRef.OnDie?.AddListener(OnPlayerDied);
        }

        /// <summary>VN: Huỷ đăng ký OnDie để tránh leak/double-call.</summary>
        private void UnsubscribePlayerDie()
        {
            var player = _playerHub != null ? _playerHub.Player : null;
            var eventsRef = player != null ? player.playerEvents : null;
            if (eventsRef == null) return;

            eventsRef.OnDie?.RemoveListener(OnPlayerDied);
        }

        /// <summary>VN: Callback khi player chết (đang điều khiển thì thoát ngay).</summary>
        private void OnPlayerDied()
        {
            if (_isControlling)
                ExitCannonControl();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === TRIGGER UI ===

        /// <summary>VN: Vào vùng trigger thì hiện hướng dẫn, tắt mũi tên.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetTutorialState(inTrigger: true);
        }

        /// <summary>VN: Ra vùng trigger thì ẩn hướng dẫn (nếu không controlling) và bật mũi tên.</summary>
        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            SetTutorialState(inTrigger: false);
        }

        /// <summary>VN: Bật/tắt UI tutorial theo việc player đang đứng trong vùng hay không.</summary>
        private void SetTutorialState(bool inTrigger)
        {
            _isPlayerInTrigger = inTrigger;

            if (inTrigger)
            {
                if (tutorialUI != null) tutorialUI.SetActive(true);
                if (tutorialArrow != null) tutorialArrow.SetActive(false);
                return;
            }

            // Nếu đang điều khiển thì đừng tắt UI theo trigger exit.
            if (_isControlling) return;

            if (tutorialUI != null) tutorialUI.SetActive(false);
            if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === CONTROL TOGGLE ===

        /// <summary>VN: Nhấn Interact để vào/thoát điều khiển cannon.</summary>
        private void HandleControlToggleInput()
        {
            if (_input == null || !_input.GetInteractDown())
                return;

            if (_isControlling) ExitCannonControl();
            else if (_isPlayerInTrigger) EnterCannonControl();
        }

        /// <summary>VN: Vào điều khiển cannon (khóa player, ưu tiên camera, hiện energy).</summary>
        private void EnterCannonControl()
        {
            SetControlling(true);
            SetCameraPriority(controllingPriority);
            SyncEnergyUI(force: true);
        }

        /// <summary>VN: Thoát điều khiển cannon (mở khóa player, stop beam, reset UI/camera).</summary>
        private void ExitCannonControl()
        {
            SetControlling(false);
            StopBeamIfNeeded();
            SetCameraPriority(idlePriority);

            if (!_isPlayerInTrigger)
            {
                if (tutorialUI != null) tutorialUI.SetActive(false);
                if (tutorialArrow != null) tutorialArrow.SetActive(arrowVisibleByDefault);
            }

            SyncEnergyUI(force: true);
        }

        /// <summary>VN: Set trạng thái controlling + đồng bộ PlayerHub (model/control + watercannon flag).</summary>
        private void SetControlling(bool isControlling, bool affectPlayer = true)
        {
            _isControlling = isControlling;

            if (_playerHub == null)
                _playerHub = PlayerHub.Instance;

            if (_playerHub == null) return;

            _playerHub.SetWaterCannonControl(isControlling);

            if (affectPlayer) _playerHub.SetPlayerControlAndModel(isControlling);
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === FIRE ===

        /// <summary>VN: Chọn logic bắn theo mode (Projectile/Beam).</summary>
        private void HandleFireByMode()
        {
            if (_input == null) return;

            if (fireMode == CannonFireMode.Projectile) HandleProjectileFire();
            else HandleBeamFire();
        }

        /// <summary>VN: Mode Projectile - bấm một lần bắn một viên.</summary>
        private void HandleProjectileFire()
        {
            if (_input.GetFireDown())
                FireProjectile();
        }

        /// <summary>VN: Mode Beam - bấm start, giữ update, nhả stop.</summary>
        private void HandleBeamFire()
        {
            if (_input.GetFireDown())
                TryStartBeam();

            if (_isBeamFiring && _input.GetFireHeld())
                beamVfx?.UpdateBeam();

            if (_isBeamFiring && _input.GetFireUp())
                StopBeamIfNeeded();
        }

        /// <summary>VN: Start beam nếu đủ điều kiện (không LOCK + còn năng lượng).</summary>
        private void TryStartBeam()
        {
            if (!CanFire()) return;
            if (!HasEnoughEnergy(kMinBeamStartEnergy)) return;

            _isBeamFiring = true;
            PlayMuzzleEffect();
            beamVfx?.StartBeam();
        }

        /// <summary>VN: Bắn projectile (cooldown + trừ energy + có thể LOCK khi cạn).</summary>
        public void FireProjectile()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("WaterCannon: Chưa gán projectilePrefab.");
                return;
            }

            fireCooldown = Mathf.Max(0f, fireCooldown);

            if (!CanFire()) return;
            if (!HasEnoughEnergy(projectileEnergyCost)) return;
            if (Time.time < _nextFireTime) return;

            _nextFireTime = Time.time + fireCooldown;

            ConsumeEnergy(projectileEnergyCost);
            MarkDepletedIfNeeded();
            SyncEnergyUI();

            Vector3 pos = _muzzle.position;
            Vector3 dir = _muzzle.forward;

            PlayMuzzleEffect();

            var projectile = Instantiate(projectilePrefab, pos, Quaternion.identity);
            projectile.LaunchForward(pos, dir);
        }

        /// <summary>VN: Stop beam nếu đang bắn (an toàn, gọi nhiều lần cũng không sao).</summary>
        private void StopBeamIfNeeded()
        {
            if (!_isBeamFiring) return;

            _isBeamFiring = false;
            beamVfx?.StopAll();
        }

        /// <summary>VN: Play VFX ở miệng súng khi bắn.</summary>
        private void PlayMuzzleEffect()
        {
            if (muzzleCastEffect == null) return;

            muzzleCastEffect.gameObject.SetActive(true);
            muzzleCastEffect.transform.SetPositionAndRotation(_muzzle.position, _muzzle.rotation);

            muzzleCastEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            muzzleCastEffect.Play();
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === ENERGY ===

        /// <summary>VN: Tick energy (drain khi beam, regen khi không drain, LOCK khi cạn tới khi full).</summary>
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

            bool isBeamHeld = _isBeamFiring && _input != null && _input.GetFireHeld();

            // Drain khi giữ beam và không bị LOCK.
            if (isBeamHeld && !_isDepletedLock)
            {
                ConsumeEnergy(beamEnergyCostPerSecond * dt);

                if (_energy <= 0f)
                {
                    _energy = 0f;
                    _isDepletedLock = true;
                    StopBeamIfNeeded();
                }

                SyncEnergyUI();
                return;
            }

            // Regen khi không drain.
            float regenRate = _isDepletedLock ? depletedRegenPerSecond : energyRegenPerSecond;
            RegenerateEnergy(regenRate * dt);

            // LOCK chỉ mở lại khi full.
            if (_isDepletedLock && _energy >= maxEnergy)
            {
                _energy = maxEnergy;
                _isDepletedLock = false;
            }

            SyncEnergyUI();
        }

        /// <summary>VN: Cho phép bắn nếu không bị LOCK.</summary>
        private bool CanFire() => !_isDepletedLock;

        /// <summary>VN: Kiểm tra đủ năng lượng để trả cost hay không.</summary>
        private bool HasEnoughEnergy(float cost) => _energy >= Mathf.Max(0f, cost);

        /// <summary>VN: Trừ energy và clamp về [0..max].</summary>
        private void ConsumeEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            _energy = Mathf.Clamp(_energy - amount, 0f, maxEnergy);
        }

        /// <summary>VN: Cộng energy và clamp về [0..max].</summary>
        private void RegenerateEnergy(float amount)
        {
            amount = Mathf.Max(0f, amount);
            _energy = Mathf.Clamp(_energy + amount, 0f, maxEnergy);
        }

        /// <summary>VN: Nếu energy đã cạn thì bật LOCK.</summary>
        private void MarkDepletedIfNeeded()
        {
            if (_energy > 0f) return;
            _energy = 0f;
            _isDepletedLock = true;
        }

        /// <summary>VN: Đồng bộ UI slider + show/hide + tween màu fill.</summary>
        private void SyncEnergyUI(bool force = false)
        {
            if (energySlider == null) return;

            float normalized = (maxEnergy <= 0f) ? 0f : (_energy / maxEnergy);
            energySlider.value = normalized;

            // Áp dụng đúng option "chỉ hiện khi controlling".
            bool shouldShow = !showEnergyOnlyWhenControlling || _isControlling;
            energySlider.gameObject.SetActive(shouldShow);

            UpdateEnergyFillColor(normalized, force);
        }

        /// <summary>VN: Tween đổi màu fill theo mức năng lượng (tránh restart mỗi frame).</summary>
        private void UpdateEnergyFillColor(float normalized, bool force)
        {
            if (energyFillImage == null) return;

            Color target = GetEnergyColor(normalized);

            if (force)
            {
                KillFillColorTween();
                energyFillImage.color = target;
                _fillColorTarget = target;
                return;
            }

            if (target == _fillColorTarget)
                return;

            _fillColorTarget = target;

            KillFillColorTween();
            _fillColorTween = energyFillImage
                .DOColor(target, colorTweenDuration)
                .SetEase(Ease.OutQuad);
        }

        /// <summary>VN: Map năng lượng -> màu (xanh/vàng/đỏ theo ngưỡng).</summary>
        private Color GetEnergyColor(float normalized)
        {
            Color full = HexToColor("72C8FA");
            Color half = HexToColor("F2E63E");
            Color low = HexToColor("F2443E");

            if (normalized >= 0.66f) return full;
            if (normalized >= 0.33f) return half;
            return low;
        }

        /// <summary>VN: Chuyển HEX (RRGGBB) sang Color Unity.</summary>
        private Color HexToColor(string hex)
        {
            if (!hex.StartsWith("#"))
                hex = "#" + hex;

            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.white;
        }

        /// <summary>VN: Kill tween đổi màu để tránh chồng tween/leak.</summary>
        private void KillFillColorTween()
        {
            if (_fillColorTween != null)
                _fillColorTween.Kill();

            _fillColorTween = null;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === ROTATION ===

        /// <summary>VN: Đọc input Look để xoay yaw/pitch (có clamp).</summary>
        private void HandleRotationInput()
        {
            if (_input == null || yawPivot == null || pitchPivot == null)
                return;

            Vector2 look = _input.GetLookAxisRaw();
            if (look.sqrMagnitude < kLookDeadZoneSqr)
                return;

            float dt = Time.deltaTime;

            // Yaw
            _yaw += look.x * yawSpeed * dt;
            if (clampYaw)
                _yaw = Mathf.Clamp(_yaw, minYaw, maxYaw);

            // Pitch
            float pitchDelta = -look.y * pitchSpeed * dt;
            if (invertY) pitchDelta = -pitchDelta;

            _pitch = Mathf.Clamp(_pitch + pitchDelta, minPitch, maxPitch);

            yawPivot.localRotation = Quaternion.Euler(0f, _yaw, 0f);
            pitchPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        /// <summary>VN: Chuẩn hoá góc về [-180..180] để clamp ổn định.</summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle > 180f) angle -= 360f;
            return angle;
        }

        #endregion

        //────────────────────────────────────────────────────
        #region === GIZMOS ===

        /// <summary>VN: Vẽ gizmo hướng miệng súng khi chọn object.</summary>
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