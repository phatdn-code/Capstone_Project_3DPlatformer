using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Component cho cầu lửa của boss
    /// </summary>
    public class BossFireball : MonoBehaviour
    {
        [Header("Fireball Settings")]
        [Tooltip("Sát thương của cầu lửa")]
        [SerializeField] private int damage = 25;

        [Tooltip("Tốc độ bay của cầu lửa")]
        [SerializeField] private float speed = 12f;

        [Tooltip("Thời gian tồn tại")]
        [SerializeField] private float lifetime = 10f;

        [Tooltip("Bán kính va chạm")]
        [SerializeField] private float collisionRadius = 0.1f;
        
        [Tooltip("Sử dụng manual movement thay vì Rigidbody")]
        [SerializeField] private bool useManualMovement = false;

        [Tooltip("Hiệu ứng cầu lửa")]
        [SerializeField] private GameObject effect;

        // Không cần target nữa vì boss đã quay mặt về player

        private Vector3 m_direction;
        private bool m_hasHit = false;
        private float m_travelTime = 0f;
        
        // Pool management
        private bool m_isFromPool = false;

        // Public properties for access from other classes
        public int Damage => damage;
        public float Speed => speed;
        public GameObject Effect => effect;

        // Public methods to set values
        public void SetDamage(int newDamage) => damage = newDamage;
        public void SetSpeed(float newSpeed) => speed = newSpeed;
        
        // Pool management methods
        public bool IsFromPool => m_isFromPool;
        
        /// <summary>
        /// Setup fireball từ pool với các thông số mới
        /// </summary>
        public void SetupFromPool(int newDamage, float newSpeed, float newLifetime)
        {
            m_isFromPool = true;
            damage = newDamage;
            speed = newSpeed;
            lifetime = newLifetime;
            
            // Reset trạng thái
            m_hasHit = false;
            m_travelTime = 0f;
            m_direction = Vector3.zero;
            
            // Reset Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = false;
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
                rb.freezeRotation = true;
            }
            
            // Reset Collider
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = true;
            }
            
            // Cancel previous coroutines nếu có
            StopAllCoroutines();
            
            // Kích hoạt GameObject
            gameObject.SetActive(true);
            
        }
        public void SetEffect(GameObject newEffect) => effect = newEffect;
        public void SetUseManualMovement(bool useManual) => useManualMovement = useManual;

        private void Start()
        {
            
            // Đảm bảo fireball không spawn quá thấp
            if (transform.position.y < 1f)
            {
                Vector3 pos = transform.position;
                pos.y = 1f; // Đặt độ cao tối thiểu là 1
                transform.position = pos;
            }

            if (useManualMovement)
            {
                // Manual movement - xóa Rigidbody nếu có để tránh conflict
                Rigidbody existingRb = GetComponent<Rigidbody>();
                if (existingRb != null)
                    DestroyImmediate(existingRb);
            }

            else
            {
                // Thêm Rigidbody để fireball bay đúng cách
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb == null)
                    rb = gameObject.AddComponent<Rigidbody>();
                
                // Cấu hình Rigidbody cho fireball
                rb.useGravity = false; // Fireball không bị trọng lực
                rb.linearDamping = 0f;
                rb.angularDamping = 0f;
                rb.freezeRotation = true; // Không xoay tự do
                rb.isKinematic = false; // Đảm bảo không bị kinematic
                rb.constraints = RigidbodyConstraints.FreezeRotation; // Chỉ freeze rotation
                
            }

            // Fireball bay thẳng về phía trước (boss đã quay mặt về player rồi)
            m_direction = transform.forward;
            m_direction.y = 0; // Bay ngang, không lên xuống
            m_direction = m_direction.normalized;
            
            // Debug log để kiểm tra
            Debug.Log($"🔥 Fireball Setup - Direction: {m_direction}, Speed: {speed}, Manual: {useManualMovement}");

            if (!useManualMovement)
            {
                // Áp dụng vận tốc ban đầu cho Rigidbody
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = m_direction * speed;
                    // Đảm bảo Rigidbody không bị kinematic
                    rb.isKinematic = false;
                }
            }

            // Tự hủy sau lifetime
            StartCoroutine(AutoDestroyAfterLifetime());

            // Tạo hiệu ứng
            if (effect != null)
            {
                GameObject fireEffect = Instantiate(effect, transform.position, Quaternion.identity);
                fireEffect.transform.SetParent(transform);
            }
        }

        private void Update()
        {
            if (m_hasHit) return;

            if (useManualMovement)
            {
                // Manual movement - bay thẳng hoàn toàn (không bị trọng lực)
                Vector3 movement = m_direction * speed * Time.deltaTime;
                
                // Đảm bảo Y coordinate không thay đổi (bay ngang)
                Vector3 newPosition = transform.position + movement;
                newPosition.y = transform.position.y; // Giữ nguyên Y
                transform.position = newPosition;
                
                // Debug log để kiểm tra
                if (m_direction == Vector3.zero)
                    Debug.LogError("❌ Fireball direction is zero!");
            }

            else
            {
                // Sử dụng Rigidbody để bay thẳng (không bị trọng lực)
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // Đảm bảo fireball bay thẳng với tốc độ cố định
                    Vector3 targetVelocity = m_direction * speed;
                    rb.linearVelocity = targetVelocity;
                    
                    // Đảm bảo không có thành phần Y bị ảnh hưởng bởi gravity
                    if (rb.useGravity)
                    {
                        rb.useGravity = false;
                    }
                    
                }
            }
            
            m_travelTime += Time.deltaTime;

            // Xoay cầu lửa
            transform.Rotate(Vector3.up, 90f * Time.deltaTime);

            // Kiểm tra va chạm
            CheckCollision();
        }

        /// <summary>
        /// Kiểm tra va chạm
        /// </summary>
        private void CheckCollision()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, collisionRadius);


            foreach (var collider in colliders)
            {
                // Chỉ biến mất khi trúng Player hoặc tường
                if (collider.CompareTag(GameTags.Player))
                {
                    HitPlayer(collider);
                    return;
                }

                else if (collider.CompareTag("Wall"))
                {
                    // Kiểm tra layer để xác định có phải tường không
                    if (collider.gameObject.layer == LayerMask.NameToLayer("Wall"))
                    {
                        HitWall();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Va chạm với player
        /// </summary>
        private void HitPlayer(Collider playerCollider)
        {
            if (m_hasHit) return;
            m_hasHit = true;


            if (playerCollider.TryGetComponent<Player>(out var player))
                player.ApplyDamage(damage, transform.position);

            // Hiệu ứng va chạm
            CreateHitEffect();
            
            // Trả fireball về pool hoặc destroy
            if (m_isFromPool)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }

            else Destroy(gameObject);
        }

        /// <summary>
        /// Va chạm với ground
        /// </summary>
        private void HitWall()
        {
            if (m_hasHit) return;
            m_hasHit = true;


            // Hiệu ứng nổ khi chạm tường
            CreateHitEffect();
            
            // Trả fireball về pool hoặc destroy
            if (m_isFromPool)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }

            else Destroy(gameObject);
        }

        private void HitGround()
        {
            if (m_hasHit) return;
            m_hasHit = true;


            // Hiệu ứng nổ khi chạm đất
            CreateHitEffect();
            
            // Trả fireball về pool hoặc destroy
            if (m_isFromPool)
            {
                ResetForPool();
                gameObject.SetActive(false);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Tự hủy sau lifetime
        /// </summary>
        private System.Collections.IEnumerator AutoDestroyAfterLifetime()
        {
            yield return new WaitForSeconds(lifetime);

            if (!m_hasHit)
            {
                CreateHitEffect();
                
                // Trả fireball về pool hoặc destroy
                if (m_isFromPool)
                {
                    ResetForPool();
                    gameObject.SetActive(false);
                }
                else Destroy(gameObject);
            }
        }

        /// <summary>
        /// Tạo hiệu ứng va chạm
        /// </summary>
        private void CreateHitEffect()
        {
            // Tạo hiệu ứng nổ
            GameObject hitEffect = new GameObject("FireballHitEffect");
            hitEffect.transform.position = transform.position;

            // Thêm particle system hoặc hiệu ứng khác
            // TODO: Thêm hiệu ứng nổ cầu lửa
        }

        /// <summary>
        /// Vẽ bán kính va chạm trong Scene view
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionRadius);
        }
        
        /// <summary>
        /// Reset fireball cho pool khi được deactivate
        /// </summary>
        private void ResetForPool()
        {
            // Reset tất cả trạng thái
            m_hasHit = false;
            m_travelTime = 0f;
            m_direction = Vector3.zero;
            
            // Reset Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
            
            // Reset Collider
            Collider col = GetComponent<Collider>();
            if (col != null)
                col.enabled = false;
            
            // Stop all coroutines
            StopAllCoroutines();
        }
    }
}


