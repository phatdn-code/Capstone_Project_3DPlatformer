using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [System.Serializable]
    public class BossPhase
    {
        [Header("Phase Settings")]
        [Tooltip("Tên giai đoạn boss")]
        public string phaseName = "Phase";
        
        [Tooltip("Máu tối đa cho giai đoạn này")]
        public int maxHealth = 100;
        
        [Tooltip("Tốc độ di chuyển trong giai đoạn này")]
        public float moveSpeed = 5f;
        
        [Tooltip("Tốc độ tấn công trong giai đoạn này")]
        public float attackSpeed = 1f;
        
        [Tooltip("Sát thương trong giai đoạn này")]
        public int damage = 10;
        
        [Tooltip("Tầm nhìn trong giai đoạn này")]
        public float sightRange = 10f;
        
        [Header("Visual Effects")]
        [Tooltip("Màu sắc boss trong giai đoạn này")]
        public Color phaseColor = Color.white;
        
        [Tooltip("Kích thước scale trong giai đoạn này")]
        public Vector3 scale = Vector3.one;
        
        [Header("Special Abilities")]
        [Tooltip("Có thể sử dụng kỹ năng đặc biệt không")]
        public bool canUseSpecialAbility = false;
        
        [Tooltip("Tên kỹ năng đặc biệt")]
        public string specialAbilityName = "";
        
        [Tooltip("Cooldown của kỹ năng đặc biệt")]
        public float specialAbilityCooldown = 5f;
    }
}


