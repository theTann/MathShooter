using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class ResourceManager {
    public static GameObject instantiateSync(string key, Vector3 position, Quaternion rot, Transform parent = null) {
        var aoh = Addressables.InstantiateAsync(key, position, rot, parent);
        GameObject result = aoh.WaitForCompletion();
        if(result == null) {
            Logger.error($"resource load fail. key : {key}");
        }
        return result;
    }

    public static void releaseInstance(GameObject obj) {
        Addressables.ReleaseInstance(obj);
    }

    public static T loadSync<T>(string key) where T : UnityEngine.Object {
        var aoh = Addressables.LoadAssetAsync<T>(key);
        T result = aoh.WaitForCompletion();
        if(result == null) {
            Logger.error($"resource load fail. key : {key}");
        }
        return result;
    }
}
