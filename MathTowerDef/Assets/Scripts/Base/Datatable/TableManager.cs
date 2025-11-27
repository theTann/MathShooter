using UnityEngine;

public class TableManager : Singleton<TableManager> {
    private LevelDataTable _levelDataTable;
    private MonsterDataTable _monsterDataTable;

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
}
