using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using System.Threading;
using System.Diagnostics;
using System.Linq;

public class BottomPanelCandidate {
    public int count;
    public List<(int x, int y, int w, int h)> positions;
    public List<int> gemCount;
}

public class BottomPanel : MonoBehaviour {
    const int width = 9;
    const int height = 9;

    [SerializeField] GameManager _gameManager;

    [SerializeField] Transform uiRoot;
    [SerializeField] Transform gemParent;

    RectTransform uiRootRectTransform;
    Canvas uiRootCanvas;

    private Vector2 startPos;
    private Vector2 endPos;

    [SerializeField] RectTransform selectionBox;
    public List<RectTransform> targetUIElements; // 검사할 UI 리스트

    private Vector2 startScreenPos;
    private Vector2 endScreenPos;

    bool _enable;
    
    public Action<List<Gem>> onSelectGems;

    Gem[] board;
    
    private Dictionary<int, BottomPanelCandidate> _candidates = new Dictionary<int, BottomPanelCandidate>();

    void Awake()
    {
        selectionBox.gameObject.SetActive(false);
        uiRootCanvas = uiRoot.GetComponent<Canvas>();
        uiRootRectTransform = uiRoot.GetComponent<RectTransform>();
    }

    private void OnDestroy() {
    }

    private void OnEnable() {
        EnhancedTouchSupport.Enable();
        TouchSimulation.Enable(); // Editor 터치 시뮬레이션
    }

    private void OnDisable() {
        EnhancedTouchSupport.Disable();
    }

    public void setEnable(bool enable) {
        _enable = enable;
        if (enable == false) {
            selectionBox.gameObject.SetActive(false);

            foreach (RectTransform ui in targetUIElements) {
                Gem gem = ui.GetComponent<Gem>();
                gem.revertColor();
            }
        }
    }

    public async Awaitable loadGem() {
        for(int i = 0; i < gemParent.childCount; i++) {
            var gem = gemParent.GetChild(i);
            Addressables.ReleaseInstance(gem.gameObject);
        }

        targetUIElements.Clear();
        gemParent.GetComponent<GridLayoutGroup>().enabled = true;

        AsyncOperationHandle<GameObject>[] operations = new AsyncOperationHandle<GameObject>[width * height];
        for (int i = 0; i <  width * height; i++) {
            operations[i] = Addressables.InstantiateAsync("Assets/Prefabs/Gem.prefab", gemParent);
        }
        
        for(int i = 0; i < operations.Length; i++) {
            await operations[i].Task;
        }

        board = new Gem[width * height];
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++) {
                int idx = width * i + j;
                if(operations[idx].Status != AsyncOperationStatus.Succeeded) {
                    UnityEngine.Debug.LogError($"gem instantiate error.");
                    return;
                }
                GameObject inst = operations[idx].Result;
                Gem gem = inst.GetComponent<Gem>();
                board[idx] = gem;
                gem.init();
                var rectTransform = inst.GetComponent<RectTransform>();
                targetUIElements.Add(rectTransform);
            }
        }
        // android에서 grid가 작동안해서 한프레임 건너뛴 후 GridLayoutGroup.enable false함.
        // await Awaitable.NextFrameAsync();
        await Awaitable.EndOfFrameAsync();
        gemParent.GetComponent<GridLayoutGroup>().enabled = false;
    }

    public void refreshGem() {
        for (int i = 0; i < height; i++) {
            for (int j = 0; j < width; j++) {
                int idx = width * i + j;
                var gem = board[idx];
                if (gem.isEnable == false)
                    continue;
                gem.genRandomNumber();
            }
        }
    }

    void Update() {
        if (_enable == false)
            return;

#if UNITY_EDITOR || UNITY_STANDALONE
        // 마우스 입력
        if (Mouse.current.leftButton.wasPressedThisFrame) {
            startScreenPos = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRootRectTransform, startScreenPos, null, out startPos);
            UnityEngine.Debug.Log($"screenPos : {startScreenPos}, localPos : {startPos}");
            selectionBox.gameObject.SetActive(true);
        }
        else if (Mouse.current.leftButton.isPressed) {
            endScreenPos = Mouse.current.position.ReadValue();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRootRectTransform, endScreenPos, null, out endPos);
            UpdateSelectionBox();
        }
        else if (Mouse.current.leftButton.wasReleasedThisFrame) {
            endScreenPos = Mouse.current.position.ReadValue();
            selectionBox.gameObject.SetActive(false);
            SelectElementsInBox();
        }
        if (Keyboard.current.spaceKey.wasReleasedThisFrame) {
            _gameManager.highlightAvailableGem();
        }
#else
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count >= 3) {
            _gameManager.highlightAvailableGem();
        }
        // 터치 입력
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                startScreenPos = touch.screenPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRootRectTransform, startScreenPos, null, out startPos);
                selectionBox.gameObject.SetActive(true);
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                endScreenPos = touch.screenPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(uiRootRectTransform, endScreenPos, null, out endPos);
                UpdateSelectionBox();
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                endScreenPos = touch.screenPosition;
                selectionBox.gameObject.SetActive(false);
                SelectElementsInBox();
            }
        }
#endif
    }

    void UpdateSelectionBox() {
        Vector2 boxStart = new Vector2(
            Mathf.Min((startPos.x + endPos.x) * 0.5f),
            Mathf.Min((startPos.y + endPos.y) * 0.5f));

        Vector2 boxSize = new Vector2(
            Mathf.Abs(startPos.x - endPos.x),
            Mathf.Abs(startPos.y - endPos.y));
        selectionBox.anchoredPosition = boxStart;
        selectionBox.sizeDelta = boxSize;

        Rect selectionRect = new Rect(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        float x = Mathf.Min(startScreenPos.x, endScreenPos.x);
        float y = Mathf.Min(startScreenPos.y, endScreenPos.y);
        float width = Mathf.Abs(startScreenPos.x - endScreenPos.x);
        float height = Mathf.Abs(startScreenPos.y - endScreenPos.y);
        Rect screenRect = new Rect(x, y, width, height);
        
        foreach (RectTransform ui in targetUIElements) {
            Rect uiScreenRect = GetScreenRect(ui, uiRootCanvas);
            if (screenRect.Overlaps(uiScreenRect, true) == true) {
                if(ui.GetComponent<Gem>().doSelect() == true) {
                    _gameManager.playSelectGemSound();
                }
            }
            else {
                ui.GetComponent<Gem>().revertColor();
            }
        }
    }

    void SelectElementsInBox() {
        Rect selectionRect = new Rect(selectionBox.anchoredPosition, selectionBox.sizeDelta);
        float x = Mathf.Min(startScreenPos.x, endScreenPos.x);
        float y = Mathf.Min(startScreenPos.y, endScreenPos.y);
        float width = Mathf.Abs(startScreenPos.x - endScreenPos.x);
        float height = Mathf.Abs(startScreenPos.y - endScreenPos.y);
        Rect screenRect = new Rect(x, y, width, height);

        List<Gem> clickedGems = new List<Gem>();
        foreach (RectTransform ui in targetUIElements) {
            Rect uiScreenRect = GetScreenRect(ui, uiRootCanvas);
            if (screenRect.Overlaps(uiScreenRect, true) == true) {
                clickedGems.Add(ui.GetComponent<Gem>());
            }
        }

        if (clickedGems.Count > 0) {
            onSelectGems?.Invoke(clickedGems);
            foreach (RectTransform ui in targetUIElements) {
                ui.GetComponent<Gem>().revertColor();
            }
        }
    }

    public Rect GetScreenRect(RectTransform rt, Canvas canvas) {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners); // 0: 좌하, 1: 좌상, 2: 우상, 3: 우하

        Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
        Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

        return new Rect(screenMin, screenMax - screenMin);
    }

    public void removeGems(List<Gem> gems) {
        foreach (Gem gem in gems) {
            targetUIElements.Remove(gem.GetComponent<RectTransform>());
            gem.gameObject.SetActive(false);
            gem.isEnable = false;
        }
    }
    
    int getNumber(int x, int y) {
        Gem gem = getGem(x, y);
        if (gem == null)
            return -1;
        return gem.value;
    }

    Gem getGem(int x, int y) {
        if (x < 0 || y < 0) return null;
        if (x >= width || y >= height) return null;
        int idx = x + width * y;
        Gem gem = board[idx];
        if (gem.isEnable == false) return null;
        return gem;
    }

    public void computeCandidates() {
        Stopwatch sw = Stopwatch.StartNew();

        HashSet<(int addX, int addY)> addPostionList = new HashSet<(int addX, int addY)>();

        for (int y = 0; y < height; ++y) {
            for (int x = 1; x < width; ++x) {
                addPostionList.Add((x, y));
                addPostionList.Add((y, x));
            }
        }

        _candidates.Clear();
        for (int y = 0; y < height; ++y) {
            for (int x = 0; x < width; ++x) {
                foreach (var (addX, addY) in addPostionList) {
                    if (x + addX >= width)
                        continue;
                    if (y + addY >= height)
                        continue;

                    int sum = 0;
                    int gemCount = 0;

                    for (int checkYIdx = y; checkYIdx <= y + addY; ++checkYIdx) {
                        for (int checkXIdx = x; checkXIdx <= x + addX; ++checkXIdx) {
                            Gem gem = getGem(checkXIdx, checkYIdx);
                            if (gem == null)
                                continue;
                            // gem.GetComponent<Image>().color = Color.red;
                            int currentVal = gem.value;
                            sum += currentVal;
                            gemCount++;
                        }
                    }

                    bool exist = _candidates.TryGetValue(sum, out BottomPanelCandidate statisticsVal);
                    if (exist == false) {
                        statisticsVal = new BottomPanelCandidate();
                        statisticsVal.count = 1;
                        statisticsVal.positions = new List<(int x, int y, int w, int h)>();
                        statisticsVal.gemCount = new List<int>();

                        statisticsVal.positions.Add((x, y, addX, addY));
                        statisticsVal.gemCount.Add(gemCount);
                        _candidates.Add(sum, statisticsVal);
                    }
                    else {
                        statisticsVal.count++;
                        statisticsVal.positions.Add((x, y, addX, addY));
                        statisticsVal.gemCount.Add(gemCount);
                    }
                }
            }
        }
        sw.Stop();
        UnityEngine.Debug.Log($"{sw.ElapsedMilliseconds}ms spent");
    }

    public (int x, int y, int w, int h) isExistGemCombine(int targetNumber) {
        bool exist = _candidates.TryGetValue(targetNumber, out BottomPanelCandidate candidate);
        if(exist == false) {
            return (-1, -1, -1, -1);
        }

        int maxCount = -1;
        int saveIdx = -1;
        for(int i = 0; i < candidate.gemCount.Count; i++) { 
            int count = candidate.gemCount[i];
            if(count > maxCount) {
                saveIdx = i;
                maxCount = count;
            }
        }
        // int idx = UnityEngine.Random.Range(0, candidate.positions.Count);
        var result = candidate.positions[saveIdx];
        return result;
    }

    public List<KeyValuePair<int, BottomPanelCandidate>> pickTargetNumber() {
        List<KeyValuePair<int, BottomPanelCandidate>> result = new List<KeyValuePair<int, BottomPanelCandidate>>();

        foreach (var kvp in _candidates) {
            int targetNumber = kvp.Key;
            if (targetNumber <= 19 || targetNumber >= 30)
                continue;

            bool isMoreThanSix = true;
            foreach (var gemCount in kvp.Value.gemCount) {
                if(gemCount < 6) {
                    isMoreThanSix = false;
                    break;
                }
            }
            if (isMoreThanSix == true)
                continue;
            result.Add(kvp);
        }

        return result;
    }

    public void setColor(int x, int y, Color color) {
        Gem gem = getGem(x, y);
        
        if (gem == null)
            return;

        gem.GetComponent<Image>().color = color;
    }

    public void revertAllGemColor() {
        for (int y = 0; y < height; ++y) {
            for (int x = 0; x < width; ++x) {
                Gem gem = getGem(x, y);
                if(gem == null)
                    continue;
                gem.revertColor();
            }
        }
    }
}
