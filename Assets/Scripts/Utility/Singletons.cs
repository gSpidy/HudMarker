using UnityEngine;

public class MonoBehaviourSingleton<T> : MonoBehaviour
    where T:MonoBehaviourSingleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = FindObjectOfType<T>();
                if (_instance) _instance.InitSingleton();
            }
            
            return _instance;
        }
    }

    protected virtual void InitSingleton() { }
}

public class ScriptableObjectSingleton<T> : ScriptableObject
    where T : ScriptableObjectSingleton<T>
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (!_instance)
            {
                _instance = Resources.Load<T>(typeof(T).Name);
                if (_instance) _instance.InitSingleton();
            }

            return _instance;
        }
    }

    protected virtual void InitSingleton(){}

    public static void ClearInstance()
    {
        _instance = null;
    }
}
