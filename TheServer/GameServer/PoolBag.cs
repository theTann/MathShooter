namespace GameServer;

public static class PoolBag {
    private const int _packetPoolSize = 10;
    
    public static readonly ObjectPool sessionPool = new ObjectPool(
        () => new Session(),
        initialCapacity: 10000
    );

    private static readonly ObjectPool bufferPool = new ObjectPool(
        () => new NetworkBuffer(1024 * 64, false),
        initialCapacity: 10000
    );

    private static readonly ObjectPool sessionListPool = new ObjectPool(
        () => new SessionList(),
        initialCapacity: 100
    );

    // todo : 자동화
    public static readonly Dictionary<Type, ObjectPool> packetPool = new () {
        { typeof(LoginRequest), new ObjectPool(() => new LoginRequest(), _packetPoolSize)},
        { typeof(LoginResponse), new ObjectPool(() => new LoginResponse(), _packetPoolSize)},
        { typeof(ReqCreateRoom), new ObjectPool(() => new ReqCreateRoom(), _packetPoolSize)},
        { typeof(ReqEnterRoom), new ObjectPool(() => new ReqEnterRoom(), _packetPoolSize)},
        { typeof(NtfEnterRoom), new ObjectPool(() => new NtfEnterRoom(), _packetPoolSize)},
        { typeof(NtfLeaveRoom), new ObjectPool(() => new NtfLeaveRoom(), _packetPoolSize)},
        { typeof(ReqLeaveRoom), new ObjectPool(() => new ReqLeaveRoom(), _packetPoolSize)},
        { typeof(ReqNoticeLoadingComplete), new ObjectPool(() => new ReqNoticeLoadingComplete(), _packetPoolSize)},
        { typeof(NtfGameStart), new ObjectPool(() => new NtfGameStart(), _packetPoolSize)},
        { typeof(ReqRemoveGem), new ObjectPool(() => new ReqRemoveGem(), _packetPoolSize)},
        { typeof(NtfRemoveGem), new ObjectPool(() => new NtfRemoveGem(), _packetPoolSize)},
        { typeof(NtfGameEnd), new ObjectPool(() => new NtfRemoveGem(), _packetPoolSize)}
    };

    public static SessionList rentSessionList() {
        return (SessionList)sessionListPool.rentItem();
    }

    public static void returnSessionList(SessionList sessionList) {
        sessionListPool.returnItem(sessionList);
    }
    
    public static T rentPacket<T>() where T : PacketBase {
        Type t = typeof(T);
        return (T)packetPool[t].rentItem();
    }
    
    public static void returnPacket(PacketBase packet) {
        Type type = packet.GetType();
        packetPool[type].returnItem(packet);
    }


    public static NetworkBuffer rentNetworkBuffer() {
        return (NetworkBuffer)bufferPool.rentItem();
    }

    public static void returnNetworkBuffer(NetworkBuffer buffer) {
        bufferPool.returnItem(buffer);
    }
}
