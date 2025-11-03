using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Gây damage khi hạt particle va chạm với player.
    /// </summary>
    public class ParticleDamage : MonoBehaviour
    {
        [Header("Damage Settings")]
        [SerializeField] private int damage = 5;

        private void OnParticleCollision(GameObject other)
        {
            if (other.CompareTag(GameTags.Player) && other.TryGetComponent<Player>(out var player))
            {
                player.ApplyDamage(damage, transform.position);
                Debug.Log($"🔥 Particle hit player, dealt {damage} damage");
            }
        }
    }
}
