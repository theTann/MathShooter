namespace GameServer;

public class PacketDispatcher {
    private delegate void PacketHandler(Session session, PacketBase packet);
    private readonly Dictionary<Type, PacketHandler> _packetHandlers;

    public PacketDispatcher() {
        _packetHandlers = new Dictionary<Type, PacketHandler>() {
            { typeof(LoginRequest), onLoginRequest },
            { typeof(ReqCreateRoom), onReqCreateRoom },
            { typeof(ReqEnterRoom), onReqEnterRoom },
            { typeof(ReqLeaveRoom), onReqLeaveRoom },
            { typeof(ReqNoticeLoadingComplete), onReqNoticeLoadingComplete },
            { typeof(ReqRemoveGem), onReqRemoveGem },
        };
    }

    public void onPacketReceive(JobQueue.PacketJobParam packetJobParameter) {
        using PacketBase requestPacket = packetJobParameter.packet;
        Session session = packetJobParameter.session;
            
        long exceptSessionId = packetJobParameter.sessionId;

        try {
            if (exceptSessionId != session.getSessionId()) {
                Console.WriteLine($"session changed");
                return;
            }

            var packetType = requestPacket.GetType();
            Console.WriteLine($"{packetType} arrived.(session :{session.getSessionId()})");
        
            _packetHandlers.TryGetValue(packetType, out PacketHandler? handler);
            handler?.Invoke(session, requestPacket);    
        } catch (Exception e) {
            Console.WriteLine(e);
        }    
    }

    private void onLoginRequest(Session session, PacketBase packet) {
        var req = (LoginRequest)packet;
        
        using LoginResponse res = PoolBag.rentPacket<LoginResponse>();
        
        Console.WriteLine($"jwtToken : {req.jwtToken}");
        res.success = true;
        res.id = session.getSessionId();
        session.sendPacket(res);
    }
    
    private void onReqCreateRoom(Session session, PacketBase packet) {
        var req = (ReqCreateRoom)packet;
        
        Room room = GameServer.roomManager.getIdleRoom();
        room.enter(session);
    }
    
    private void onReqEnterRoom(Session session, PacketBase packet) {
        var req = (ReqEnterRoom)packet;
        
        Room? room = GameServer.roomManager.getRoom(req.roomId);
        room?.enter(session);
    }
    
    private void onReqLeaveRoom(Session session, PacketBase packet) {
        var req = (ReqLeaveRoom)packet;
        
        Room? room = session.room;
        if (room == null) {
            Console.Error.WriteLine($"room not exist.");
            return;
        }
        
        room.leave(session);
    }

    private void onReqNoticeLoadingComplete(Session session, PacketBase packet) {
        var req = (ReqNoticeLoadingComplete)packet;
        Room? room = session.room;
        
        if (room == null) {
            // todo : 어떻게 해야되나.
            return;
        }
        Game game = room.game;

        using var ntfGameStart = PoolBag.rentPacket<NtfGameStart>();
        DateTime startTime = DateTime.UtcNow.AddSeconds(5);
        ntfGameStart.startTime = startTime;
        
        lock (game) {
            bool everybodyLoadComplete = game.onLoadComplete(session);
            if (everybodyLoadComplete == false)
                return;
            
            ntfGameStart.board = game.getCurrentBoard();
        }

        var jobParameter = new JobQueue.GameLogicJobParam() {
            logicType = GameLogicDispatcher.LogicType.startGame,
            sender = game,
        };
        GameServer.jobQueue.push(jobParameter, TimeSpan.FromSeconds(5));
        
        
        room.broadcast(ntfGameStart);
    }

    private void onReqRemoveGem(Session session, PacketBase packet) {
        var req = (ReqRemoveGem)packet;
        Room? room = session.room;
        if (room == null) {
            // todo : 어떻게 해야되나.
            Console.Error.WriteLine("room not exist");
            return;
        }
        
        Game game = room.game;
        int score = -1;
        lock (game) {
            score = game.removeGems(session, req.toRemove);
        }

        if (score != -1) { 
            long sessionId = session.getSessionId();
            Console.WriteLine($"player({sessionId}) attack!. score({score})");
            using NtfRemoveGem ntfRemoveGem = PoolBag.rentPacket<NtfRemoveGem>();
            ntfRemoveGem.toRemove.AddRange(req.toRemove);
            ntfRemoveGem.removePlayer = session.getSessionId();
            ntfRemoveGem.newScore = score;
            room.broadcast(ntfRemoveGem);
        }
    }
}
