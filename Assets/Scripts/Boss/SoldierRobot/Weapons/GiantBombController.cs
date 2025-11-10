using System;
using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// ✦ Điều khiển bomb khổng lồ:
    /// - Chờ player đánh trúng để phản về boss.
    /// - Kích hoạt sự kiện khi bomb trúng boss.
    /// </summary>
    [RequireComponent(typeof(BossBomb))]
    public class GiantBombController : MonoBehaviour
    {
        public event Action onHitBoss;

        //─────────────────────────────────────────────
        private bool exploded;

        private void OnEnable() => exploded = false;

        /// <summary>
        /// Xử lý khi bomb va vào boss → kích hoạt sự kiện thắng.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (exploded) return;

            if (other.CompareTag(GameTags.Boss))
            {
                exploded = true;
                onHitBoss?.Invoke();
            }
        }

        /// <summary>
        /// Khi player đánh trúng bomb → bomb bay ngược về boss.
        /// </summary>
        public void ReflectTo(Vector3 bossPosition)
        {
            var bomb = GetComponent<BossBomb>();
            bomb.LaunchToPosition(bossPosition);
        }
    }
}
