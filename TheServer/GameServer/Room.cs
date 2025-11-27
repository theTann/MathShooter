using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer;
public class Room(int id) {
    private const int _maxPlayerCount = 2;
    public int roomId { get; } = id;
    private readonly List<Session> _sessions = [];
    private readonly Lock _roomLock = new Lock();

    public readonly Game game = new();
    
    public void enter(Session session) {
        using SessionList sessions = PoolBag.rentSessionList();
        using NtfEnterRoom res = PoolBag.rentPacket<NtfEnterRoom>();
        
        lock (_roomLock) {
            if (_sessions.Count >= _maxPlayerCount) {
                // 꽉찬방 누가 또 입장했냐.
                res.roomId = -1;
                return;
            } else {
                res.roomId = roomId;
                _sessions.Add(session);
                session.room = this;
                foreach (var player in _sessions) {
                    sessions.Add(player);
                    res.players.Add(player.getSessionId());
                }
            }
        }

        broadcast(sessions, res);
        
        if (sessions.Count == _maxPlayerCount) {
            lock (game) {
                game.init(sessions);
            }
        }
        
        Console.WriteLine($"{session.getSessionId()} enter room.(room : {roomId}, {sessions.Count} count)");
    }

    public void leave(Session session) {
        using SessionList broadcastList = PoolBag.rentSessionList();
        using NtfLeaveRoom res = PoolBag.rentPacket<NtfLeaveRoom>();
        
        res.roomId = roomId;
        
        lock (_roomLock) {
            for (int i = _sessions.Count - 1; i >= 0; i--) {
                Session s = _sessions[i];
                long sessionId = s.getSessionId();
                
                // disconnect로 leave가 불리는 경우가 있음.
                // 정상적으로 leave가 된 경우에는 NtfLeaveRoom을 보내주기 위함.
                if(session.isConnected() == true)
                    broadcastList.Add(s);
                
                if (s == session) {
                    // todo : swap remove
                    _sessions.RemoveAt(i);
                    res.leavePlayer = sessionId;
                } else {
                    res.players.Add(sessionId);
                }
            }
            session.room = null;
            if (_sessions.Count == 0) {
                Console.WriteLine($"{roomId} room returned");
                GameServer.roomManager.pushIdleRoom(roomId);
            }
        }

        lock (game) {
            game.onLeavePlayer(session);
        }
        broadcast(broadcastList, res);
    }

    public void broadcast(PacketBase packet) {
        using SessionList sessions = PoolBag.rentSessionList();
        lock (_roomLock) {
            sessions.AddRange(_sessions);
        }
        broadcast(sessions, packet);
    }

    private void broadcast(SessionList sessions, PacketBase packet) {
        if (sessions.Count <= 0)
            return;
        
        NetworkBuffer buffer = PoolBag.rentNetworkBuffer();
        PacketSerializer.serializePacket(packet, buffer);
        buffer.addRefCount();
        
        foreach (var s in sessions) {
            s.sendNetworkBuffer(buffer);
        }

        if (buffer.decRefCount() == 0) {
            // Console.WriteLine($"return buffer on broadcast");
            PoolBag.returnNetworkBuffer(buffer);
        }
    }

    // public void loadComplete(Session session) {
    //     using var sessions = PoolBag.rentSessionList();
    //     using var ntfGameStart = PoolBag.rentPacket<NtfGameStart>();
    //     
    //     lock (_roomLock) {
    //         bool everybodyLoadComplete = game.onLoadComplete(session);
    //         if (everybodyLoadComplete == false)
    //             return;
    //         
    //         sessions.AddRange(_sessions);
    //         DateTime startTime = DateTime.UtcNow.AddSeconds(5);
    //         ntfGameStart.board = game.getCurrentBoard();
    //         ntfGameStart.startTime = startTime;
    //         Console.WriteLine($"gameStart!(room:{roomId})");
    //         game.startGame(startTime);
    //     }
    //     broadcast(sessions, ntfGameStart);
    // }
}
