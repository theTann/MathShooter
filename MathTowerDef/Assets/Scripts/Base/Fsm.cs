using UnityEngine;

using System;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Reflection;


[AttributeUsage(AttributeTargets.Class)]
public class StateMappingAttribute : Attribute {
    public object Key { get; }
    public StateMappingAttribute(object key) => Key = key;
}

public interface IState<T> {
    void onEnter(T owner);
    void onExit(T owner);
    void onUpdate(T owner, float deltaTime);
}

public class Fsm<T, K> where K : Enum {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int toInt(K k) => Unsafe.As<K, int>(ref k);

    private IState<T>[] _states = new IState<T>[Enum.GetValues(typeof(K)).Length];
    private IState<T> _currentState;
    private K _currentStateEnum;
    private readonly T _owner;

    public Fsm(T owner) {
        _owner = owner;
        registerAllStates();
    }

    private void registerAllStates() {
        // todo : 최적화 - 어셈블리 전체를 뒤지는거.
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var selected = assemblies.SelectMany(a => a.GetTypes());
        var targetTypes = selected.Where(t => typeof(IState<T>).IsAssignableFrom(t) && !t.IsAbstract);

        foreach (var type in targetTypes) {
            var attr = type.GetCustomAttribute<StateMappingAttribute>();
            if (attr == null) {
                Debug.LogError($"{type.Name}에 StateKeyAttribute가 없습니다.");
                continue;
            }

            var key = (K)attr.Key;
            var instance = Activator.CreateInstance(type) as IState<T>;
            setState(key, instance);
        }
    }

    private void setState(K stateEnum, IState<T> state) {
        _states[toInt(stateEnum)] = state;
    }

    public K getCurrentState() {
        return _currentStateEnum;
    }

    public void changeState(K newStateEnum) {
        int idx = toInt(newStateEnum);
        var newState = _states[idx];
        transitionTo(newState, newStateEnum);
    }

    private void transitionTo(IState<T> newState, K newStateEnum) {
        _currentState?.onExit(_owner);
        _currentState = newState;
        _currentStateEnum = newStateEnum;
        _currentState?.onEnter(_owner);
    }

    public void update(float deltaTime) {
        _currentState?.onUpdate(_owner, deltaTime);
    }
}
