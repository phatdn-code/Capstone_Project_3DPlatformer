using UnityEngine;

namespace MiniGame
{
    /// <summary>
    /// Lớp quản lý toàn bộ hệ thống Boid (đàn chim / cá bay theo đàn).
    /// Chịu trách nhiệm spawn và điều khiển các boid xung quanh object này.
    /// </summary>
    public class BoidFlockManager : MonoBehaviour
    {
        /// <summary>
        /// Prefab được dùng để tạo một boid.
        /// </summary>
        public GameObject boidPrefab;

        /// <summary>
        /// Số lượng boid sẽ được tạo khi bắt đầu game.
        /// </summary>
        public int spawnCount = 10;

        /// <summary>
        /// Bán kính xung quanh object này mà boid có thể được spawn.
        /// </summary>
        public float spawnRadius = 100f;

        /// <summary>
        /// Khoảng cách tối thiểu giữa các boid (dùng cho thuật toán flocking).
        /// </summary>
        public float neighborDistance = 10.0f;

        /// <summary>
        /// Tốc độ di chuyển của boid.
        /// </summary>
        public float speed = 10f;

        /// <summary>
        /// Độ biến thiên của tốc độ (tạo sự tự nhiên).
        /// </summary>
        public float speedVariation = 1f;

        /// <summary>
        /// Hệ số quay đầu của boid (quyết định boid xoay nhanh hay chậm).
        /// </summary>
        public float rotationCoefficient = 5.0f;

        /// <summary>
        /// Layer trong Unity mà các boid thuộc về (dùng để tìm neighbors).
        /// </summary>
        public LayerMask searchLayer;

        /// <summary>
        /// Khi game bắt đầu sẽ tạo sẵn một số lượng boid.
        /// </summary>
        void Start()
        {
            for (var i = 0; i < spawnCount; i++)
            {
                Spawn();
            }
        }

        /// <summary>
        /// Spawn boid tại vị trí ngẫu nhiên trong bán kính spawnRadius.
        /// </summary>
        public GameObject Spawn()
        {
            return Spawn(transform.position + Random.insideUnitSphere * spawnRadius);
        }

        /// <summary>
        /// Spawn boid tại vị trí cụ thể.
        /// </summary>
        public GameObject Spawn(Vector3 position)
        {
            // Tạo rotation ngẫu nhiên nhưng vẫn gần với hướng của object quản lý
            var rotation = Quaternion.Slerp(transform.rotation, Random.rotation, 0.25f);

            // Instantiate boid
            var boid = Instantiate(boidPrefab, position, rotation) as GameObject;

            // Gán manager này cho boid
            boid.GetComponent<BoidAgent>().master = this;

            // Nếu object này có parent thì đặt boid cùng cấp trong hierarchy
            if (this.transform.parent != null)
            {
                boid.transform.parent = this.transform.parent;
            }

            return boid;
        }
    }
}