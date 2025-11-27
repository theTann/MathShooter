using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Base;
using NetworkGame;
using MemoryPack;

[MemoryPackable]
[Serializable]
public partial class PacketBase : IPoolObject {
    public virtual void reset() { }
}

public class User {
    private readonly StringBuilder _sb = new StringBuilder(1024);
    
    private Session _session;
    public long sessionId = -1;
    public int roomId = -1;

    private readonly List<long> _players = new List<long>();
    
    public NetworkManager manager;
    private readonly Dictionary<Type, Action<PacketBase>> _packetHandlers;

    public  BoardUI boardUI;
    
    public User() {
        _packetHandlers = new Dictionary<Type, Action<PacketBase>> {
            {typeof(LoginResponse), onLoginResponse},
            {typeof(NtfEnterRoom), onNtfEnterRoom},
            {typeof(NtfLeaveRoom), onNtfLeaveRoom},
            {typeof(NtfGameStart), onNtfGameStart},
            {typeof(NtfRemoveGem), onNtfRemoveGem },
        };
    }

    public async Awaitable connect() {
        _session = new Session();
        _session.setPacketReceiveCallback(onPacketReceive);
        await _session.connect("127.0.0.1", 7777);
        manager.dirtyText = true;
    }

    public void onDestroy() {
        _session?.doDisconnect();
    }

    public async Awaitable sendPacket(PacketBase packet) {
        await _session.sendPacket(packet);
    }

    private void onLoginResponse(PacketBase packet) {
        var res = (LoginResponse)packet;
        sessionId = res.id;
    }

    private void onNtfEnterRoom(PacketBase obj) {
        var res = (NtfEnterRoom)obj;
        roomId = res.roomId;
        _players.Clear();
        _players.AddRange(res.players);
    }
    
    private void onNtfLeaveRoom(PacketBase obj) {
        var res = (NtfLeaveRoom)obj;
        if (sessionId == res.leavePlayer) {
            roomId = -1;
            _players.Clear();
            return;
        }
        _players.Clear();
        _players.AddRange(res.players);
    }

    private void onNtfGameStart(PacketBase packet) {
        var ntf =  (NtfGameStart)packet;
        if (ntf.board == null) {
            Debug.LogError($"board is null");
        }
        
        _ = startGame(ntf);
    }

    private void onNtfRemoveGem(PacketBase packet) {
        var ntf = (NtfRemoveGem)packet;
        boardUI.removeGems(ntf.toRemove);
    }

    private async Awaitable startGame(NtfGameStart startPacket) {
        DateTime startTime = startPacket.startTime;
        boardUI.setGems(startPacket.board);
        boardUI.setEnable(false);

        var remainTime = startTime - DateTime.UtcNow;
        int remainSec = remainTime.Seconds;
        
        Debug.Log($"remainSec: {remainSec}");
        
        if (remainSec > 0) {
            await Awaitable.WaitForSecondsAsync(remainSec);
        }
        boardUI.setEnable(true);
    }

    private void onPacketReceive(PacketBase packet) {
        var packetType = packet.GetType();
        Debug.Log($"packet receive : {packetType}");
        _packetHandlers[packetType](packet);
        manager.dirtyText = true;
    }
    
    public void addText(StringBuilder sb) {
        string sessionIdStr = sessionId != -1 ? $" ({sessionId.ToString()})" : null;
        sb.AppendLine($"session : {_session.getState()}{sessionIdStr}");
        sb.AppendLine($"room : {roomId.ToString()}");

        string playerStr = "";
        for (int i = 0; i < _players.Count; i++) {
            if(i == 0)
                playerStr += _players[i].ToString();
            else
                playerStr += $", {_players[i]}";
        }
        sb.AppendLine($"players : {playerStr}");
        sb.AppendLine($"");
        sb.AppendLine($"=============================================");
    }
}

public class NetworkManager : MonoBehaviour {
    [SerializeField] private TMP_Text _text;
    public static readonly List<User> _users = new List<User>();

    public bool dirtyText = false;
    private readonly StringBuilder _sb = new StringBuilder(1024);

    [SerializeField] private BoardUI board0;
    [SerializeField] private BoardUI board1;

    private void Awake() {
        try {
            _users.Add(new User());
            _users.Add(new User());
            _users[0].manager = this;
            _users[1].manager = this;
            _users[0].boardUI = board0;
            _users[1].boardUI = board1;
        }
        catch (Exception e) {
            Debug.LogError(e);
        }
    }

    private void Update() {
        if (dirtyText) {
            refreshText();
            dirtyText = false;
        }
    }

    private void OnDestroy() {
        foreach (var user in _users) {
            user.onDestroy();
        }
    }

    public async void onBtnClick() {
        try {
            var go = EventSystem.current.currentSelectedGameObject;
            if (go == null)
                return;

            var clickedButton = go?.GetComponent<Button>();
            if (clickedButton == null)
                return;

            string clickedButtonName = clickedButton.name;
            int start = clickedButtonName.LastIndexOf('(');
            int end = clickedButtonName.LastIndexOf(')');
            string indexStr = clickedButtonName.Substring(start + 1, end - start - 1);
            if (int.TryParse(indexStr, out int index) == false) {
                return;
            }

            switch (index) {
                case 1: {
                    {
                        Awaitable[] awaits = {
                            _users[0].connect(), 
                            _users[1].connect(),
                        };
                        await Utils.awaitAll(awaits);
                    }
                    {
                        var loginRequest = new LoginRequest { jwtToken = "tempToken" };
                        _ = _users[0].sendPacket(loginRequest);
                        _ = _users[1].sendPacket(loginRequest);
                        await Utils.until(() => _users[0].sessionId != -1 && _users[1].sessionId != -1);
                    }
                    
                    {
                        var request = new ReqCreateRoom { };
                        await _users[0].sendPacket(request);
                        await Utils.until(() => _users[0].roomId != -1);
                    }
                    
                    {
                        var req = new ReqEnterRoom { roomId = _users[0].roomId, };
                        await _users[1].sendPacket(req);
                        await Utils.until(() => _users[1].roomId != -1);
                    }
                    
                    {
                        var req = new ReqNoticeLoadingComplete { };
                        Awaitable[] awaits = { _users[1].sendPacket(req), _users[0].sendPacket(req), };
                        await Base.Utils.awaitAll(awaits);
                    }
                    break;
                }
                case 2: {
                    break;
                }
                case 3: {
                    break;
                }
                case 4: {
                    break;
                }
                case 5: {
                    // var req = new ReqLeaveRoom { };
                    // _ = _users[0].sendPacket(req);
                    break;
                }
                case 6: {
                    // var req = new ReqLeaveRoom { };
                    // _ = _users[1].sendPacket(req);
                    break;
                }
            }
        } catch (Exception e) {
            Debug.LogError(e);
        }
    }

    private void refreshText() {
        _sb.Clear();
        foreach (var user in _users) {
            user.addText(_sb);
        }
        _text.text = _sb.ToString();
    }
}
