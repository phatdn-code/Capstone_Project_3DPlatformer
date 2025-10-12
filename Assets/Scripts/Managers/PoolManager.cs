using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PoolManager handles pooling of reusable components to optimize performance.
/// It pre-instantiates components and reuses them instead of creating/destroying frequently.
/// </summary>
[DisallowMultipleComponent]
public class PoolManager : SingletonMonobehaviour<PoolManager>
{
    #region === Serialized Fields ===

    [Tooltip("Populate this array with prefabs to be pooled. Each entry defines the prefab, its pool size, and the type of component to reuse.")]
    [SerializeField] private Pool[] poolArray = null;

    #endregion

    #region === Private Fields ===

    private Transform objectPoolTransform;
    private Dictionary<int, Queue<Component>> poolDictionary = new();

    [Serializable]
    public struct Pool
    {
        public int poolSize;             // Number of objects to pool
        public GameObject prefab;        // Prefab to instantiate
        public string componentType;     // Type of component to reuse (must match script class name)
    }

    #endregion

    #region === Unity Events ===

    private void Start()
    {
        objectPoolTransform = this.transform;

        // Create object pools for all defined prefabs
        for (int i = 0; i < poolArray.Length; i++)
            CreatePool(poolArray[i].prefab, poolArray[i].poolSize, poolArray[i].componentType);
    }

    #endregion

    #region === Pool Creation ===

    /// <summary>
    /// Creates a pool for the specified prefab with the given pool size and component type.
    /// </summary>
    private void CreatePool(GameObject prefab, int poolSize, string componentType)
    {
        int poolKey = prefab.GetInstanceID();
        string prefabName = prefab.name;

        // Tạo anchor object chứa pool
        GameObject parentObject = new(prefabName + "Anchor");
        parentObject.transform.SetParent(objectPoolTransform);

        if (!poolDictionary.ContainsKey(poolKey))
        {
            poolDictionary.Add(poolKey, new Queue<Component>());

            // ✅ Fallback componentType = Transform nếu trống
            if (string.IsNullOrEmpty(componentType))
                componentType = "Transform";

            // ✅ Hỗ trợ đầy đủ namespace
            Type componentTypeObj =
                Type.GetType(componentType)
                ?? Type.GetType($"UnityEngine.{componentType}")
                ?? Type.GetType($"PLAYERTWO.PlatformerProject.{componentType}");

            if (componentTypeObj == null)
            {
                Debug.LogWarning($"⚠️ PoolManager: Không tìm thấy componentType '{componentType}' cho prefab '{prefab.name}'. Dùng Transform mặc định.");
                componentTypeObj = typeof(Transform);
            }

            for (int i = 0; i < poolSize; i++)
            {
                GameObject newObject = Instantiate(prefab, parentObject.transform);
                newObject.SetActive(false);

                Component component = newObject.GetComponent(componentTypeObj);

                if (component == null)
                    // ✅ Nếu prefab không có componentType đó, dùng Transform luôn
                    component = newObject.transform;

                poolDictionary[poolKey].Enqueue(component);
            }

            Debug.Log($"✅ PoolManager: Khởi tạo {poolSize} '{componentType}' cho prefab '{prefab.name}'.");
        }
    }


    #endregion

    #region === Public API ===

    /// <summary>
    /// Retrieves a pooled component instance for reuse, resetting its transform.
    /// </summary>
    /// <param name="prefab">Prefab used as pool key.</param>
    /// <param name="position">Position to place the reused object.</param>
    /// <param name="rotation">Rotation of the reused object.</param>
    /// <returns>The reused Component if available, otherwise null.</returns>
    public Component ReuseComponent(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        int poolKey = prefab.GetInstanceID();

        if (poolDictionary.ContainsKey(poolKey))
        {
            Component reusedComponent = GetComponentFromPool(poolKey);
            ResetObject(reusedComponent, position, rotation, prefab);
            return reusedComponent;
        }

        return null;
    }

    #endregion

    #region === Internal Helpers ===

    /// <summary>
    /// Lấy component từ pool (dequeue, reset trạng thái vật lý, tắt gameObject).
    /// </summary>
    private Component GetComponentFromPool(int poolKey)
    {
        if (!poolDictionary.ContainsKey(poolKey) || poolDictionary[poolKey].Count == 0)
        {
            Debug.LogWarning($"⚠️ PoolManager: Pool rỗng hoặc chưa được khởi tạo cho key {poolKey}");
            return null;
        }

        Component component = poolDictionary[poolKey].Dequeue();
        poolDictionary[poolKey].Enqueue(component);

        // Reset trạng thái vật lý & collider
        ResetPhysicsAndCollider(component, resetCollider: true, enableCollider: false);

        // Deactivate để đảm bảo clean state
        component.gameObject.SetActive(false);
        return component;
    }

    /// <summary>
    /// Đặt lại vị trí, xoay, scale, và kích hoạt object lấy từ pool.
    /// </summary>
    private void ResetObject(Component component, Vector3 position, Quaternion rotation, GameObject prefab)
    {
        Transform t = component.transform;
        t.position = position;
        t.rotation = rotation;
        t.localScale = prefab.transform.localScale;

        // Reset vật lý và collider
        ResetPhysicsAndCollider(component, resetCollider: true, enableCollider: true);

        // Bật object
        component.gameObject.SetActive(true);
    }

    /// <summary>
    /// Reset velocity, angularVelocity, và bật/tắt collider.
    /// </summary>
    private void ResetPhysicsAndCollider(Component component, bool resetCollider, bool enableCollider)
    {
        // Reset Rigidbody nếu có
        if (component.TryGetComponent(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (resetCollider && component.TryGetComponent(out Collider col))
            col.enabled = enableCollider;
    }


    #endregion
}
