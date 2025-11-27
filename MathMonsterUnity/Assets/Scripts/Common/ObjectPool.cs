using System;
using System.Collections.Concurrent;

public interface IPoolObject {
    void reset();
}

public class ObjectPool {
    private readonly ConcurrentStack<IPoolObject> _pool = new ConcurrentStack<IPoolObject>();
    private readonly Func<IPoolObject> _objectFactory;

    public ObjectPool(Func<IPoolObject> objectFactory, int initialCapacity = 0) {
        _objectFactory = objectFactory ?? throw new ArgumentNullException(nameof(objectFactory));

        for (int i = 0; i < initialCapacity; i++) {
            _pool.Push(_objectFactory());
        }
    }

    public IPoolObject rentItem() {
        if (_pool.TryPop(out var item)) {
            return item;
        }

        return _objectFactory();
    }

    public void returnItem(IPoolObject item) {
        item.reset();
        _pool.Push(item);
    }

    public int count => _pool.Count;
}
 