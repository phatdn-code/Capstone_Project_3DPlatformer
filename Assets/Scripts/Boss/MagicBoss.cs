using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Boss phép thuật - Sử dụng các spell khác nhau, có thể teleport
    /// </summary>
    [AddComponentMenu("PLAYER TWO/Platformer Project/Boss/Magic Boss")]
    public class MagicBoss : BaseBoss
    {
        [Header("Magic Boss Settings")]
        [Tooltip("Prefab spell")]
        public GameObject[] spellPrefabs;
        
        [Tooltip("Điểm spawn spell")]
        public Transform[] spellSpawnPoints;
        
        [Tooltip("Hiệu ứng teleport")]
        public GameObject teleportEffect;
        
        [Tooltip("Hiệu ứng spell casting")]
        public GameObject castingEffect;
        
        [Tooltip("Khoảng cách teleport")]
        public float teleportDistance = 10f;
        
        [Tooltip("Cooldown teleport")]
        public float teleportCooldown = 8f;

        private float m_lastTeleportTime;
        private bool m_isCasting = false;

        protected override void Start()
        {
            base.Start();
            InitializeMagicBoss();
        }

        private void InitializeMagicBoss()
        {
            // Khởi tạo các giá trị ban đầu cho Magic Boss
            m_lastTeleportTime = 0f;
            m_isCasting = false;
        }

        protected override void UpdateBossBehavior()
        {
            base.UpdateBossBehavior();
            
            if (currentPhase == null) return;

            // Magic behavior
            if (CanTeleport())
            {
                PerformTeleport();
            }

            if (CanCastSpell())
            {
                CastSpell();
            }
        }

        /// <summary>
        /// Kiểm tra có thể teleport không
        /// </summary>
        protected virtual bool CanTeleport()
        {
            if (player == null) return false;
            if (m_isCasting) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= attackRange && Time.time >= m_lastTeleportTime + teleportCooldown;
        }

        /// <summary>
        /// Thực hiện teleport
        /// </summary>
        protected virtual void PerformTeleport()
        {
            m_lastTeleportTime = Time.time;

            // Hiệu ứng teleport đi
            if (teleportEffect != null)
            {
                Instantiate(teleportEffect, transform.position, Quaternion.identity);
            }

            // Tính toán vị trí teleport
            Vector3 teleportPosition = GetTeleportPosition();
            
            // Teleport
            transform.position = teleportPosition;
            
            // Hiệu ứng teleport đến
            if (teleportEffect != null)
            {
                Instantiate(teleportEffect, transform.position, Quaternion.identity);
            }

            Debug.Log($"{GetType().Name} teleport tới vị trí mới!");
        }

        /// <summary>
        /// Lấy vị trí teleport
        /// </summary>
        protected virtual Vector3 GetTeleportPosition()
        {
            Vector3 direction = (player.position - transform.position).normalized;
            Vector3 teleportPos = player.position + direction * teleportDistance;
            
            // Đảm bảo không teleport ra ngoài map
            teleportPos.y = transform.position.y;
            return teleportPos;
        }

        /// <summary>
        /// Kiểm tra có thể cast spell không
        /// </summary>
        protected virtual bool CanCastSpell()
        {
            if (player == null) return false;
            if (m_isCasting) return false;
            if (m_isAttacking) return false;

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= attackRange && Time.time >= m_lastAttackTime + attackInterval;
        }

        /// <summary>
        /// Cast spell
        /// </summary>
        protected virtual void CastSpell()
        {
            m_isCasting = true;
            m_lastAttackTime = Time.time;

            // Hiệu ứng casting
            if (castingEffect != null)
            {
                Instantiate(castingEffect, transform.position, Quaternion.identity);
            }

            // Chọn spell dựa trên giai đoạn
            GameObject spellToCast = SelectSpell();
            
            if (spellToCast != null)
            {
                StartCoroutine(CastSpellCoroutine(spellToCast));
            }
        }

        /// <summary>
        /// Chọn spell để cast
        /// </summary>
        protected virtual GameObject SelectSpell()
        {
            if (spellPrefabs == null || spellPrefabs.Length == 0) return null;

            // Chọn spell dựa trên giai đoạn
            int spellIndex = 0;
            
            if (currentPhase.phaseName.Contains("2"))
            {
                spellIndex = 1; // Spell giai đoạn 2
            }
            else if (currentPhase.phaseName.Contains("3"))
            {
                spellIndex = 2; // Spell giai đoạn 3
            }

            spellIndex = Mathf.Clamp(spellIndex, 0, spellPrefabs.Length - 1);
            return spellPrefabs[spellIndex];
        }

        /// <summary>
        /// Coroutine cast spell
        /// </summary>
        protected virtual System.Collections.IEnumerator CastSpellCoroutine(GameObject spellPrefab)
        {
            // Thời gian casting
            yield return new WaitForSeconds(1f);

            // Tạo spell
            Vector3 spawnPos = GetSpellSpawnPosition();
            GameObject spell = Instantiate(spellPrefab, spawnPos, Quaternion.identity);
            
            // Setup spell
            SetupSpell(spell);

            m_isCasting = false;
            Debug.Log($"{GetType().Name} cast spell: {spellPrefab.name}!");
        }

        /// <summary>
        /// Lấy vị trí spawn spell
        /// </summary>
        protected virtual Vector3 GetSpellSpawnPosition()
        {
            if (spellSpawnPoints != null && spellSpawnPoints.Length > 0)
            {
                int randomIndex = Random.Range(0, spellSpawnPoints.Length);
                return spellSpawnPoints[randomIndex].position;
            }
            
            return transform.position + Vector3.up;
        }

        /// <summary>
        /// Setup spell
        /// </summary>
        protected virtual void SetupSpell(GameObject spell)
        {
            var spellComponent = spell.GetComponent<BossSpell>();
            if (spellComponent == null)
            {
                spellComponent = spell.AddComponent<BossSpell>();
            }

            spellComponent.damage = currentPhase.damage;
            spellComponent.target = player;
        }

        /// <summary>
        /// Override kỹ năng đặc biệt cho Magic Boss
        /// </summary>
        protected override void UseSpecialAbility()
        {
            base.UseSpecialAbility();

            if (currentPhase.phaseName.Contains("2"))
            {
                // Giai đoạn 2: Multi Spell
                PerformMultiSpell();
            }
            else if (currentPhase.phaseName.Contains("3"))
            {
                // Giai đoạn 3: Ultimate Spell
                PerformUltimateSpell();
            }
        }

        /// <summary>
        /// Multi Spell - Cast nhiều spell cùng lúc
        /// </summary>
        protected virtual void PerformMultiSpell()
        {
            Debug.Log($"{GetType().Name} sử dụng Multi Spell!");
            
            StartCoroutine(CastMultipleSpells());
        }

        /// <summary>
        /// Ultimate Spell - Spell mạnh nhất
        /// </summary>
        protected virtual void PerformUltimateSpell()
        {
            Debug.Log($"{GetType().Name} sử dụng Ultimate Spell!");
            
            // Tạo spell ở nhiều vị trí khác nhau
            for (int i = 0; i < 3; i++)
            {
                Vector3 spawnPos = player.position + Random.insideUnitSphere * 5f;
                spawnPos.y = transform.position.y;
                
                GameObject ultimateSpell = Instantiate(spellPrefabs[2], spawnPos, Quaternion.identity);
                SetupSpell(ultimateSpell);
            }
        }

        /// <summary>
        /// Cast multiple spells
        /// </summary>
        protected virtual System.Collections.IEnumerator CastMultipleSpells()
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject spell = SelectSpell();
                if (spell != null)
                {
                    Vector3 spawnPos = GetSpellSpawnPosition();
                    GameObject spellInstance = Instantiate(spell, spawnPos, Quaternion.identity);
                    SetupSpell(spellInstance);
                }
                
                yield return new WaitForSeconds(0.5f);
            }
        }

        /// <summary>
        /// Override để thêm logic đặc biệt khi chuyển giai đoạn
        /// </summary>
        protected override void OnPhaseChanged(int newPhase)
        {
            base.OnPhaseChanged(newPhase);
            
            // Magic Boss tăng sức mạnh spell ở giai đoạn sau
            if (newPhase >= 1)
            {
                teleportCooldown *= 0.7f; // Teleport nhanh hơn
            }
            
            if (newPhase >= 2)
            {
                // Giai đoạn 3: Có thể teleport liên tục
                teleportCooldown *= 0.5f;
            }
        }
    }

    /// <summary>
    /// Component cho spell của boss
    /// </summary>
    public class BossSpell : MonoBehaviour
    {
        public int damage = 15;
        public Player target;
        public float lifetime = 3f;
        
        private void Start()
        {
            Destroy(gameObject, lifetime);
        }
        
        private void Update()
        {
            // Spell tự động tìm target
            if (target != null)
            {
                Vector3 direction = (target.position - transform.position).normalized;
                transform.position += direction * 10f * Time.deltaTime;
            }
        }
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(GameTags.Player))
            {
                if (other.TryGetComponent<Player>(out var player))
                {
                    player.ApplyDamage(damage, transform.position);
                }
                
                Destroy(gameObject);
            }
        }
    }
}
