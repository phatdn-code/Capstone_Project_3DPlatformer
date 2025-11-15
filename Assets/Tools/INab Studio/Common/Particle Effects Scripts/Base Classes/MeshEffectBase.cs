using INab.CommonVFX;
using System.Collections.Generic;
using UnityEngine;

namespace INab.Common
{
    /// <summary>
    /// MeshParticlesBase + materials logic.
    /// </summary>
    [ExecuteAlways]
    public abstract class MeshEffectBase : MeshParticlesBase
    {
        public abstract string ShaderName { get; }
        public abstract bool MaterialsRequired { get; }

        #region Material Properties

        [SerializeField, Tooltip("")]
        protected bool useMaterials = true;

        public bool UseMaterials
        {
            get { return useMaterials; }
            private set { }
        }

        [SerializeField, Tooltip("List of materials used by the effect.")]
        public List<Material> materials = new List<Material>();

        [SerializeField, Tooltip("Ensures that the materials are only affected on the selected mesh renderer. This may breaks SRP batching.")]
        public bool useInstancedMaterials = false;

        [SerializeField, Tooltip("Rate at which the properties are scaled.")]
        protected float propertiesScaleRate = 1;
        public float PropertiesScaleRate
        {
            get { return propertiesScaleRate; }
            set
            {
                propertiesScaleRate = value;
                UpdateAllMaterialProperties();
            }
        }

        #endregion

        #region Material Properties Methods

        protected string GetMaterialPropertyPrefix()
        {
            return "_" + ShaderName;
        }

        protected void UpdateAllMaterialProperties(string propertyName, Color color)
        {
            foreach (var material in materials)
            {
                material.SetColor(GetMaterialPropertyPrefix() + propertyName, color);
            }
        }

        protected void UpdateAllMaterialProperties(string propertyName, float vector)
        {
            foreach (var material in materials)
            {
                material.SetFloat(GetMaterialPropertyPrefix() + propertyName, vector);
            }
        }

        protected void UpdateAllMaterialProperties(string propertyName, Vector3 vector)
        {
            foreach (var material in materials)
            {
                material.SetVector(GetMaterialPropertyPrefix() + propertyName, vector);
            }
        }

        protected void UpdateAllMaterialProperties(string propertyName, bool value)
        {
            foreach (var material in materials)
            {
                material.SetInt(GetMaterialPropertyPrefix() + propertyName, value ? 1 : 0);
            }
        }

        protected void UpdateAllMaterialProperties(string propertyName, Texture value)
        {
            if (value == null) return;
            foreach (var material in materials)
            {
                material.SetTexture(GetMaterialPropertyPrefix() + propertyName, value);
            }
        }


        /// <summary>
        /// Updates all materials's effect type properties attached to this script.
        /// </summary>
        protected void UpdateAllMaterialProperties()
        {
            if (materials == null) return;

            foreach (var material in materials)
            {
                MaterialPropertiesUpdate(material);
            }
        }

        protected virtual void MaterialPropertiesUpdate(Material material)
        {
        }


        #endregion

        #region UnityLifecycleMethods

        protected override void Start()
        {
            if (useInstancedMaterials && Application.isPlaying && useMaterials)
            {
                GetRendererMaterials(true);
            }
            base.Start();
        }

        

        protected override void OnValidate()
        {
            base.OnValidate();

            if (useMaterials)
            {
                UpdateAllMaterialProperties();
            }

            if(MaterialsRequired)
            {
                PassMaterialPropertiesToGraph();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Get materials from the mesh renderer.
        /// </summary>
        /// <param name="sharedMaterials"></param>
        public void GetRendererMaterials(bool instancedMaterials = false)
        {
            materials.Clear();

            if(!meshRenderer)
            {
                _FindRenderer();
            }

            if (instancedMaterials) materials.AddRange(meshRenderer.materials);
            else materials.AddRange(meshRenderer.sharedMaterials);
        }

        #endregion

        #region EditorMethods

        /// <summary>
        /// Editor only method to clear the materials list.
        /// </summary>
        public void _ClearMaterialsList()
        {
            materials.Clear();
        }

        public bool _DoMaterialsUseProperShaders()
        {
            bool value = true;
            foreach (var material in materials)
            {
                var name = material.shader.name;

                if (name != "INab Studio/" + ShaderName +" Effect")
                {
                    value = false;
                }
            }

            return value;
        }
        #endregion

        #region Vfx Graph Methods


        protected virtual void PassMaterialPropertiesToGraph(Material material)
        {
            if(MaterialsRequired)
            {
                Debug.Log("PassMaterialPropertiesToGraph was called, but MaterialsRequired is false.");
                return;
            }
        }

        public Material GetDefaultMaterial()
        {
            if (materials.Count < 1)
            {
                return null;
            }

            int index = 0;

            var submeshCount = VFXMeshSamplingHelper.RendererToMesh(meshRenderer).subMeshCount;
            if (materials.Count == submeshCount && usePerSubmeshBaking == true)
            {
                index = submeshIndex;
            }

            Material material = materials[index];

            if (visualEffect != null && useParticleEffects == true && material != null)
            {
                return material;
            }

            return null;
        }


        /// <summary>
        /// Copies properties (base map, guide properties, etc.) from material from materials list (at index 0) to the effect graph.
        /// </summary>
        public void PassMaterialPropertiesToGraph()
        {
            PassMaterialPropertiesToGraph(GetDefaultMaterial());
        }
        #endregion
    }
}