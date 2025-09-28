using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Utility class chứa các helper methods chung cho boss system
    /// </summary>
    public static class BossUtils
    {
        /// <summary>
        /// Tìm Player trong scene với conditional compilation
        /// </summary>
        public static Player FindPlayer()
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<Player>();
#else
            return Object.FindObjectOfType<Player>();
#endif
        }
        
        /// <summary>
        /// Tìm BaseBoss trong scene với conditional compilation
        /// </summary>
        public static BaseBoss FindBoss()
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<BaseBoss>();
#else
            return Object.FindObjectOfType<BaseBoss>();
#endif
        }
        
        /// <summary>
        /// Tìm component với conditional compilation
        /// </summary>
        public static T FindComponent<T>() where T : Component
        {
#if UNITY_6000_0_OR_NEWER
            return Object.FindFirstObjectByType<T>();
#else
            return Object.FindObjectOfType<T>();
#endif
        }
        
        /// <summary>
        /// Lấy BossHealth component từ GameObject
        /// </summary>
        public static BossHealth GetBossHealth(GameObject obj)
        {
            return obj.GetComponent<BossHealth>();
        }
        
        /// <summary>
        /// Reset GameObject về pool (deactivate)
        /// </summary>
        public static void ReturnToPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }
        
        /// <summary>
        /// Activate GameObject từ pool
        /// </summary>
        public static void ActivateFromPool(GameObject obj)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }
        
        /// <summary>
        /// Kiểm tra GameObject có active không
        /// </summary>
        public static bool IsActive(GameObject obj)
        {
            return obj != null && obj.activeInHierarchy;
        }
        
        /// <summary>
        /// Tìm child transform theo tên (recursive)
        /// </summary>
        public static Transform FindChildTransform(Transform parent, string childName)
        {
            if (parent == null) return null;
            
            // Tìm trực tiếp
            Transform found = parent.Find(childName);
            if (found != null) return found;
            
            // Tìm recursive trong children
            foreach (Transform child in parent)
            {
                found = FindChildTransform(child, childName);
                if (found != null) return found;
            }
            
            return null;
        }
        
        /// <summary>
        /// Tạo random offset cho projectile
        /// </summary>
        public static Vector3 GetRandomOffset(float range)
        {
            return new Vector3(
                Random.Range(-range, range),
                0,
                Random.Range(-range, range)
            );
        }
        
        /// <summary>
        /// Tính toán direction từ source đến target
        /// </summary>
        public static Vector3 GetDirection(Vector3 source, Vector3 target)
        {
            return (target - source).normalized;
        }
        
        /// <summary>
        /// Setup boss events với optional callbacks
        /// </summary>
        public static void SetupBossEvents(BaseBoss boss, UnityEngine.Events.UnityAction<int> onPhaseStart = null, 
                                        UnityEngine.Events.UnityAction<string> onSpecialAbility = null,
                                        UnityEngine.Events.UnityAction<int> onPhaseChanged = null,
                                        UnityEngine.Events.UnityAction onBossHealed = null,
                                        UnityEngine.Events.UnityAction onBossDefeated = null)
        {
            if (boss == null) return;
            
            if (onPhaseStart != null)
                boss.OnBossPhaseStartEvent.AddListener(onPhaseStart);
                
            if (onSpecialAbility != null)
                boss.OnSpecialAbilityUsedEvent.AddListener(onSpecialAbility);
                
            if (onPhaseChanged != null)
                boss.bossHealth.OnPhaseChanged.AddListener(onPhaseChanged);
                
            if (onBossHealed != null)
                boss.bossHealth.OnBossHealed.AddListener(onBossHealed);
                
            if (onBossDefeated != null)
                boss.bossHealth.OnBossDefeated.AddListener(onBossDefeated);
        }
        
        /// <summary>
        /// Kiểm tra distance giữa 2 points
        /// </summary>
        public static float GetDistance(Vector3 point1, Vector3 point2)
        {
            return Vector3.Distance(point1, point2);
        }
    }
}
