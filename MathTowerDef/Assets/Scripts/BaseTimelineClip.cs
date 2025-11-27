using UnityEngine;
using UnityEngine.Playables;

public class BaseTimelineClip : PlayableBehaviour {
    private bool _isEntered = false;

    public virtual void onEnter(Playable playable, FrameData info, object playerData) { }
    public virtual void onExit(Playable playable, FrameData info) { }

    public override void OnBehaviourPause(Playable playable, FrameData info) {
        base.OnBehaviourPause(playable, info);

        if (info.effectivePlayState == PlayState.Playing) {
            Debug.Log("OnBehaviorPause ignored.");
            return;
        }

        if (_isEntered == false) {
            return;
        }

        onExit(playable, info);
        _isEntered = false;
    }

    public override void ProcessFrame(Playable playable, FrameData info, object playerData) {
        base.ProcessFrame(playable, info, playerData);
        if (_isEntered == false) {
            _isEntered = true;
            if (Application.isPlaying == false)
                return;

            onEnter(playable, info, playerData);
        }
    }

    //public override void OnPlayableCreate(Playable playable) {
    //    base.OnPlayableCreate(playable);
    //    Debug.Log("OnPlayableCreate");
    //}

    //public override void OnGraphStart(Playable playable) {
    //    base.OnGraphStart(playable);
    //    Debug.Log("OnGraphStart");
    //}

    //public override void OnGraphStop(Playable playable) {
    //    base.OnGraphStop(playable);
    //    Debug.Log("OnGraphStop");
    //}

    //public override void OnPlayableDestroy(Playable playable) {
    //    base.OnPlayableDestroy(playable);
    //    Debug.Log("OnPlayableDestroy");
    //}

    //public override void PrepareData(Playable playable, FrameData info) {
    //    base.PrepareData(playable, info);
    //    Debug.Log("prepare data");
    //}

    //public override void PrepareFrame(Playable playable, FrameData info) {
    //    base.PrepareFrame(playable, info);
    //}
}
