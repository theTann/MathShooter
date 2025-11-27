#nullable disable
using System;
using System.Collections.Generic;
using MemoryPack;

[MemoryPackable]
[Serializable]
public partial class LevelData
{
    [MemoryPackOrder(1)] public uint Level { get; set; }
    [MemoryPackOrder(2)] public ulong NextExp { get; set; }
}

[MemoryPackable]
[Serializable]
public partial class LevelDataTable
{
    [MemoryPackOrder(1)] public List<LevelData> LevelDatas = new();
}

