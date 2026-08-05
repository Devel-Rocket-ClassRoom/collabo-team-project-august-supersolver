using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>();

                if (_instance == null)
                    Debug.LogError($"[{typeof(T).Name}] Instance를 찾을 수 없습니다. 접근하기 전에 Scene에 {typeof(T).Name}을(를) 배치하세요.");
            }

            return _instance;
        }
    }

    protected virtual void Awake()
    {
        T current = this as T;

        if (_instance == null)
            _instance = current;

        if (_instance == current)
        {
            DontDestroyOnLoad(gameObject);
            return;
        }

        Debug.LogWarning($"[{typeof(T).Name}] '{gameObject.name}'에서 중복 Instance가 감지되어 중복 오브젝트를 제거합니다.");
        Destroy(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
            _instance = null;
    }
}

