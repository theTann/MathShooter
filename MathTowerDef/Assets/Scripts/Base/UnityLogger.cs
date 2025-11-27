using UnityEngine;

public class UnityLogger : ICustomLogger {
    [HideInCallstack]
    void ICustomLogger.debug(object message) {
        Debug.Log(message);
    }

    [HideInCallstack]
    void ICustomLogger.error(object message) {
        Debug.LogError(message);
    }

    [HideInCallstack]
    void ICustomLogger.info(object message) {
        Debug.Log(message);
    }

    [HideInCallstack]
    void ICustomLogger.warning(object message) {
        Debug.LogWarning(message);
    }
}
