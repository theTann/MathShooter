using NetworkGame;
using System;
using System.Collections.Generic;

using TMPro;
using UnityEngine;

using Base;
using System.Threading.Tasks;

public class MultiGameManager : MonoBehaviour
{
    [SerializeField] TMP_Text _infoTxt;

    [SerializeField] TMP_InputField _serverAddress;
    [SerializeField] GameObject _connectPanel;

    [SerializeField] GameObject _gamePanel;
    [SerializeField] TMP_Text _scoreTxt;
    [SerializeField] BoardUI _boardUI;

    [SerializeField] GameObject _lobbyPanel;
    [SerializeField] TMP_InputField _inputField;
    
    private Session _session;
    private long sessionId = -1;
    private int roomId = -1;
    private readonly List<long> _players = new List<long>();

    private readonly Dictionary<Type, Action<PacketBase>> _packetHandlers;

    int _myScore = 0;
    int _yourScore = 0;

    public MultiGameManager() {
        _packetHandlers = new Dictionary<Type, Action<PacketBase>> {
            {typeof(LoginResponse), onLoginResponse},
            {typeof(NtfEnterRoom), onNtfEnterRoom},
            {typeof(NtfLeaveRoom), onNtfLeaveRoom},
            {typeof(NtfGameStart), onNtfGameStart},
            {typeof(NtfRemoveGem), onNtfRemoveGem },
        };
        _session = new Session();
        _session.setPacketReceiveCallback(onPacketReceive);
        _session.setDisconnectAction(onDisconnect);
    }

    void Start()
    {
        try {
            _boardUI.onSelectGems = onGemRemove;
            _connectPanel.SetActive(true);
            _lobbyPanel.SetActive(false);
            _gamePanel.SetActive(false);
            _infoTxt.gameObject.SetActive(true);

            //_gamePanel.SetActive(true);
            //_connectPanel.SetActive(false);
            //_infoTxt.gameObject.SetActive(false);
            //_boardUI.setEnable(true);

        }
        catch (Exception e) {
            Debug.Log(e);
        }
    }

    private void OnDestroy() {
        _session.doDisconnect();
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
        if (_players.Count == 2) {
            var req = new ReqNoticeLoadingComplete { };
            _ = sendPacket(req);
        }
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

    private async void onNtfGameStart(PacketBase packet) {
        Debug.Log("onNtfGameStartBegin");
        var ntf = (NtfGameStart)packet;
        if (ntf.board == null) {
            Debug.LogError($"board is null");
        }
        Debug.Log("start game before");
        await startGame(ntf);
    }

    private void onNtfRemoveGem(PacketBase packet) {
        var ntf = (NtfRemoveGem)packet;
        _boardUI.removeGems(ntf.toRemove);
        if(ntf.removePlayer == sessionId) {
            _myScore = ntf.newScore;
        }
        else {
            _yourScore = ntf.newScore;
        }

        refreshScore();
    }

    void refreshScore() {
        _scoreTxt.text = $"you : {_myScore}, enemy : {_yourScore}";
    }

    private async Awaitable startGame(NtfGameStart startPacket) {
        DateTime startTime = startPacket.startTime;
        
        _boardUI.setEnable(false);
        Debug.Log("while before");
        while (true) {
            DateTime now = DateTime.UtcNow;
            if (startTime < now) break;

            var remainTime = startTime - DateTime.UtcNow;
            int remainSec = remainTime.Seconds;

            _infoTxt.text = $"{remainSec} sec remain.";
            await Awaitable.NextFrameAsync();
        }
        _boardUI.setGems(startPacket.board);
        _infoTxt.text = $"Start!";
        await Awaitable.WaitForSecondsAsync(0.6f);
        _infoTxt.gameObject.SetActive(false);
        _boardUI.setEnable(true);
    }

    private void onPacketReceive(PacketBase packet) {
        var packetType = packet.GetType();
        Debug.Log($"packet receive : {packetType}");
        _packetHandlers[packetType](packet);
    }

    private void onDisconnect() {
        sessionId = -1;
        roomId = -1;
        _myScore = 0;
        _yourScore = 0;
        if(_gamePanel != null) _gamePanel.SetActive(false);
        if(_connectPanel != null) _connectPanel.SetActive(true);
        if (_infoTxt != null) {
            _infoTxt.gameObject.SetActive(true);
            _infoTxt.SetText("diconnected");
        }
    }

    public async void onConnectBtn() {
        try {
            string address = _serverAddress.text;
            await _session.connect(address, 7777);
            if(_session.getState() != Session.State.connected) {
                _infoTxt.text = $"connect fail!";
                return;
            }
            var loginRequest = new LoginRequest { jwtToken = "tempToken" };
            await sendPacket(loginRequest);
            bool isTimeout = await Utils.until(() => sessionId != -1, 10.0f);
            if(isTimeout == false) {
                _infoTxt.text = "login fail!";
                return;
            }
            _connectPanel.SetActive(false);
            _lobbyPanel.SetActive(true);
        }
        catch (Exception e) {
            Debug.LogError(e);
        }
    }

    public async void onCreateRoomBtn() {
        try {
            var request = new ReqCreateRoom { };
            await sendPacket(request);
            bool isTimeout = await Utils.until(() => roomId != -1, 10.0f);
            if(isTimeout == false) {
                _infoTxt.text = "create room fail.";
                return;
            }

            _infoTxt.text = $"your room id : {roomId}";

            await onEnterRoom();
        }
        catch (Exception e) {
            Debug.LogError(e);
        }
    }
    
    public async void onJoinRoomBtn() {
        try {
            if(int.TryParse(_inputField.text, out int inputRoomId) == false) {
                _infoTxt.text = "invalid room number";
                return;
            }

            var req = new ReqEnterRoom { roomId = inputRoomId, };
            await sendPacket(req);
            bool isTimeout = await Utils.until(() => roomId != -1, 10.0f);
            if(isTimeout == false) {
                _infoTxt.text = "enter room fail";
                return;
            }

            await onEnterRoom();
        }
        catch (Exception e) {
            Debug.LogError(e);
        }
    }

    public async Awaitable onEnterRoom() {
        _lobbyPanel.SetActive(false);
        _gamePanel.SetActive(true);
        await Task.CompletedTask;
    }

    public async Awaitable onGemRemove(List<Gem> removeGems) {
        int sum = 0;

        foreach (var gem in removeGems) {
            sum += gem.value;
        }

        if (sum == 10) {
            ReqRemoveGem req = new ReqRemoveGem();
            foreach (var gem in removeGems) {
                req.toRemove.Add((byte)gem.boardIdx);
            }
            await sendPacket(req);
        }
    }
}
