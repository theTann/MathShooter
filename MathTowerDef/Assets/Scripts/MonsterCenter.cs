using System.Collections.Generic;
using UnityEditor.VersionControl;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class MonsterCenter {
    public enum WaveState {
        playing,
        summonEnd,
        noMonster,
    }

    List<Monster> _reachedMonsters = new();
    List<Monster> _activeMonster = new();

    PlayableDirector _pd;
    Player _player;

    float _minZPosition = float.MaxValue;
    Monster _frontMonster = null;
    public Monster getFrontMonster() => _frontMonster;

    private bool _freezeMove = false;
    

    WaveState _waveState;
    int _currentWaveIndex = 0;
    int _currentSpawnCount;
    int _maxSpawnCount;

    public void initMonsterCenter() {
        _player = GameManager.instance.getPlayer();
        _pd = GameManager.instance.GetComponent<PlayableDirector>();

        GameEventBus.register<MonsterDieEvent>(onMonsterDie);

        _ = loadAndPlayWave(_currentWaveIndex, 0);
    }

    public void destroyMonsterCenter() {
        GameEventBus.unregister<MonsterDieEvent>(onMonsterDie);
    }

    private async Awaitable<bool> loadAndPlayWave(int waveIndex, float waitSec) {
        string key = $"Assets/SpawnTimeline/Wave{waveIndex}.playable";
        
        var playableAsset = ResourceManager.loadSync<PlayableAsset>(key);
        if (playableAsset == null) {
            Logger.info($"all wave clear!");
            return false;
        }

        getWaveInformation(playableAsset);
        _pd.playableAsset = playableAsset;

        if(waitSec > 0) {
            await Awaitable.WaitForSecondsAsync(waitSec);
        }
        _pd.Play();
        return true;
    }

    public void getWaveInformation(PlayableAsset asset) {
        var timeline = asset as TimelineAsset;
        if (timeline == null) {
            Logger.error($"not a TimelineAsset. {asset}");
            return;
        }

        _currentSpawnCount = 0;
        _maxSpawnCount = 0;

        foreach (var track in timeline.GetOutputTracks()) {
            if (track.GetType() != typeof(SpawnTrack))
                continue;
            
            foreach (var clip in track.GetClips()) {
                var clipAsset = clip.asset as SpawnClipAsset;
                _maxSpawnCount += clipAsset.context.monsterCount;
            }
        }
        
        _waveState = WaveState.playing;

        Logger.debug($"{asset}, total spawn count : {_maxSpawnCount}");
    }

    public Monster spawnMonster(int id) {
        MonsterData monsterData = TableManager.instance.getMonsterData(id);

        // todo : to async load prefab
        float randomX = Random.Range(-2f, 2f);
        Vector3 pos = new Vector3(randomX, 0, 5);
        GameObject monsterObj = ResourceManager.instantiateSync(monsterData.ResourceId, pos, Quaternion.identity);

        var monster = monsterObj.GetComponent<Monster>();

        monster.initMonster(monsterData);
        _activeMonster.Add(monster);
        _currentSpawnCount++;

        if (_currentSpawnCount == _maxSpawnCount) {
            _waveState = WaveState.summonEnd;
        }

        return monster;
    }
    
    public void update(float deltaTime) {
        _minZPosition = float.MaxValue;
        _frontMonster = null;
        _reachedMonsters.Clear();
        bool doMove = !_freezeMove;
        foreach (var monster in _activeMonster) {
            float zPos = monster.updateMonster(deltaTime, doMove);

            if(zPos <= 0f) {
                _reachedMonsters.Add(monster);
                continue;
            } 
            
            if (zPos < _minZPosition) {
                _minZPosition = zPos;
                _frontMonster = monster;
            }
        }

        // todo : 최적화 필요.
        foreach(var monster in _reachedMonsters) {
            onMonsterReachEnd(monster);
        }

        if(_waveState == WaveState.summonEnd) {
            if (_activeMonster.Count == 0) {
                _waveState = WaveState.noMonster;
                _currentWaveIndex++;
                _ = loadAndPlayWave(_currentWaveIndex, 3);
            }
        }

        // updateSpawn(deltaTime);
    }

    public void setFreezeMove(bool freezeMove) { 
        _freezeMove = freezeMove;
        if (_freezeMove == true) {
            _pd.Pause();
        } 
        else {
            _pd.Resume();
        }
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
