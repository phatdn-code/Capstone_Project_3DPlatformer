using UnityEngine;
using UnityEngine.Events;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Lớp tiện ích (Utility class) chứa các hàm hỗ trợ chung cho hệ thống Boss.
    /// Không lưu trữ state, chỉ cung cấp các hàm static dùng chung.
    /// </summary>
    public static class BossUtils
    {
        // ─────────────────────────────────────────────────────────────
        // FIND / SEARCH UTILITIES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Tìm Player trong scene (dùng API khác nhau tùy Unity version).
        /// </summary>
        public static Player FindPlayer()
        {
            return Object.FindFirstObjectByType<Player>();
        }

        /// <summary>
        /// Tìm BaseBoss trong scene (dùng API khác nhau tùy Unity version).
        /// </summary>
        public static BaseBoss FindBoss()
        {
            return Object.FindFirstObjectByType<BaseBoss>();
        }

        /// <summary>
        /// Tìm component T đầu tiên trong scene.
        /// </summary>
        public static T FindComponent<T>() where T : Component
        {
            return Object.FindFirstObjectByType<T>();
        }

        // ─────────────────────────────────────────────────────────────
        // GAMEOBJECT UTILITIES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Kích hoạt/ẩn một GameObject (nếu khác null).
        /// </summary>
        public static void SetActiveSafe(GameObject go, bool active)
        {
            if (go != null) go.SetActive(active);
        }

        /// <summary>
        /// Tìm child theo tên (tìm đệ quy trong hierarchy).
        /// </summary>
        public static Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                var result = FindChildRecursive(child, childName);
                if (result != null) return result;
            }
            return null;
        }

        // ─────────────────────────────────────────────────────────────
        // MATH / POSITION UTILITIES
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Sinh ra vị trí ngẫu nhiên xung quanh gốc với offset (trong hình tròn).
        /// </summary>
        public static Vector3 GetRandomOffsetPosition(Vector3 origin, float radius)
        {
            Vector2 randomCircle = Random.insideUnitCircle * radius;
            return origin + new Vector3(randomCircle.x, 0, randomCircle.y);
        }

        /// <summary>
        /// Tính hướng (vector unit) từ A đến B.
        /// </summary>
        public static Vector3 GetDirection(Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }

        // ─────────────────────────────────────────────────────────────
        // BOSS EVENTS SETUP
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Gắn các listener cho Boss events một cách an toàn.
        /// Cho phép truyền callback tuỳ chọn (null thì bỏ qua).
        /// </summary>
        public static void SetupBossEvents(
            BaseBoss boss,
            UnityAction<int> onPhaseStart = null,
            UnityAction<string> onSpecialAbility = null,
            UnityAction<int> onPhaseChanged = null,
            UnityAction onBossHealed = null,
            UnityAction onBossDefeated = null)
        {
            if (boss == null || boss.bossHealth == null) return;

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
    }
}
