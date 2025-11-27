using System;
using UnityEngine;

public abstract class Singleton<T> where T : class, new() {
    private static readonly Lazy<T> _instance = new Lazy<T>(() => new T());
    public static T instance => _instance.Value;
}

public class MonoSingleton<T> : MonoBehaviourBase where T : MonoBehaviourBase {
    public static T instance { get; private set; }

    protected override void Awake() {
        base.Awake();

        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }

        instance = this as T;
        DontDestroyOnLoad(gameObject);
    }
}
