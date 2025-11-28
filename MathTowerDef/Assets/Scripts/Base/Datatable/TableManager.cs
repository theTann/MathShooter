using UnityEngine;

public class TableManager : Singleton<TableManager> {
    private LevelDataTable _levelDataTable;
    private MonsterDataTable _monsterDataTable;
    private DefineDataTable _defineDatatable;

    public void initTableManager() {
        var levelDataTable = TableLoader.loadTable<LevelDataTable>("Assets/StreamingAssets/DataTables/LevelData.bytes");
        if (levelDataTable == null) {
            Logger.error($"LevelDataTable is not exist.");
            return;
        }
        _levelDataTable = levelDataTable;

        var monsterDataTable = TableLoader.loadTable<MonsterDataTable>("Assets/StreamingAssets/DataTables/MonsterData.bytes");
        if (monsterDataTable == null) {
            Logger.error($"MonsterDataTable is not exist.");
            return;
        }
        _monsterDataTable = monsterDataTable;
        
        var defineDatatable = TableLoader.loadTable<DefineDataTable>("Assets/StreamingAssets/DataTables/DefineData.bytes");
        if (defineDatatable == null) {
            Logger.error($"MonsterDataTable is not exist.");
            return;
        }
        _defineDatatable = defineDatatable;
    }

    public ulong getLevelExp(uint level) {
        return _levelDataTable.LevelDatas[(int)level].NextExp;
    }

    public MonsterData getMonsterData(int id) {
        if(_monsterDataTable.MonsterDatas.TryGetValue(id, out MonsterData result) == false) {
            Logger.error($"Monster Data not exist.");
        }
        return result;
    }

    public float getDefineValue(string key) {
        bool result = _defineDatatable.DefineDatas.TryGetValue(key, out var value);
        if (result == false)
            return 0f;
        return value.val;
    }
}
