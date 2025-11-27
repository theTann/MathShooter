using System.Net;
using System.Net.Sockets;

namespace GameServer;
public class GameServer {
    private readonly IPEndPoint _endPoint;
    private readonly Socket _listenSocket;
    private readonly SocketAsyncEventArgs _acceptEventArg;
    
    public static int currentSessionCount;
    public static readonly JobQueue jobQueue = new JobQueue();
    public static readonly RoomManager roomManager = new RoomManager();

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    
    public static void touch() {
        Console.WriteLine("GameServer touched");
    }
    
    public GameServer() {
        _endPoint = new IPEndPoint(IPAddress.Any, 7777);
        _listenSocket = new Socket(_endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        _listenSocket.Bind(_endPoint);
        _acceptEventArg = new SocketAsyncEventArgs();
        
        jobQueue.touch();
    }

    public void start(int backlog) {
        // PacketSerializer는 클라랑 같이 써서 이렇게 함.
        PacketSerializer.packetPools = PoolBag.packetPool;
        
        roomManager.startUpdateLoop(_cancellationTokenSource.Token);
        
        _listenSocket.Listen(backlog);

        Console.WriteLine($"Server started on {_endPoint}");
        _acceptEventArg.Completed += onAcceptCompleted;
        
        registerAccept();
    }

    void registerAccept() {
        _acceptEventArg.AcceptSocket = null;
        bool pending = _listenSocket.AcceptAsync(_acceptEventArg);
        if (!pending)
            onAcceptCompleted(null, _acceptEventArg);
    }

    void onAcceptCompleted(object? sender, SocketAsyncEventArgs args) {
        if (args.SocketError == SocketError.Success && args.AcceptSocket != null) {
            Interlocked.Increment(ref currentSessionCount);
            Socket acceptedSocket = args.AcceptSocket;
            Session session = (Session)PoolBag.sessionPool.rentItem();
            session.bindSocket(acceptedSocket);
            session.start();
            Console.WriteLine($"Client connected: {args.AcceptSocket.RemoteEndPoint} (session hash : {session.GetHashCode()}, sessionId : {session.getSessionId()})");
        }
        else {
            Console.WriteLine($"Accept Failed: {args.SocketError}");
        }
        
        // 다음 Accept 대기
        registerAccept();
    }
}
