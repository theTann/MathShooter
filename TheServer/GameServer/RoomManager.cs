using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;

namespace GameServer;

public class RoomManager {
    private const int _roomCount = 50;

    private readonly ConcurrentStack<int> _idleRooms;
    private readonly List<Room> _rooms = new List<Room>(_roomCount);

    public RoomManager() {
        _idleRooms = new ConcurrentStack<int>();

        for (int i = 0; i < _roomCount; i++) {
            _rooms.Add(new Room(i));
            _idleRooms.Push(i);
        }
    }

    public Room getIdleRoom() {
        if (_idleRooms.TryPop(out int idx) == true)
            return _rooms[idx];

        Console.WriteLine("Room all in");
        idx = _rooms.Count;
        _rooms.Add(new Room(idx));
        return _rooms[idx];
    }

    public Room? getRoom(int id) {
        if (id < 0) return null;
        if (id >= _rooms.Count) return null;

        return _rooms[id];
    }

    public void pushIdleRoom(int id) {
        _idleRooms.Push(id);
    }

    public void startUpdateLoop(CancellationToken token) {
        Task.Run(() => updateLoop(token));

        int i = 0;
        Task.Run(async () => {
            for (int i = 0; i < _roomCount; i++) {
                _rooms[i].game.runUpdate = true;
                await Task.Delay(5);
            }
        });
    }

    private void updateLoop(CancellationToken token) {
        var options = new ParallelOptions {
            MaxDegreeOfParallelism = 8 // 최대 8개 스레드만 사용
        };
        
        while (!token.IsCancellationRequested) {
            long now = Stopwatch.GetTimestamp();
            int processCount = 0;
            Parallel.For(0, _roomCount, options, (i, state) => {
                bool processed = _rooms[i].game.tryUpdate(now);
                if (processed) Interlocked.Increment(ref processCount);
            });
            
            // for profile
            {
                long afterUpdate = Stopwatch.GetTimestamp();
                long elapsedTick = afterUpdate - now;
                double updateTime = elapsedTick * Game.tickToMs;
                Console.WriteLine($"global update : {updateTime}ms, processCount:{processCount}");
            }
        }
    }
}
