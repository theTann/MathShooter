#nullable disable
using System;
using System.Collections.Generic;
using MemoryPack;

[MemoryPackable]
[Serializable]
public partial class DefineData
{
    [MemoryPackOrder(1)] public string name { get; set; }
    [MemoryPackOrder(2)] public float val { get; set; }
}

[MemoryPackable]
[Serializable]
public partial class DefineDataTable
{
    [MemoryPackOrder(1)] public Dictionary<string, DefineData> DefineDatas = new();
}

