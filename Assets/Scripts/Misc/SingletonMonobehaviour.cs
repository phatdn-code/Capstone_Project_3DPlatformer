using UnityEngine;

public abstract class SingletonMonobehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T instance;

    public static T Instance
    {
        get
        {
            // Return cached instance if already initialized.
            if (instance != null)
                return instance;

            // Fallback: locate an existing instance in the scene.
            // This prevents null access when Instance is requested before Awake (e.g., OnEnable order).
            instance = FindFirstObjectByType<T>();

            return instance;
        }
    }

    protected virtual void Awake()
    {
        // First instance becomes the singleton.
        if (instance == null)
        {
            instance = this as T;
            return;
        }

        // If another instance exists, destroy this duplicate.
        if (instance != this)
            Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        // Clear the singleton reference when the active instance is destroyed.
        if (instance == this)
            instance = null;
    }
}
