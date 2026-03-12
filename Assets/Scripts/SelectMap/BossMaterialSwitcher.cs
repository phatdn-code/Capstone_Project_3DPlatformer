using UnityEngine;

namespace PLAYERTWO.PlatformerProject
{
    public class BossMaterialSwitcher : MonoBehaviour
    {
        [Header("Elements")]
        [SerializeField] private Material defeatedMaterial;
        [SerializeField] private Material normalMaterial;

        [Header("Settings")]
        [SerializeField] private bool includeInactiveChildren = true;

        private SkinnedMeshRenderer[] skinnedMeshRenderers;

        private void Awake()
        {
            CacheRenderers();
        }

        /// <summary>
        /// VN: Cache toàn bộ SkinnedMeshRenderer của boss.
        /// </summary>
        private void CacheRenderers()
        {
            skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>(includeInactiveChildren);
        }

        /// <summary>
        /// VN: Đổi material của boss theo trạng thái đã clear hay chưa.
        /// </summary>
        public void ApplyState(bool wasDefeated)
        {
            if (skinnedMeshRenderers == null || skinnedMeshRenderers.Length == 0)
                CacheRenderers();

            Material targetMaterial = wasDefeated ? defeatedMaterial : normalMaterial;

            if (targetMaterial == null)
                return;

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer == null)
                    continue;

                Material[] materials = renderer.sharedMaterials;

                for (int i = 0; i < materials.Length; i++)
                    materials[i] = targetMaterial;

                renderer.sharedMaterials = materials;
            }
        }
    }
}