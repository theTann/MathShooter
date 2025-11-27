#nullable enable

using MemoryPack;
using System.IO;

public class TableLoader {
    public static T? loadTable<T>(string binPath) {
        byte[] bytes = File.ReadAllBytes(binPath);
        T? result = MemoryPackSerializer.Deserialize<T>(bytes);
        return result;
    }
}
