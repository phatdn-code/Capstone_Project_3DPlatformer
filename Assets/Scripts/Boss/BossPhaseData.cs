using UnityEngine;
using System.Collections.Generic;

namespace PLAYERTWO.PlatformerProject
{
    /// <summary>
    /// Dữ liệu cấu hình cho từng giai đoạn (phase) của Boss
    /// </summary>
    [System.Serializable]
    public struct BossPhaseData
    {
        [Header("Phase Info")]
        public string phaseName;
        public int maxHealth;
        public float moveSpeed;
        public float attackSpeed;
        public int damage;
        public float sightRange;

        [Header("Visuals")]
        public Color phaseColor;
        public Vector3 scale;

        [Header("Special Ability")]
        public bool canUseSpecialAbility;
        public string specialAbilityName;
        public float specialAbilityCooldown;

        public BossPhaseData(string name, int health, float move, float attack, int dmg, float sight,
                             Color color, Vector3 scl, bool special, string ability, float cooldown)
        {
            phaseName = name;
            maxHealth = health;
            moveSpeed = move;
            attackSpeed = attack;
            damage = dmg;
            sightRange = sight;
            phaseColor = color;
            scale = scl;
            canUseSpecialAbility = special;
            specialAbilityName = ability;
            specialAbilityCooldown = cooldown;
        }
    }
}