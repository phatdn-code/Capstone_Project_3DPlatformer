using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    [System.Serializable]
    public class BossPhase
    {
        [Header("Phase Settings")]
        public string phaseName = "Phase";
        public int maxHealth = 100;
        public float moveSpeed = 5f;
        public float attackSpeed = 1f;
        public int damage = 10;
        public float sightRange = 10f;

        [Header("Visuals")]
        public Color phaseColor = Color.white;
        public Vector3 scale = Vector3.one;

        [Header("Special")]
        public bool canUseSpecialAbility = false;
        public string specialAbilityName = "";
        public float specialAbilityCooldown = 5f;
    }
}
