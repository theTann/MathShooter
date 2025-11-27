using System.Collections.Generic;

public interface IGameEventData {
    GameEventType eventType { get; }
}

public delegate void GameEventHandler<T>(ref T data) where T : struct, IGameEventData;

public class GameEventBus {
    private static readonly IEventDispatcher[] _dispatchers = new IEventDispatcher[(int)GameEventType.max];
    private static readonly IEventDispatcher[] _preProcessors = new IEventDispatcher[(int)GameEventType.max];

    public static void registerPreProcess<T>(GameEventHandler<T> listener) where T : struct, IGameEventData {
        T t = default(T);
        int idx = (int)t.eventType;

        EventDispatcher<T> preProcessor = _preProcessors[idx] as EventDispatcher<T>;

        if (preProcessor == null) { // is not EventDispatcher<T> dispatcher) {
            preProcessor = new EventDispatcher<T>();
            _preProcessors[idx] = preProcessor;
        }
        preProcessor.register(listener);
    }

    public static void unregisterPreProcess<T>(GameEventHandler<T> listener) where T : struct, IGameEventData {
        int idx = (int)default(T).eventType;
        var dispatcher = _preProcessors[idx] as EventDispatcher<T>;
        dispatcher.unregister(listener);
    }
    
    public static void register<T>(GameEventHandler<T> listener) where T : struct, IGameEventData {
        T t = default(T);
        int idx = (int)t.eventType;

        EventDispatcher<T> dispatcher = _dispatchers[idx] as EventDispatcher<T>;

        if (dispatcher == null) { // is not EventDispatcher<T> dispatcher) {
            dispatcher = new EventDispatcher<T>();
            _dispatchers[idx] = dispatcher;
        }
        dispatcher.register(listener);
    }

    // todo : register하는 건 필요한 이벤트를 구독하는 것이니 자연스러운데 unregister는 불편함. 자동화 할 필요있음.
    public static void unregister<T>(GameEventHandler<T> listener) where T : struct, IGameEventData {
        int idx = (int)default(T).eventType;
        var dispatcher = _dispatchers[idx] as EventDispatcher<T>;
        dispatcher.unregister(listener);
    }

    public static void broadcast<T>(ref T data) where T : struct, IGameEventData {
        int idx = (int)data.eventType;
        var dispatcher = _dispatchers[idx] as EventDispatcher<T>;
        var preProcessor = _preProcessors[idx] as EventDispatcher<T>;
        preProcessor?.broadcast(ref data);
        dispatcher?.broadcast(ref data);
    }

    public static void multicast<T>(ref T data, List<GameEventHandler<T>> eventReceiver) where T : struct, IGameEventData {
        int idx = (int)data.eventType;
        var preProcessor = _preProcessors[idx] as EventDispatcher<T>;
        preProcessor?.broadcast(ref data);
        foreach (var receiver in eventReceiver) {
            receiver(ref data);
        }
    }

    private interface IEventDispatcher { }

    private class EventDispatcher<T> : IEventDispatcher where T : struct, IGameEventData {
        private readonly List<GameEventHandler<T>> _listeners = new();

        public void register(GameEventHandler<T> listener) {
            _listeners.Add(listener);
        }

        public void unregister(GameEventHandler<T> listener) {
            for (int i = 0; i < _listeners.Count; i++) {
                if (_listeners[i] == listener) {
                    _listeners.swapRemoveAt(i);
                    return;
                }
            }
        }

        public void broadcast(ref T data) {
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i](ref data);
        }
    }
}
