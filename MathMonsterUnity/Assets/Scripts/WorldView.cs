using System.Threading.Tasks;

using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldView : MonoBehaviour {
    // [SerializeField] Button stageBtn;

    public void onStageBtnClick() {
        _ = loadSceneAsync();
    }

    public async Task<bool> loadSceneAsync() {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("GameScene");
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone) {
            // 로딩 퍼센트: asyncLoad.progress (0 ~ 0.9까지 증가, 0.9 이후 isDone이 true 됨)
            await Task.Yield();
        }

        return true;
    }
}
