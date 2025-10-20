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

        [Header("Visuals")]
        public Color phaseColor = Color.white;
        public Vector3 scale = Vector3.one;
    }
}
