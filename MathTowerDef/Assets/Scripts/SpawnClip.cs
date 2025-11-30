using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class SpawnClip : BaseTimelineClip {
    public SpawnClipContext context;
    float _elapsedTime = 0f;
    int _spawnCount = 0;

    List<float> _spawnTimings = null;

    public override void onEnter(Playable playable, FrameData info, object playerData) {
        base.onEnter(playable, info, playerData);
        
        _elapsedTime = 0.0f;
        _spawnCount = 0;

        // monsterCount가 1일때만 SpawnType을 봄.
        if (context.monsterCount == 1 && context.spawnType == SpawnClipType.atStart) {
            doSpawnNow(1);
            return;
        }
        else if(context.monsterCount > 1 && context.intervalType == IntervalType.atOnceOnStart ) {
            doSpawnNow(context.monsterCount);
            return;
        }

        // LoopType이라면 computeWaitTimes을 두번할 필요는 없으므로 _spawnTiming이 존재하면 리턴.
        if (_spawnTimings.isNullOrEmpty() == false)
            return;

        computeWaitTimes(playable);
    }

    private void doSpawnNow(int monsterCount) {
        int monsterId = context.monsterId;
        for (int i = 0; i < monsterCount; i++) {
            var monsterCenter = GameManager.instance.getMonsterCenter();
            monsterCenter.spawnMonster(monsterId);
        }
    }

    private void computeWaitTimes(Playable playable) {
        
        float clipDuration = (float)playable.GetDuration();
        
        // monsterCount가 1일때만 SpawnType을 봄.
        // 그 이상일때에는 IntervalType을 봄.
        if(context.monsterCount == 1) {
            // SpawnClipType.atStart는 onEnter에서 처리했음.
            if (context.spawnType == SpawnClipType.randomInRange) {
                _spawnTimings = new List<float>(1);
                float waitTime = Random.Range(0f, clipDuration);
                _spawnTimings.Add(waitTime);
            }
        }
        else if(context.monsterCount > 1) {
            // IntervalType.atOnceOnStart는 onEnter에서 처리했음.
            if (context.intervalType == IntervalType.atOnceOnRandom) {
                _spawnTimings = new List<float>(1);
                float waitTime = Random.Range(0f, clipDuration);
                _spawnTimings.Add(waitTime);
            }
            else if (context.intervalType == IntervalType.evenDistribution) {
                _spawnTimings = new List<float>(context.monsterCount);
                float interval = (float)clipDuration / context.monsterCount;
                for(int i = 0; i < context.monsterCount; i++) {
                    _spawnTimings.Add(i * interval);
                }
            }
            else if (context.intervalType == IntervalType.randomInterval) {
                _spawnTimings = new List<float>(context.monsterCount);
                for (int i = 0; i < context.monsterCount; i++) {
                    float waitTime = Random.Range(0f, clipDuration);
                    _spawnTimings.Add(waitTime);
                }
                _spawnTimings.Sort();
            }
        }


        if (_spawnTimings == null) {
            _spawnTimings = new List<float>();
        }
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
        base.ProcessFrame(playable, info, playerData);

        if (_spawnTimings.isNullOrEmpty() == true)
            return;

        float deltaTime = GameManager.deltaTime;
        _elapsedTime += deltaTime;

        for(int i = _spawnCount; i < _spawnTimings.Count; i++) {
            float timing = _spawnTimings[i];
            if (_elapsedTime < timing) {
                break;
            }
            doSpawnNow(1);
            _spawnCount++;
        }
    }
}
