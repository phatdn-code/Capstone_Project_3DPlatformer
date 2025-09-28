using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Component để xử lý animation events cho Soldier Robot
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Soldier Robot Animation Events")]
    public class SoldierRobotAnimationEvents : MonoBehaviour
    {
        [Header("Animation Events")]
        [Tooltip("Soldier Robot component")]
        [SerializeField] private SoldierRobot soldierRobot;

        [Tooltip("Animator của boss")]
        private Animator bossAnimator;

        private void Start()
        {
            Debug.Log("🎬 SoldierRobotAnimationEvents Start()");
            Debug.Log($"🎬 GameObject: {gameObject.name}");
            Debug.Log($"🎬 Parent: {(transform.parent != null ? transform.parent.name : "null")}");

            // Tự động tìm SoldierRobot nếu chưa được gán
            if (soldierRobot == null)
            {
                soldierRobot = GetComponentInParent<SoldierRobot>();
                Debug.Log($"🔍 Tìm SoldierRobot trong parent hierarchy: {(soldierRobot != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
            }

            // Tự động tìm Animator nếu chưa được gán
            if (bossAnimator == null)
            {
                // Tìm Animator trong Skin trước
                if (soldierRobot != null && soldierRobot.SkinAnimator != null)
                {
                    bossAnimator = soldierRobot.SkinAnimator;
                    Debug.Log($"🔍 Tìm Skin Animator: {(bossAnimator != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
                }
                else
                {
                    // Tìm Animator trong children hoặc parent
                    bossAnimator = GetComponent<Animator>();

                    if (bossAnimator == null)
                        bossAnimator = GetComponent<Animator>();

                    Debug.Log($"🔍 Tìm Animator trong hierarchy: {(bossAnimator != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
                }
            }

            Debug.Log($"🎬 Setup hoàn tất - SoldierRobot: {(soldierRobot != null ? "✅" : "❌")}, Animator: {(bossAnimator != null ? "✅" : "❌")}");
        }

        /// <summary>
        /// Animation event: Bắn bom từ tay phải
        /// Được gọi từ animation "Right Hand Shoot"
        /// </summary>
        public void OnRightHandShoot()
        {
            Debug.Log("🎯 OnRightHandShoot() được gọi!");

            // Fallback: Tìm SoldierRobot nếu null
            if (soldierRobot == null)
            {
                soldierRobot = GetComponentInParent<SoldierRobot>();
                Debug.Log($"🔍 Fallback tìm SoldierRobot: {(soldierRobot != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
            }

            if (soldierRobot != null)
            {
                soldierRobot.ShootBombFromAnimation(true); // true = sử dụng tay phải
                Debug.Log("✅ Animation Event: Bắn bom từ tay phải!");
            }
            else
            {
                Debug.LogError("❌ SoldierRobot is null! Không thể bắn bom!");
            }
        }

        /// <summary>
        /// Animation event: Bắn bom từ tay trái
        /// Được gọi từ animation "Left Hand Shoot"
        /// </summary>
        public void OnLeftHandShoot()
        {
            Debug.Log("🎯 OnLeftHandShoot() được gọi!");

            // Fallback: Tìm SoldierRobot nếu null
            if (soldierRobot == null)
            {
                soldierRobot = GetComponentInParent<SoldierRobot>();
                Debug.Log($"🔍 Fallback tìm SoldierRobot: {(soldierRobot != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
            }

            if (soldierRobot != null)
            {
                soldierRobot.ShootBombFromAnimation(false); // false = sử dụng tay trái
                Debug.Log("✅ Animation Event: Bắn bom từ tay trái!");
            }
            else
            {
                Debug.LogError("❌ SoldierRobot is null! Không thể bắn bom!");
            }
        }

        /// <summary>
        /// Animation event: Bắt đầu animation bắn
        /// </summary>
        public void OnShootStart()
        {
            Debug.Log("Animation Event: Bắt đầu animation bắn!");

            // Có thể thêm hiệu ứng âm thanh, particle, etc.
            PlayShootSound();
        }

        /// <summary>
        /// Animation event: Kết thúc animation bắn
        /// </summary>
        public void OnShootEnd()
        {
            Debug.Log("Animation Event: Kết thúc animation bắn!");
        }

        /// <summary>
        /// Animation event: Bắt đầu animation cầu lửa
        /// </summary>
        public void OnFireballStart()
        {
            Debug.Log("Animation Event: Bắt đầu animation cầu lửa!");

            // Có thể thêm hiệu ứng chuẩn bị cầu lửa
            PlayFireballChargeSound();
        }

        /// <summary>
        /// Animation event: Bắn cầu lửa
        /// </summary>
        public void OnFireballShoot()
        {
            Debug.Log("🎯 OnFireballShoot() được gọi!");

            // Fallback: Tìm SoldierRobot nếu null
            if (soldierRobot == null)
            {
                soldierRobot = GetComponentInParent<SoldierRobot>();
                Debug.Log($"🔍 Fallback tìm SoldierRobot: {(soldierRobot != null ? "✅ Tìm thấy" : "❌ Không tìm thấy")}");
            }

            if (soldierRobot != null)
            {
                // Gọi method tạo cầu lửa từ SoldierRobot
                soldierRobot.CreateFireballFromAnimation();
                Debug.Log("✅ Animation Event: Bắn cầu lửa!");
            }
            else
            {
                Debug.LogError("❌ SoldierRobot is null! Không thể bắn cầu lửa!");
            }
        }

        /// <summary>
        /// Animation event: Kết thúc animation cầu lửa
        /// </summary>
        public void OnFireballEnd()
        {
            Debug.Log("Animation Event: Kết thúc animation cầu lửa!");
        }

        /// <summary>
        /// Animation event: Boss bị damage
        /// </summary>
        public void OnDamageTaken()
        {
            Debug.Log("Animation Event: Boss nhận sát thương!");

            // Có thể thêm hiệu ứng damage
            PlayDamageSound();
        }

        /// <summary>
        /// Animation event: Boss chuyển giai đoạn
        /// </summary>
        public void OnPhaseTransition()
        {
            Debug.Log("Animation Event: Boss chuyển giai đoạn!");

            // Có thể thêm hiệu ứng chuyển giai đoạn
            PlayPhaseTransitionEffect();
        }

        /// <summary>
        /// Animation event: Boss chết
        /// </summary>
        public void OnBossDeath()
        {
            Debug.Log("Animation Event: Boss chết!");

            // Có thể thêm hiệu ứng chết
            PlayDeathEffect();
        }

        // Helper methods cho hiệu ứng
        private void PlayShootSound()
        {
            // TODO: Thêm âm thanh bắn bom
            // AudioSource.PlayClipAtPoint(shootSound, transform.position);
        }

        private void PlayFireballChargeSound()
        {
            // TODO: Thêm âm thanh chuẩn bị cầu lửa
            // AudioSource.PlayClipAtPoint(fireballChargeSound, transform.position);
        }

        private void PlayDamageSound()
        {
            // TODO: Thêm âm thanh nhận sát thương
            // AudioSource.PlayClipAtPoint(damageSound, transform.position);
        }

        private void PlayPhaseTransitionEffect()
        {
            // TODO: Thêm hiệu ứng chuyển giai đoạn
            // Instantiate(phaseTransitionEffect, transform.position, Quaternion.identity);
        }

        private void PlayDeathEffect()
        {
            // TODO: Thêm hiệu ứng chết
            // Instantiate(deathEffect, transform.position, Quaternion.identity);
        }

        /// <summary>
        /// Method để trigger animation từ code
        /// </summary>
        /// <param name="animationName">Tên animation</param>
        public void TriggerAnimation(string animationName)
        {
            if (bossAnimator != null)
            {
                bossAnimator.SetTrigger(animationName);
                Debug.Log($"🎬 Trigger animation: {animationName}");
            }
            else
            {
                Debug.LogError($"❌ Không thể trigger animation {animationName} - Animator null!");
            }
        }

        /// <summary>
        /// Debug animation state
        /// </summary>
        public void DebugAnimationState()
        {
            if (bossAnimator != null)
            {
                Debug.Log($"🎬 Animator State: {bossAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash}");
                Debug.Log($"🎬 Animator Speed: {bossAnimator.speed}");
                Debug.Log($"🎬 Animator Enabled: {bossAnimator.enabled}");
            }
            else
            {
                Debug.LogError("❌ Animator null - không thể debug!");
            }
        }

        /// <summary>
        /// Method để set animation parameter
        /// </summary>
        /// <param name="parameterName">Tên parameter</param>
        /// <param name="value">Giá trị</param>
        public void SetAnimationParameter(string parameterName, float value)
        {
            if (bossAnimator != null)
                bossAnimator.SetFloat(parameterName, value);
        }

        /// <summary>
        /// Method để set animation parameter (bool)
        /// </summary>
        /// <param name="parameterName">Tên parameter</param>
        /// <param name="value">Giá trị</param>
        public void SetAnimationParameter(string parameterName, bool value)
        {
            if (bossAnimator != null)
                bossAnimator.SetBool(parameterName, value);
        }

        // Public methods to set values
        public void SetSoldierRobot(SoldierRobot newSoldierRobot) => soldierRobot = newSoldierRobot;
        public void SetBossAnimator(Animator newAnimator) => bossAnimator = newAnimator;
    }
}


