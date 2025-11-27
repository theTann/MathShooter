#nullable enable

using MemoryPack;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PacketTypeAttribute : Attribute {
    public int id { get; }
    public PacketTypeAttribute(int packetType) => id = packetType;
}

public static class PacketRegistry {
    // public static RuntimeTypeModel typeModel => RuntimeTypeModel.Default;
    private static readonly Dictionary<int, Type?> idToType;
    private static readonly Dictionary<Type, int> typeToId;

    public static void touch() {}

    static PacketRegistry() {
        idToType = new();
        typeToId = new();

        // var baseMeta = typeModel.Add(typeof(PacketBase), applyDefaultBehaviour: true);
        
        // scan all public classes inheriting PacketBase
        IEnumerable<Type?> packetTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(PacketBase).IsAssignableFrom(t));

        // int subTypeIdx = 100;
        foreach (var type in packetTypes) {
            if (type == null)
                continue;

                var attr = type.GetCustomAttribute<PacketTypeAttribute>();
                if (attr == null)
                    continue;
            try {
                // Console.WriteLine($"Registering: {type.FullName}");
                idToType[attr.id] = type;
                typeToId[type] = attr.id;
                // typeModel.Add(type, true);
                // baseMeta.AddSubType(subTypeIdx++, type);
            } catch (Exception) {
                // Console.WriteLine($"Error registering {type.FullName}: {ex}");
            }
        }
        // typeModel.CompileInPlace();
        //typeModel.Compile();
    }

    public static bool tryGetType(int id, out Type? type) => idToType.TryGetValue(id, out type);
    public static bool tryGetId(Type type, out int id) => typeToId.TryGetValue(type, out id);
}

public static class PacketSerializer {
    // Header: [4 bytes typeId][4 bytes payload length]
    private const int _headerSize = 8;

    public static Dictionary<Type, ObjectPool>? packetPools = null;
    
    public static int serializePacket(PacketBase packet, NetworkBuffer buffer) {
        Type packetType = packet.GetType();
        if (!PacketRegistry.tryGetId(packetType, out var packetId))
            throw new InvalidOperationException("Unregistered packet type: " + packet.GetType());

        buffer.written(_headerSize);

        // PacketRegistry.typeModel.Serialize(buffer, packet);
        // Serializer.Serialize(buffer, packet);

        MemoryPackSerializer.Serialize<NetworkBuffer>(packetType, buffer, packet);

        var span = buffer.peekBufferAsSpan(_headerSize);
        int len = buffer.getWrittenSize() - _headerSize;

        // frame = header + payload
        // write typeId (big-endian)
        span[0] = (byte)(packetId >> 24);
        span[1] = (byte)(packetId >> 16);
        span[2] = (byte)(packetId >> 8);
        span[3] = (byte)(packetId & 0xFF);
        // write length
        span[4] = (byte)(len >> 24);
        span[5] = (byte)(len >> 16);
        span[6] = (byte)(len >> 8);
        span[7] = (byte)(len & 0xFF);

        return packetId;
    }
    
    public static PacketBase? deserializePacket(NetworkBuffer networkBuffer) {
        int writtenSize = networkBuffer.getWrittenSize();
        if (writtenSize < _headerSize)
            return null;

        // read header
        var header = networkBuffer.peekBufferAsSpan(_headerSize);

        // parse typeId
        int packetId = (header[0] << 24) | (header[1] << 16) | (header[2] << 8) | header[3];
        // parse length
        int length = (header[4] << 24) | (header[5] << 16) | (header[6] << 8) | header[7];

        if (writtenSize < _headerSize + length)
            return null;

        PacketRegistry.tryGetType(packetId, out var type);
        if(type == null) {
            throw new InvalidOperationException($"Unknown packet id: {packetId}");
        }

        ReadOnlySpan<byte> body = networkBuffer.peekBufferAsSpan(_headerSize, length);

        PacketBase? packetInstance = (PacketBase?)packetPools?[type].rentItem();

        if (packetInstance == null) {
            packetInstance = (PacketBase?)MemoryPackSerializer.Deserialize(type, body);
        }
        else {
            object? wrap = packetInstance;
            MemoryPackSerializer.Deserialize(type, body, ref wrap);
            packetInstance = (PacketBase?)wrap;
        }
        
        if (packetInstance != null) {
            networkBuffer.flushBuffer(length + _headerSize);
        }
        return packetInstance;
    }
}

[PacketType(1)]
[MemoryPackable]
[Serializable]
public partial class LoginRequest : PacketBase {
    [MemoryPackOrder(1)] public string? jwtToken { get; set; }
}

[PacketType(2)]
[MemoryPackable]
[Serializable]
public partial class LoginResponse : PacketBase {
    [MemoryPackOrder(1)] public bool success { get; set; }
    [MemoryPackOrder(2)] public long id { get; set; }
}

[PacketType(3)]
[MemoryPackable]
[Serializable]
public partial class ReqCreateRoom : PacketBase {
}

[PacketType(4)]
[MemoryPackable]
[Serializable]
public partial class ReqEnterRoom : PacketBase {
    [MemoryPackOrder(1)] public int roomId { get; set; }
}

[PacketType(5)]
[MemoryPackable]
[Serializable]
public partial class NtfEnterRoom : PacketBase {
    [MemoryPackOrder(1)] public int roomId { get; set; }
    [MemoryPackOrder(2)] public List<long> players { get; set; } = new();

    public override void reset() {
        base.reset();
        players?.Clear();
    }
}

[PacketType(6)]
[MemoryPackable]
[Serializable]
public partial class NtfLeaveRoom : PacketBase {
    [MemoryPackOrder(1)] public int roomId { get; set; }
    [MemoryPackOrder(2)] public long leavePlayer { get; set; }
    [MemoryPackOrder(3)] public List<long> players { get; set; } = new();

    public override void reset() {
        base.reset();
        players.Clear();
    }
}

[PacketType(7)]
[MemoryPackable]
[Serializable]
public partial class ReqLeaveRoom : PacketBase {
}

[PacketType(8)]
[MemoryPackable]
[Serializable]
public partial class ReqNoticeLoadingComplete : PacketBase {
}

[PacketType(9)]
[MemoryPackable]
[Serializable]
public partial class NtfGameStart : PacketBase {
    [MemoryPackOrder(1)] public DateTime startTime { get; set; }
    [MemoryPackOrder(2)] public int[]? board { get; set; }

    public override void reset() {
        base.reset();
        board = null;
    }
}

[PacketType(10)]
[MemoryPackable]
[Serializable]
public partial class ReqRemoveGem : PacketBase {
    // [ProtoMember(1)] public readonly List<BoardGrid> toRemove = new();
    [MemoryPackOrder(1)] public List<byte> toRemove { get; set; } = new();

    public override void reset() {
        base.reset();
        toRemove.Clear();
    }
}

[PacketType(11)]
[MemoryPackable]
[Serializable]
public partial class NtfRemoveGem : PacketBase {
    // [ProtoMember(1)] public readonly List<BoardGrid> toRemove = new();
    [MemoryPackOrder(1)] public List<byte> toRemove { get; set; } = new();
    [MemoryPackOrder(2)] public long removePlayer { get; set; }
    [MemoryPackOrder(3)] public int newScore { get; set; }
    
    public override void reset() {
        base.reset();
        toRemove.Clear();
    }
}

[PacketType(12)]
[MemoryPackable]
[Serializable]
public partial class NtfGameEnd : PacketBase {
    
}
