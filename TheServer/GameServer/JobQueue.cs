using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace GameServer;
public class JobQueue : IDisposable {
    private static long toDelayTicks(TimeSpan delay) => (long)(delay.TotalSeconds * _frequency);

    public struct PacketJobParam {
        public PacketBase packet;
        public Session session;
        public long sessionId;
        public long executeTick { get; private set; }
        
        public void setDealy(TimeSpan? delay) {
            long computeTick = Stopwatch.GetTimestamp() + toDelayTicks(delay ?? TimeSpan.Zero);
            this.executeTick = computeTick;
        }
    }

    // [StructLayout(LayoutKind.Explicit)]
    // public struct UnionParameter {
    //     [FieldOffset(0)] public Game game;
    // }
    
    public struct GameLogicJobParam {
        public GameLogicDispatcher.LogicType logicType;
        public object sender;
        
        public long executeTick { get; private set; }
        
        public void setDealy(TimeSpan? delay) {
            long computeTick = Stopwatch.GetTimestamp() + toDelayTicks(delay ?? TimeSpan.Zero);
            this.executeTick = computeTick;
        }
    }
    
    private readonly PriorityQueue<PacketJobParam, long> _packetQueue = new();
    private readonly PriorityQueue<GameLogicJobParam, long> _gameLogicJobQueue = new();
    
    private readonly Lock _lock = new Lock();
    private readonly SemaphoreSlim _jobSignal = new(0);
    private readonly CancellationTokenSource _cts = new();
    private readonly List<Thread> _workerThreads = new();
    private readonly PacketDispatcher _packetDispatcher = new();
    private readonly GameLogicDispatcher _gameLogicDispatcher = new();
    
    private static readonly long _frequency = Stopwatch.Frequency;

    public JobQueue(int workerThreadCount = 4) {
        for (int i = 0; i < workerThreadCount; i++) {
            var thread = new Thread(() => worker(_cts.Token)) {
                IsBackground = true, // main쓰레드가 종료시 자동종료된다함.
                Name = "GameLogic Thread",
                // Priority = ThreadPriority.Highest,
            };
            
            thread.Start();
            _workerThreads.Add(thread);
        }
    }
    
    public void Dispose() {
        _cts.Cancel();
        
        for (int i = 0; i < _workerThreads.Count; i++)
            _jobSignal.Release(); // 모든 Worker 깨움
        
        foreach (var t in _workerThreads)
            t.Join();

        _cts.Dispose();
        _jobSignal.Dispose();
    }

    public void touch() { }

    public void push(PacketJobParam packetJobParam, TimeSpan? delay = null) {
        packetJobParam.setDealy(delay);
        
        lock (_lock) {
            _packetQueue.Enqueue(packetJobParam, packetJobParam.executeTick);
        }
        
        _jobSignal.Release(); // 누적신호.
    }

    public void push(GameLogicJobParam logicJobParam, TimeSpan? delay = null) {
        logicJobParam.setDealy(delay);
        
        lock (_lock) {
            _gameLogicJobQueue.Enqueue(logicJobParam, logicJobParam.executeTick);
        }
        
        _jobSignal.Release(); // 누적신호.
    }
    
    private void worker(CancellationToken token) {
        while (!token.IsCancellationRequested) {
            PacketJobParam? packetJobToRun = null;
            GameLogicJobParam? gameLogicJobToRun = null;
            int waitMs = Timeout.Infinite;
            long now = Stopwatch.GetTimestamp();
            
            lock (_lock) {
                if (_packetQueue.Count > 0) {
                    var next = _packetQueue.Peek();
                    if (next.executeTick <= now) {
                        packetJobToRun = _packetQueue.Dequeue();
                    } else {
                        long waitTicks = next.executeTick - now;
                        waitMs = Math.Max((int)(waitTicks * 1000 / _frequency), 1);
                    }
                }
                
                if (_gameLogicJobQueue.Count > 0) {
                    var next = _gameLogicJobQueue.Peek();
                    if (next.executeTick <= now) {
                        gameLogicJobToRun = _gameLogicJobQueue.Dequeue();
                        // Console.WriteLine("poped");
                    } else {
                        long waitTicks = next.executeTick - now;
                        int gameLogicWaitMs = Math.Max((int)(waitTicks * 1000 / _frequency), 1);
                        if(waitMs == Timeout.Infinite || waitMs > gameLogicWaitMs)
                            waitMs = gameLogicWaitMs;
                    }
                }
            }

            bool needToContinue = false;
            if (packetJobToRun.HasValue) {
                needToContinue = true;
                try {
                    _packetDispatcher.onPacketReceive(packetJobToRun.Value);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Job error: {ex}");
                }
            }
            
            if (gameLogicJobToRun.HasValue) {
                needToContinue = true;
                
                try {
                    _gameLogicDispatcher.gameLogicRun(gameLogicJobToRun.Value);
                }
                catch (Exception ex) {
                    Console.WriteLine($"Job error: {ex}");
                }
            }

            if (needToContinue == true) {
                continue;
            }
            
            try {
                // Console.WriteLine($"going to sleep. {Thread.CurrentThread.ManagedThreadId} ({waitMs}ms), (logicCount : {_gameLogicJobQueue.Count}, packetCount : {_packetQueue.Count})");
                _jobSignal.Wait(waitMs);
                // Console.WriteLine($"thread wake up. {Thread.CurrentThread.ManagedThreadId}");
            }
            catch (OperationCanceledException) {
                break;
            }
        }
    }
}
