using UnityEngine;
using System.Collections;

public class SingletonClass<T> where T : class, new()
{
    private static T _Singleton;
    protected SingletonClass()
    {
    }
    public static T Singleton
    {
        get
        {
            if (_Singleton == null)
            {
                _Singleton = new T();
            }
            return _Singleton as T;
        }
    }
    public static T Instance
    {
        get
        {
            if (_Singleton == null)
            {
                _Singleton = new T();
            }
            return _Singleton as T;
        }
    }
}
