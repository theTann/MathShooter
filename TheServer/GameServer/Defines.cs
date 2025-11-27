using MemoryPack;
using System.Text.Json.Serialization;

[MemoryPackable]
[Serializable]
public partial class PacketBase : IPoolObject, IDisposable {
    public virtual void reset() { }

    public virtual void Dispose() {
        // Console.WriteLine($"PacketBase.Dispose {this.GetType().Name}");
        GameServer.PoolBag.returnPacket(this);
    }
}
