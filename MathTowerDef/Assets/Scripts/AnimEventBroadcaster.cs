using UnityEngine;
using System.Collections.Generic;

public interface IAnimEventReceiver {
    void onAnimEvent(AnimationEvent animEvent);
}

public class AnimEventBroadcaster : MonoBehaviour {
    [SerializeField] private List<IAnimEventReceiver> _listener;

    private void OnValidate() {
        var listener = transform.GetComponentInParent<IAnimEventReceiver>();
        addListener(listener);
    }

    public void addListener(IAnimEventReceiver listener) {
        if(_listener == null) {
            _listener = new(1);
        }
        _listener.Add(listener);
    }

    public void onAnimEvent(AnimationEvent animEvent) {
        foreach(var listener in _listener) {
            listener.onAnimEvent(animEvent);
        }
    }
}
