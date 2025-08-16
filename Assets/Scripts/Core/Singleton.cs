using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }
    private static bool _applicationIsQuitting = false;

    protected virtual void Awake()
    {
        if (_applicationIsQuitting) return;

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[Singleton] Duplicate instance of {typeof(T)} found, destroying.");
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            _applicationIsQuitting = true;
            Instance = null;
        }
    }
}