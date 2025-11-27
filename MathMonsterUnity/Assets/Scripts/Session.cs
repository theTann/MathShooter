#nullable enable

using System;

using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System.Linq;

public class Session {
    public enum State {
        disconnected,
        connecting,
        connected,
    }
    
    private const int _bufferSize = 1024 * 64; // 64kb
    
    private State _state = State.disconnected;
    public State getState() { return _state; }
    
    private Socket? _socket;
    
    private readonly NetworkBuffer _receiveBuffer = new(_bufferSize);
    private readonly NetworkBuffer _sendBuffer = new(_bufferSize);
    private readonly CancellationTokenSource _receiveCts = new();
    private readonly CancellationTokenSource _sendCts = new();
    
    private Action<PacketBase>? _onPacketReceived;
    private Action? _onDisconnect;
    // private readonly Queue<PacketBase> _packetQ = new();
    // private readonly object _lock = new();
    // private bool _send = false;

    public void setDisconnectAction(Action disconnectAction) { _onDisconnect = disconnectAction; }
    public void setPacketReceiveCallback(Action<PacketBase> callback) { _onPacketReceived = callback; }
    
    public async Awaitable connect(string host, int port) {
        _state = State.connecting;        
        await Awaitable.BackgroundThreadAsync();
        
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        try {
            IPAddress ipAddress;
            if (!IPAddress.TryParse(host, out ipAddress)) {
                var addresses = await Dns.GetHostAddressesAsync(host);
                var ipv4 = addresses.FirstOrDefault(addr => addr.AddressFamily == AddressFamily.InterNetwork);
                if (ipv4 == null)
                    throw new Exception("IPv4 address not found");
                ipAddress = ipv4;
            }

            await _socket.ConnectAsync(ipAddress, port);
        } catch(Exception e) {
            Debug.LogError($"connect error. {e}");
            _state = State.disconnected;
            return;
        }

        Debug.Log($"connected.");
        _state = State.connected;
        _ = receiveAsync();
    }

    public void doDisconnect() {
        if (_socket == null) return;
        
        Debug.Log("disconnected");
        _state = State.disconnected;
        _receiveCts.Cancel();
        _sendCts.Cancel();
        _socket?.Close();
        _socket?.Dispose();
        _socket = null;
        _onDisconnect?.Invoke();
    }

    private async Awaitable receiveAsync() {
        
        while (_state == State.connected) {
            try {
                int received = await _socket.ReceiveAsync(_receiveBuffer.getBufferAsArrSegment(), SocketFlags.None, _receiveCts.Token);
                if (received <= 0) {
                    Debug.Log("receive zero");
                    doDisconnect();
                    break;
                }
                _receiveBuffer.written(received);
                PacketBase? packet = PacketSerializer.deserializePacket(_receiveBuffer);
                if(packet == null) {
                    continue;
                }
                await Awaitable.MainThreadAsync();
                _onPacketReceived?.Invoke(packet);
                await Awaitable.BackgroundThreadAsync();
            } catch (OperationCanceledException e) {
                Debug.Log($"receive cancel. {e}");
                break;
            } catch (Exception e) {
                Debug.LogError($"receive exception : {e}");
                doDisconnect();
                break;
            }
        }
    }

    public async Awaitable sendPacket(PacketBase packet) {
        try {
            // todo : 큐잉해야함. buffer는 하나이기떄문.
            PacketSerializer.serializePacket(packet, _sendBuffer);
            Memory<byte> memoryBuffer = _sendBuffer.peekBuffer();
            int tobeSendSize = memoryBuffer.Length;
            int sent = await _socket.SendAsync(memoryBuffer, SocketFlags.None, _sendCts.Token);
            if (sent <= 0) {
                Debug.LogError("send fail");
                doDisconnect();
                return;
            }

            if (tobeSendSize != sent) {
                Debug.LogError($"send에서는 이런일 없다던데?");
                doDisconnect();
                return;
            }

            _sendBuffer.flushBuffer(sent);
        } catch (OperationCanceledException) {
            Debug.Log($"send canceled");
        } catch (Exception e) {
            Debug.LogError(e);
        }
        
    }
}
