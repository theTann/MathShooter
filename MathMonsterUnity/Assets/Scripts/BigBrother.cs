using UnityEngine;

enum BigBrotherState {
    Watching,
    Ignoring
}

public class BigBrother : MonoBehaviour {
    Fsm<BigBrother, BigBrotherState> _fsm;

    void Start() {
        Debug.Log("Big Brother is watching you.");
    }
}
