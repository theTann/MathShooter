using System.Net.Sockets;

namespace GameServer;

public class SessionList : List<Session>, IPoolObject, IDisposable  {
    public void reset() {
        Clear();
    }

    public void Dispose() {
        // Console.WriteLine("session list return");
        PoolBag.returnSessionList(this);
    }
}

public class Session : IPoolObject {
    public enum DisconnectReason {
        byClient,
        socketClosed,
        packetParsingError,
        deserializeException,
        sendFail,
    }
    
    private static long _globalSessionCounter = 0;
    
    private enum State {
        disconnected,
        connected,
    }
    
    public Room? room { get; set; }
    
    private Socket? _socket;
    private State _state = State.disconnected;
    
    private readonly NetworkBuffer _receiveBuffer;
    // private readonly NetworkBuffer _sendBuffer;
    private readonly SocketAsyncEventArgs _receiveArgs;
    private readonly SocketAsyncEventArgs _sendArgs;
    private readonly Queue<NetworkBuffer> _sendQueue = new();
    private readonly Lock _sendQueueLock = new Lock();
    private readonly Lock _sessionStateLock = new Lock();
    private bool _sending;

    private long _sessionId;
    public long getSessionId() {
        return _sessionId;
    }
    
    public Session() {
        _receiveBuffer = new NetworkBuffer(1024 * 64);
        // _sendBuffer = new NetworkBuffer(1024 * 64);
        
        _receiveArgs = new SocketAsyncEventArgs();
        _receiveArgs.Completed += onReceiveCompleted;
        _receiveArgs.SetBuffer(_receiveBuffer.getBuffer());

        _sendArgs = new SocketAsyncEventArgs();
        _sendArgs.Completed += onSendCompleted;
    }
    
    public void reset() {
        
    }

    public bool isConnected() {
        lock (_sessionStateLock) {
            return _state == State.connected;    
        }
    }
    
    private void doDisconnect(DisconnectReason reason) {
        // 연결 끊김 처리
        Interlocked.Decrement(ref GameServer.currentSessionCount);
        lock (_sessionStateLock) {
            if (_socket == null)
                return;
        
            _socket.Close();
            _socket = null;
            _state = State.disconnected;
        }
        
        Console.WriteLine($"Client disconnected (reason : {reason} session hash : {GetHashCode()}, sessionId : {getSessionId()})");

        room?.leave(this);
        room = null;
        
        _receiveBuffer.reset();
        // _sendBuffer.reset();
        
        lock (_sendQueueLock) {
            foreach (var networkBuffer in _sendQueue) {
                PoolBag.returnNetworkBuffer(networkBuffer);
            }
            _sendQueue.Clear();
        }
        _sending = false;
        
        PoolBag.sessionPool.returnItem(this);
    }

    public void bindSocket(Socket socket) {
        lock (_sessionStateLock) {
            _state = State.connected;
            _socket = socket;
            _sessionId = Interlocked.Increment(ref _globalSessionCounter); // 고유 ID 갱신
        }
    }

    public void start() {
        registerReceive();
    }

    private void registerReceive() {
        if (_socket == null) {
            doDisconnect(DisconnectReason.socketClosed);
            return;
        }

        _receiveArgs.SetBuffer(_receiveBuffer.getBuffer());
        bool pending = _socket.ReceiveAsync(_receiveArgs);
        if (!pending)
            onReceiveCompleted(null, _receiveArgs);
    }

    private void onReceiveCompleted(object? sender, SocketAsyncEventArgs args) {
        if(args.BytesTransferred <= 0 || args.SocketError != SocketError.Success) {
            doDisconnect(DisconnectReason.byClient);
            return;
        }

        _receiveBuffer.written(args.BytesTransferred);

        do {
            try {
                PacketBase? packet = PacketSerializer.deserializePacket(_receiveBuffer);
                if (packet == null) {
                    doDisconnect(DisconnectReason.packetParsingError);
                    return;
                }

                GameServer.jobQueue.push(new JobQueue.PacketJobParam() {
                    packet = packet, session = this, sessionId = _sessionId,
                });
            } catch (Exception ex) {
                Console.WriteLine($"deserialize exception: {ex}");
                doDisconnect(DisconnectReason.deserializeException);
                return;
            }
        } while (false);

        // 계속 다음 수신 대기
        registerReceive();
    }
    
    public void sendPacket(PacketBase packet) {
        if (isConnected() == false)
            return;
        
        NetworkBuffer buffer = PoolBag.rentNetworkBuffer();
        PacketSerializer.serializePacket(packet, buffer);
        sendNetworkBuffer(buffer);
    }

    public void sendNetworkBuffer(NetworkBuffer buffer) {
        if (isConnected() == false)
            return;
        
        lock (_sendQueueLock) {
            buffer.addRefCount();
            _sendQueue.Enqueue(buffer);
        }
    
        if (_sending == false) {
            registerSend();
        }
    }
    
    void registerSend() {
        lock (_sessionStateLock) {
            if (_state == State.disconnected) {
                return;
            }
            
            // nullable이라서 넣음.
            if (_socket == null)
                return;
        }

        NetworkBuffer? buffer = null;
        lock (_sendQueueLock) {
            if (_sendQueue.Count == 0) {
                _sending = false;
                return;
            }
            buffer = _sendQueue.Dequeue();
        }
        _sending = true;
        Memory<byte> memoryBuffer = buffer.peekBuffer();
        _sendArgs.SetBuffer(memoryBuffer);
        _sendArgs.UserToken = buffer;
        // Console.WriteLine($"send request (session : {_sessionId})");
        bool pending = _socket.SendAsync(_sendArgs);
        if (!pending)
            onSendCompleted(null, _sendArgs);
    }
    
    private void onSendCompleted(object? sender, SocketAsyncEventArgs args) {
        NetworkBuffer buffer = (NetworkBuffer)args.UserToken!;
        // Console.WriteLine($"send complete. (session : {_sessionId})");
        if (args.SocketError == SocketError.Success) {
            // todo : 덜보내는 케이스가 send는 없다는 썰이 있는데 만약 있다면?
            if (args.BytesTransferred != buffer.getWrittenSize()) {
                Console.Error.WriteLine("send error. sent size wrong.");
            }
            
            int count = buffer.decRefCount();
            if (count == 0) {
                // Console.WriteLine($"return buffer on sendComplete");
                PoolBag.returnNetworkBuffer(buffer);
            }
            // 다음 보낼 것이 있으면 이어서 전송
            registerSend();
        }
        else {
            Console.Error.WriteLine("Send Failed");
            doDisconnect(DisconnectReason.sendFail);
        }
    }
}
