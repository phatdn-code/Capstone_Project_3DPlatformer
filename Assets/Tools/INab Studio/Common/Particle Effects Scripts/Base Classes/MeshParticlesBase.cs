using INab.CommonVFX;
using System;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;

namespace INab.Common
{
    /// <summary>
    /// Vfx graph properties with uniform mesh baker for skinned and standard meshes. Play/Stop vfx graph events.
    /// </summary>
    [ExecuteAlways]
    public abstract class MeshParticlesBase : MonoBehaviour
    {
        #region Visual Effect Graph Properties

        [SerializeField, Tooltip("Indicates whether to use VFX Graph Effect.")]
        public bool useParticleEffects = false;

        [SerializeField, Tooltip("Reference to the Visual Effect component.")]
        public VisualEffect visualEffect;

        [SerializeField, Tooltip("Which visual effect asset the visual effect will use.")]
        public VisualEffectAsset visualEffectAsset;

        [SerializeField, Tooltip("Reference to the VFX Property Binder component.")]
        public VFXPropertyBinder propertyBinder;

        public Transform meshTransform
        {
            get
            {
                if (meshRenderer != null)
                {
                    if (IsSkinnedMesh)
                    {
                        var skinned = meshRenderer as SkinnedMeshRenderer;
                        return skinned.rootBone.parent.transform;
                    }
                    else
                    {
                        return meshRenderer.transform;
                    }
                }

                return null;
            }
        }

        [SerializeField, Tooltip("Renderer of the mesh used for the effect.")]
        public Renderer meshRenderer;

        /// <summary>
        /// Determines if the mesh renderer is a skinned mesh renderer.
        /// </summary>
        public bool IsSkinnedMesh
        {
            get
            {
                return meshRenderer is SkinnedMeshRenderer;
            }
            private set { }
        }

        /// <summary>
        /// Determines if the mesh in the mesh renderer is Readable.
        /// </summary>
        public bool IsMeshReadable
        {
            get
            {
                bool readable;

                if (meshRenderer == null) return true;

                if (IsSkinnedMesh)
                {
                    var renderer = meshRenderer as SkinnedMeshRenderer;
                    readable = renderer.sharedMesh.isReadable;
                }
                else
                {
                    var filter = meshRenderer.GetComponent<MeshFilter>();
                    readable = filter.sharedMesh.isReadable;
                }

                return readable;
            }
            private set { }
        }

        [SerializeField, Tooltip("Instance of the VFX Uniform Mesh Baker.")]
        public VFXUniformMeshBaker meshBaker = new VFXUniformMeshBaker();

        [Range(0.01f, 10f), SerializeField, Tooltip("Multiply sample count by this value to control density of the particles. Keep this as low as possible.")]
        public float sampleCountMultiplier = 1f;

        [SerializeField, Tooltip("Whether to use the effect only on one submesh.")]
        public bool usePerSubmeshBaking = false;
        [SerializeField, Tooltip("Submesh index.")]
        public int submeshIndex = 0; 

        #endregion

        #region Unity Lifecycle Methods

        protected virtual void Start()
        {
            SetupVfxGraph();
        }

        protected virtual void Update()
        {
            if (useParticleEffects)
            {
                if (visualEffect && meshRenderer) meshBaker.Update(visualEffect, meshRenderer);
            }
        }

        protected virtual void OnDisable()
        {
            meshBaker.OnDisable();
        }

        protected virtual void OnValidate()
        {
            if (enabled == false || gameObject.activeSelf == false) return;

            if (useParticleEffects)
            {
                meshBaker.SampleCountMultiplier = sampleCountMultiplier;
                meshBaker.UsePerSubmeshBaking = usePerSubmeshBaking;
                meshBaker.SubmeshIndex = submeshIndex;
            }
            else
            {
                SendStopEvent();
            }

            _SetGraphicsBuffer();
        }

        #endregion

        #region Editor Methods

        /// <summary>
        /// Editor only method to bake the mesh for the VFX Graph.
        /// </summary>
        public void _BakeUniformMesh()
        {
            if (meshRenderer && visualEffect) meshBaker.Bake(visualEffect, meshRenderer);
        }

        /// <summary>
        /// Editor only method to set the graphics buffer for the VFX Graph.
        /// </summary>
        public void _SetGraphicsBuffer()
        {
            if (visualEffect) meshBaker.SetGraphicsBuffer(visualEffect);
        }


        /// <summary>
        /// Editor only method to find the renderer in the parent.
        /// </summary>
        public void _FindRenderer()
        {
            meshRenderer = GetComponentInParent<Renderer>();
            if (meshRenderer == null)
            {
                if(gameObject.transform.parent) meshRenderer = gameObject.transform.parent.GetComponentInChildren<Renderer>();
                else meshRenderer = gameObject.GetComponentInChildren<Renderer>();
            }

            if (meshRenderer == null)
            {
                Debug.LogWarning("No renderer could be found.");
            }

            if (!(meshRenderer is SkinnedMeshRenderer) && !(meshRenderer is MeshRenderer))
            {
                meshRenderer = null;
                Debug.LogWarning("Found renderer different than SkinnedMeshRenderer or MeshRenderer.");
            }
        }

        /// <summary>
        /// Editor only method to setup the VFX Graph GameObject.
        /// </summary>
        public void _SetupVfxGraphGameObject(string initialEventName = "OnPlay")
        {
            GameObject newGameObject = new GameObject("Vfx Graph");
            newGameObject.transform.parent = transform;
            newGameObject.transform.localPosition = Vector3.zero;
            newGameObject.transform.localRotation = Quaternion.identity;
            newGameObject.transform.localScale = Vector3.one;

            visualEffect = newGameObject.AddComponent<VisualEffect>();
            visualEffect.initialEventName = initialEventName;
            propertyBinder = newGameObject.AddComponent<VFXPropertyBinder>();
        }



        #endregion

        #region Public Methods

        /// <summary>
        /// Bakes uniform mesh buffer, setups propery binder and mesh renderer properties in the vfx grap.
        /// Calls OnValidate().
        /// </summary>
        public virtual void SetupVfxGraph()
        {
            if (useParticleEffects && meshRenderer)
            {
                VFXMeshSetup.SetupRenderer(meshRenderer, visualEffect);
                VFXMeshSetup.SetupPropertyBinder(propertyBinder, meshTransform);
            }

            OnValidate();

            if (useParticleEffects)
            {
                _BakeUniformMesh();
            }

            if(visualEffect)
            {
                visualEffect.Reinit();
            }
        }


        #endregion

        #region Vfx Graph Methods

        /// <summary>
        /// Sends a play event to the VFX Graph.
        /// </summary>
        public void SendPlayEvent()
        {
            if (visualEffect) visualEffect.Play();
        }

        /// <summary>
        /// Sends a stop event to the VFX Graph.
        /// </summary>
        public void SendStopEvent()
        {
            if (visualEffect) visualEffect.Stop();
        }

        #endregion
    }
}