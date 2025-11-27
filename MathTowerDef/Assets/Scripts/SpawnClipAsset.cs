using UnityEngine;
using UnityEngine.Playables;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Timeline;
#endif

public enum SpawnClipType {
    atStart,
    randomInRange,
}

public enum IntervalType {
    atOnceOnStart,
    atOnceOnRandom,
    randomInterval,
    evenDistribution,
}

[System.Serializable]
public struct SpawnClipContext {
    public SpawnClipType spawnType;
    public int monsterId;
    public int monsterCount;
    public IntervalType intervalType;
}

[System.Serializable]
public class SpawnClipAsset : PlayableAsset {
    public SpawnClipContext context;

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner) {
        var playable = ScriptPlayable<SpawnClip>.Create(graph);
        var behaviour = playable.GetBehaviour();
        behaviour.context = context;
        return playable;
    }

//#if UNITY_EDITOR
//    double getAutoCalculatedDuration() {
//        const float minDuration = 0.41f;
//        float duration = context.monsterCount * context.interval;
//        return (double)Mathf.Max(minDuration, duration);
//    }

//    private void OnValidate() {
//        EditorApplication.delayCall += () => {
//            var asset = this;
//            var timeline = TimelineEditor.inspectedAsset;
//            if (timeline == null) return;

//            foreach (var track in timeline.GetOutputTracks()) {
//                foreach (var clip in track.GetClips()) {
//                    if (clip.asset == asset) {
//                        clip.duration = getAutoCalculatedDuration();
//                    }
//                }
//            }
//            TimelineEditor.Refresh(RefreshReason.ContentsModified);
//        };
//    }
//#endif
}
