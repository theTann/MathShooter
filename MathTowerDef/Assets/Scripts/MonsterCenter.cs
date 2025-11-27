using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Playables;

public class MonsterCenter {
    List<Monster> reachedMonsters = new();
    List<Monster> _activeMonster = new();

    PlayableDirector _pd;
    Player _player;

    float _minZPosition = float.MaxValue;
    Monster _frontMonster = null;
    public Monster getFrontMonster() => _frontMonster;

    private bool _freezeMove = false;
    public void setFreezeMove(bool freezeMove) { _freezeMove = freezeMove; }

    public void initMonsterCenter() {
        _player = GameManager.instance.getPlayer();
        _pd = GameManager.instance.GetComponent<PlayableDirector>();

        GameEventBus.register<MonsterDieEvent>(onMonsterDie);

        var playableAsset = ResourceManager.loadSync<PlayableAsset>("Assets/SpawnTimeline/Wave0.playable");
        _pd.playableAsset = playableAsset;
        _pd.Play();
    }

    public void destroyMonsterCenter() {
        GameEventBus.unregister<MonsterDieEvent>(onMonsterDie);
    }

    public Monster generateMonster(int id) {
        MonsterData monsterData = TableManager.instance.getMonsterData(id);

        // todo : to async load prefab
        float randomX = Random.Range(-2f, 2f);
        Vector3 pos = new Vector3(randomX, 0, 5);
        GameObject monsterObj = ResourceManager.instantiateSync(monsterData.ResourceId, pos, Quaternion.identity);

        var monster = monsterObj.GetComponent<Monster>();

        monster.initMonster(monsterData);
        _activeMonster.Add(monster);
        return monster;
    }
    
    public void update(float deltaTime) {
        _minZPosition = float.MaxValue;
        _frontMonster = null;
        reachedMonsters.Clear();
        bool doMove = !_freezeMove;
        foreach (var monster in _activeMonster) {
            float zPos = monster.updateMonster(deltaTime, doMove);

            if(zPos <= 0f) {
                reachedMonsters.Add(monster);
                continue;
            } 
            
            if (zPos < _minZPosition) {
                _minZPosition = zPos;
                _frontMonster = monster;
            }
        }

        // todo : 최적화 필요.
        foreach(var monster in reachedMonsters) {
            onMonsterReachEnd(monster);
        }

        // updateSpawn(deltaTime);
    }

    //private void updateSpawn(float deltaTime) {
    //    _spawnTimer += deltaTime;

    //    if(_spawnTimer >= 1.0f) {
    //        _spawnTimer -= 1.0f;
    //        generateMonster(0);
    //    }
    //}

    public int getActiveMonsterCount() {
        return _activeMonster.Count;
    }

    public void onMonsterDie(ref MonsterDieEvent data) {
        Monster monster = data.dieMonster;

        // todo : swap remove for performance
        _activeMonster.Remove(monster);
        ResourceManager.releaseInstance(monster.gameObject);
    }

    public void onMonsterReachEnd(Monster monster) {
        GameManager.instance.onMonsterPassed(monster.getDamageToBase());

        _activeMonster.Remove(monster);
        ResourceManager.releaseInstance(monster.gameObject);
    }
}
