#nullable disable
using System;
using System.Collections.Generic;
using MemoryPack;

[MemoryPackable]
[Serializable]
public partial class MonsterData
{
    [MemoryPackOrder(1)] public int MonsterId { get; set; }
    [MemoryPackOrder(2)] public string ResourceId { get; set; }
    [MemoryPackOrder(3)] public float spd { get; set; }
    [MemoryPackOrder(4)] public float hp { get; set; }
    [MemoryPackOrder(5)] public ulong gainExp { get; set; }
    [MemoryPackOrder(6)] public int damageToBase { get; set; }
}

[MemoryPackable]
[Serializable]
public partial class MonsterDataTable
{
    [MemoryPackOrder(1)] public Dictionary<int, MonsterData> MonsterDatas = new();
}

