using UnityEngine;
using UnityEngine.Playables;
using System.Collections.Generic;

public class SpawnClip : BaseTimelineClip {
    public SpawnClipContext context;

    public override void onEnter(Playable playable, FrameData info, object playerData) {
        base.onEnter(playable, info, playerData);
        
        // _spawnPoint = (SpawnPointPrefabCache)playerData;
        // Debug.Log(_spawnPoint);
        _ = doSpawn(playable, context);
    }

    // async함수이고 SpawnClip은 destroy될 수 있기 때문에 static으로 this참조를 못하게 함.
    private static async Awaitable doSpawn(Playable playable, SpawnClipContext spawnContext) {
        var duration = playable.GetDuration();

        try {
            // 스폰 카운트가 1이면 spawnType에 따라서, 그 이상이면 intervalType에 따라서 처리
            if (spawnContext.monsterCount == 1) {
                await doSpawnBySpawnType(spawnContext, duration);
            }
            else if(spawnContext.monsterCount > 1) {
                await doSpawnByIntervalType(spawnContext, duration);
            }
        }
        catch (System.Exception e) {
            Logger.error(e);
        }
    }

    private static async Awaitable doSpawnBySpawnType(SpawnClipContext spawnContext, double clipDuration) {
        float waitTime = 0f;
        switch (spawnContext.spawnType) {
            case SpawnClipType.atStart:
                break;
            case SpawnClipType.randomInRange:
                waitTime = Random.Range(0f, (float)clipDuration);
                break;
        }

        if (waitTime > 0f)
            await Awaitable.WaitForSecondsAsync(waitTime);

        var monsterCenter = GameManager.instance.getMonsterCenter();
        monsterCenter.generateMonster(spawnContext.monsterId);
    }

    private static async Awaitable doSpawnByIntervalType(SpawnClipContext spawnContext, double clipDuration) {
        switch (spawnContext.intervalType) {
            case IntervalType.atOnceOnStart:
                {
                    var monsterCenter = GameManager.instance.getMonsterCenter();
                    for (int i = 0; i < spawnContext.monsterCount; i++) {
                        monsterCenter.generateMonster(spawnContext.monsterId);
                    }
                }
                break;
            case IntervalType.atOnceOnRandom:
                {
                    float waitTime = Random.Range(0f, (float)clipDuration);
                    await Awaitable.WaitForSecondsAsync(waitTime);
                    var monsterCenter = GameManager.instance.getMonsterCenter();
                    for (int i = 0; i < spawnContext.monsterCount; i++) {
                        monsterCenter.generateMonster(spawnContext.monsterId);
                    }
                }
                break;
            case IntervalType.randomInterval:
                {
                    var monsterCenter = GameManager.instance.getMonsterCenter();
                    List<float> waitTimes = new List<float>(spawnContext.monsterCount);

                    for (int i = 0; i < spawnContext.monsterCount; i++) {
                        float waitTime = Random.Range(0f, (float)clipDuration);
                        waitTimes.Add(waitTime);
                    }
                    waitTimes.Sort();
                    foreach (var waitTime in waitTimes) {
                        await Awaitable.WaitForSecondsAsync(waitTime);
                        monsterCenter.generateMonster(spawnContext.monsterId);
                    }
                }
                break;
            case IntervalType.evenDistribution:
                {
                    float interval = (float)clipDuration / spawnContext.monsterCount;
                    var monsterCenter = GameManager.instance.getMonsterCenter();
                    for (int i = 0; i < spawnContext.monsterCount; i++) {
                        if (i > 0)
                            await Awaitable.WaitForSecondsAsync(interval);
                        monsterCenter.generateMonster(spawnContext.monsterId);
                    }
                }
                break;
        }
    }
}
