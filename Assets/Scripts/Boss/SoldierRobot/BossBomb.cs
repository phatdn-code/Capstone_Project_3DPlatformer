using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Component cho bom của boss
    /// </summary>
    public class BossBomb : MonoBehaviour
    {
        [Header("Bomb Settings")]
        [Tooltip("Sát thương của bom")]
        [SerializeField] private int damage = 20;
        
        
        [Tooltip("Lực ném ban đầu (cho quỹ đạo cong)")]
        [SerializeField] private float throwForce = 18f;
        
        [Tooltip("Thời gian trước khi nổ")]
        [SerializeField] private float fuseTime = 2f;
        
        [Tooltip("Bán kính nổ")]
        [SerializeField] private float explosionRadius = 8f;
        
        [Tooltip("Lực đẩy khi nổ")]
        [SerializeField] private float explosionForce = 800f;
        
        [Tooltip("Hiệu ứng nổ")]
        [SerializeField] private GameObject explosionEffect;
        
        [Tooltip("Target của bom")]
        private Player target;

        private Vector3 m_velocity;
        private bool m_hasExploded = false;
        private float m_travelTime = 0f;
        private bool m_isGrounded = false;
        
        // Pool management
        private bool m_isFromPool = false;

        // Public properties for access from other classes
        public int Damage => damage;
        public GameObject ExplosionEffect => explosionEffect;
        
        // Pool management methods
        public bool IsFromPool => m_isFromPool;
        public Player Target => target;

        // Public methods to set values
        public void SetDamage(int newDamage) => damage = newDamage;
        public void SetExplosionEffect(GameObject newEffect) => explosionEffect = newEffect;
        public void SetTarget(Player newTarget) => target = newTarget;
        
        /// <summary>
        /// Setup bomb từ pool với các thông số mới
        /// </summary>
        public void SetupFromPool(Player newTarget, float newThrowForce, int newDamage, float newFuseTime, float newExplosionRadius, float newExplosionForce)
        {
            m_isFromPool = true;
            target = newTarget;
            throwForce = newThrowForce;
            damage = newDamage;
            fuseTime = newFuseTime;
            explosionRadius = newExplosionRadius;
            explosionForce = newExplosionForce;
            
            // Reset trạng thái
            m_hasExploded = false;
            m_travelTime = 0f;
            m_isGrounded = false;
            m_velocity = Vector3.zero;
            
            // Reset Rigidbody
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.useGravity = true;
                rb.linearDamping = 0.1f;
                rb.angularDamping = 0.1f;
            }
            
            // Reset Collider
            Collider col = GetComponent<Collider>();

            if (col != null)
                col.enabled = true;
            
            // Cancel previous invoke nếu có
            CancelInvoke(nameof(Explode));
            
            // Setup Rigidbody và velocity (giống như Start())
            // GameObject đã được activate bởi PoolManager
            SetupBombPhysics();
        }
        
        /// <summary>
        /// Setup physics cho bomb (dùng cho cả pool và instantiate)
        /// </summary>
        private void SetupBombPhysics()
        {
            
            // Thêm Rigidbody để bomb bay đúng cách
            Rigidbody rb = GetComponent<Rigidbody>();

            if (rb == null)
                rb = gameObject.AddComponent<Rigidbody>();
            
            // Cấu hình Rigidbody
            rb.useGravity = true;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.1f;
            
            // Tính toán quỹ đạo cong từ đầu (không bay lên)
            if (target != null)
            {
                Vector3 targetPos = target.position;
                // Thêm offset để bomb chệch hướng nhiều hơn (dễ trúng player với range lớn)
                Vector3 randomOffset = BossUtils.GetRandomOffset(8f);
                targetPos += randomOffset;

                Vector3 direction = BossUtils.GetDirection(transform.position, targetPos);
                float horizontalSpeed = throwForce;
                m_velocity = new Vector3(direction.x * horizontalSpeed, 0, direction.z * horizontalSpeed);
                rb.linearVelocity = m_velocity;
                
            }
            else
            {
                // Nếu không có target, tìm player trong scene
                Player player = BossUtils.FindPlayer();
                if (player != null)
                {
                    target = player;
                    Vector3 targetPos = target.position;
                    
                    // Thêm offset ngẫu nhiên
                    Vector3 randomOffset = BossUtils.GetRandomOffset(8f);
                    targetPos += randomOffset;
                    
                    Vector3 direction = BossUtils.GetDirection(transform.position, targetPos);
                    float horizontalSpeed = throwForce;
                    m_velocity = new Vector3(direction.x * horizontalSpeed, 0, direction.z * horizontalSpeed);
                    rb.linearVelocity = m_velocity;
                    
                }
                else
                {
                    // Fallback: bay theo hướng forward
                    m_velocity = transform.forward * throwForce;
                    rb.linearVelocity = m_velocity;
                    
                }
            }
            
            // Tự hủy sau fuse time
            Invoke(nameof(Explode), fuseTime);
        }
        
        public void SetThrowForce(float newForce) => throwForce = newForce;

        private void Start()
        {
            // Sử dụng SetupBombPhysics() để setup physics
            SetupBombPhysics();
        }

        private void Update()
        {
            if (m_hasExploded) return;

            // Sử dụng Rigidbody để bay (không manual movement)
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null && !m_isGrounded)
            {
                // Kiểm tra bomb có bị stuck không
                if (rb.linearVelocity.magnitude < 0.1f && m_travelTime > 0.5f)
                {
                    // Force push bomb ra khỏi vật cản
                    Vector3 pushDirection = (target != null) ? (target.position - transform.position).normalized : transform.forward;
                    rb.linearVelocity = pushDirection * throwForce;
                }
                
                // Kiểm tra chạm đất
                if (transform.position.y <= 0.1f) // Giả sử ground ở y = 0
                {
                    transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
                    m_isGrounded = true;
                    rb.linearVelocity = Vector3.zero;
                    rb.useGravity = false;
                }
            }
            
            m_travelTime += Time.deltaTime;

            // Xoay bom khi bay
            transform.Rotate(Vector3.up, 180f * Time.deltaTime);
        }

        /// <summary>
        /// Nổ bom
        /// </summary>
        public void Explode()
        {
            if (m_hasExploded) return;
            m_hasExploded = true;

            // Hiệu ứng nổ
            if (explosionEffect != null)
            {
                GameObject effect = Instantiate(explosionEffect, transform.position, Quaternion.identity);
                // Tự hủy hiệu ứng sau 3 giây
                Destroy(effect, 3f);
            }

            // Gây sát thương trong phạm vi
            DealExplosionDamage();

            // Áp dụng lực đẩy
            ApplyExplosionForce();

            // Trả bomb về pool hoặc destroy
            if (m_isFromPool)
            {
                // Reset trước khi trả về pool
                ResetForPool();
                gameObject.SetActive(false);
            }

            else
                // Destroy nếu không từ pool
                Destroy(gameObject);
        }

        /// <summary>
        /// Gây sát thương trong phạm vi nổ
        /// </summary>
        private void DealExplosionDamage()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag(GameTags.Player))
                {
                    if (collider.TryGetComponent<Player>(out var player))
                    {
                        // Tính sát thương dựa trên khoảng cách
                        float distance = Vector3.Distance(transform.position, player.position);
                        float damageMultiplier = 1f - (distance / explosionRadius);
                        damageMultiplier = Mathf.Clamp(damageMultiplier, 0.1f, 1f);
                        
                        int finalDamage = Mathf.RoundToInt(damage * damageMultiplier);
                        player.ApplyDamage(finalDamage, transform.position);
                        
                    }
                }
            }
        }

        /// <summary>
        /// Áp dụng lực đẩy
        /// </summary>
        private void ApplyExplosionForce()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            
            foreach (var collider in colliders)
            {
                if (collider.CompareTag(GameTags.Player))
                {
                    if (collider.TryGetComponent<Rigidbody>(out var rb))
                    {
                        Vector3 direction = (collider.transform.position - transform.position).normalized;
                        float distance = Vector3.Distance(transform.position, collider.transform.position);
                        float forceMultiplier = 1f - (distance / explosionRadius);
                        
                        rb.AddForce(direction * explosionForce * forceMultiplier);
                    }
                }
            }
        }

        /// <summary>
        /// Kích hoạt nổ khi va chạm
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player) ||
                other.CompareTag("Ground") ||
                other.CompareTag("Untagged"))
                Explode();
        }

        /// <summary>
        /// Vẽ Gizmos để hiển thị phạm vi nổ trong Scene view
        /// </summary>
        private void OnDrawGizmos()
        {
            // Vẽ phạm vi nổ với màu đỏ nhạt
            Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
            
            // Vẽ viền phạm vi nổ
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
            
            // Vẽ target nếu có
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireCube(target.position, Vector3.one * 0.5f);
            }
        }

        /// <summary>
        /// Vẽ Gizmos khi được chọn (luôn hiển thị)
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Vẽ phạm vi nổ với màu đậm hơn khi được chọn
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Gizmos.DrawSphere(transform.position, explosionRadius);
            
            // Vẽ phạm vi nổ với viền đậm
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
            
            // Vẽ target với màu sáng hơn
            if (target != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, target.position);
                Gizmos.DrawWireCube(target.position, Vector3.one * 0.5f);
                
                // Vẽ tên target
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(target.position + Vector3.up * 2f, "Target: " + target.name);
                #endif
            }
            
            // Vẽ thông tin bomb
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 3f, 
                $"Bomb\nDamage: {damage}\nRadius: {explosionRadius}\nForce: {explosionForce}");
            #endif
        }
        
        /// <summary>
        /// Reset bomb cho pool khi được deactivate
        /// </summary>
        private void ResetForPool()
        {
            // Reset tất cả trạng thái
            m_hasExploded = false;
            m_travelTime = 0f;
            m_isGrounded = false;
            m_velocity = Vector3.zero;
            
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
            
            // Cancel all invokes
            CancelInvoke();
        }
    }
}
