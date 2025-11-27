using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

// public class BottomPanelCandidate {
//     public int count;
//     public List<(int x, int y, int w, int h)> positions;
//     public List<int> gemCount;
// }

namespace NetworkGame {
    public class BoardUI : MonoBehaviour {
        private const int _width = 9;
        private const int _height = 9;

        // [SerializeField] int _userIdx;
        [SerializeField] Transform uiRoot;
        [SerializeField] Transform gemParent;

        private RectTransform _uiRootRectTransform;
        private Canvas _uiRootCanvas;

        private Vector2 _startPos;
        private Vector2 _endPos;

        [SerializeField] RectTransform selectionBox;
        private List<RectTransform> _pickTargets = new();

        private Vector2 _startScreenPos;
        private Vector2 _endScreenPos;

        bool _enable;
    
        public Func<List<Gem>, Awaitable> onSelectGems;

        private Gem[] _gems;

        private readonly Dictionary<int, BottomPanelCandidate> _candidates = new Dictionary<int, BottomPanelCandidate>();

        // public User user;

        void Awake()
        {
            selectionBox.gameObject.SetActive(false);
            _uiRootCanvas = uiRoot.GetComponent<Canvas>();
            _uiRootRectTransform = uiRoot.GetComponent<RectTransform>();
            
        }

        private async void Start() {
            await loadGem();

            // user = NetworkManager._users[_userIdx];
            // onSelectGems += onAttackTry;
        }

        private void OnEnable() {
            EnhancedTouchSupport.Enable();
            // TouchSimulation.Enable();
        }

        private void OnDisable() {
            EnhancedTouchSupport.Disable();
        }

        public void removeGems(List<byte> removeList) {
            foreach (var item in removeList) {
                var gem = _gems[(int)item];
                setEnableGem(gem, false);
            }
        }

        public void setGems(int[] newBoard) {
            _pickTargets.Clear();

            for (int i = 0; i < newBoard.Length; i++) {
                int val = newBoard[i];
                Gem gem = _gems[i];
                
                gem.setNumber(val);
                if(val == -1) {
                    setEnableGem(gem, false);
                } else {
                    setEnableGem(gem, true);
                }
            }
        }

        public void setEnableGem(Gem gem, bool enable) {
            var rectTrans = gem.GetComponent<RectTransform>();
            if (enable == true) {
                _pickTargets.Add(rectTrans);
            } else {
                _pickTargets.Remove(rectTrans);
            }
                
            gem.gameObject.SetActive(enable);
            gem.isEnable = enable;
        }

        //public void onAttackTry(List<Gem> gems) {
        //    int sum = 0;

        //    foreach(var gem in gems) {
        //        sum += gem.value;
        //    }

        //    if(sum == 10) {
        //        ReqRemoveGem req = new ReqRemoveGem();
        //        foreach(var gem in gems) {
        //            req.toRemove.Add((byte)gem.boardIdx);
        //        }
        //        // _ = user.sendPacket(req);
        //    }
        //}

        public void setEnable(bool enable) {
            _enable = enable;
            if (enable == false) {
                selectionBox.gameObject.SetActive(false);

                foreach (RectTransform ui in _pickTargets) {
                    Gem gem = ui.GetComponent<Gem>();
                    gem.revertColor();
                }
            }
        }

        public async Awaitable loadGem() {
            _pickTargets.Clear();
            gemParent.GetComponent<GridLayoutGroup>().enabled = true;

            AsyncOperationHandle<GameObject>[] operations = new AsyncOperationHandle<GameObject>[_width * _height];
            for (int i = 0; i <  _width * _height; i++) {
                operations[i] = Addressables.InstantiateAsync("Assets/Prefabs/Gem.prefab", gemParent);
            }
        
            foreach (var t in operations) {
                await t.Task;
            }

            _gems = new Gem[_width * _height];
            for (int i = 0; i < _height; i++) {
                for (int j = 0; j < _width; j++) {
                    int idx = _width * i + j;
                    if(operations[idx].Status != AsyncOperationStatus.Succeeded) {
                        UnityEngine.Debug.LogError($"gem instantiate error.");
                        return;
                    }
                    GameObject inst = operations[idx].Result;
                    Gem gem = inst.GetComponent<Gem>();
                    _gems[idx] = gem;
                    gem.boardIdx = idx;
                    // int val = _board[idx];
                    // gem.init(val);
                    var rectTransform = inst.GetComponent<RectTransform>();
                    _pickTargets.Add(rectTransform);
                }
            }
            // android에서 grid가 동작안하는 케이스가 있어서 기다림.
            await Awaitable.NextFrameAsync();
            gemParent.GetComponent<GridLayoutGroup>().enabled = false;
        }

        void Update() {
            if (_enable == false)
                return;

#if UNITY_EDITOR || UNITY_STANDALONE
            if (Mouse.current.leftButton.wasPressedThisFrame) {
                _startScreenPos = Mouse.current.position.ReadValue();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRootRectTransform, _startScreenPos, null, out _startPos);
                // UnityEngine.Debug.Log($"screenPos : {_startScreenPos}, localPos : {_startPos}");
                selectionBox.gameObject.SetActive(true);
            }
            else if (Mouse.current.leftButton.isPressed) {
                _endScreenPos = Mouse.current.position.ReadValue();
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRootRectTransform, _endScreenPos, null, out _endPos);
                updateSelectionBox();
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame) {
                _endScreenPos = Mouse.current.position.ReadValue();
                selectionBox.gameObject.SetActive(false);
                selectElementsInBox();
            }
#else
        if (UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0)
        {
            var touch = UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches[0];

            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                _startScreenPos = touch.screenPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRootRectTransform, _startScreenPos, null, out _startPos);
                selectionBox.gameObject.SetActive(true);
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved || touch.phase == UnityEngine.InputSystem.TouchPhase.Stationary)
            {
                _endScreenPos = touch.screenPosition;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_uiRootRectTransform, _endScreenPos, null, out _endPos);
                updateSelectionBox();
            }
            else if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                _endScreenPos = touch.screenPosition;
                selectionBox.gameObject.SetActive(false);
                selectElementsInBox();
            }
        }
#endif
        }

        private void updateSelectionBox() {
            Vector2 boxStart = new Vector2(
                Mathf.Min((_startPos.x + _endPos.x) * 0.5f),
                Mathf.Min((_startPos.y + _endPos.y) * 0.5f));

            Vector2 boxSize = new Vector2(
                Mathf.Abs(_startPos.x - _endPos.x),
                Mathf.Abs(_startPos.y - _endPos.y));
            selectionBox.anchoredPosition = boxStart;
            selectionBox.sizeDelta = boxSize;

            Rect selectionRect = new Rect(selectionBox.anchoredPosition, selectionBox.sizeDelta);
            float x = Mathf.Min(_startScreenPos.x, _endScreenPos.x);
            float y = Mathf.Min(_startScreenPos.y, _endScreenPos.y);
            float width = Mathf.Abs(_startScreenPos.x - _endScreenPos.x);
            float height = Mathf.Abs(_startScreenPos.y - _endScreenPos.y);
            Rect screenRect = new Rect(x, y, width, height);
        
            foreach (RectTransform ui in _pickTargets) {
                Rect uiScreenRect = getScreenRect(ui, _uiRootCanvas);
                if (screenRect.Overlaps(uiScreenRect, true) == true) {
                    ui.GetComponent<Image>().color = Color.red;
                }
                else {
                    ui.GetComponent<Gem>().revertColor();
                }
            }
        }

        private void selectElementsInBox() {
            Rect selectionRect = new Rect(selectionBox.anchoredPosition, selectionBox.sizeDelta);
            float x = Mathf.Min(_startScreenPos.x, _endScreenPos.x);
            float y = Mathf.Min(_startScreenPos.y, _endScreenPos.y);
            float width = Mathf.Abs(_startScreenPos.x - _endScreenPos.x);
            float height = Mathf.Abs(_startScreenPos.y - _endScreenPos.y);
            Rect screenRect = new Rect(x, y, width, height);

            List<Gem> clickedGems = new List<Gem>();
            foreach (RectTransform ui in _pickTargets) {
                Rect uiScreenRect = getScreenRect(ui, _uiRootCanvas);
                if (screenRect.Overlaps(uiScreenRect, true) == true) {
                    clickedGems.Add(ui.GetComponent<Gem>());
                }
            }

            if (clickedGems.Count > 0) {
                onSelectGems?.Invoke(clickedGems);
                foreach (RectTransform ui in _pickTargets) {
                    ui.GetComponent<Gem>().revertColor();
                }
            }
        }

        private Rect getScreenRect(RectTransform rt, Canvas canvas) {
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners); // 0: ����, 1: �»�, 2: ���, 3: ����

            Camera cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

            Vector2 screenMin = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 screenMax = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);

            return new Rect(screenMin, screenMax - screenMin);
        }

        public void removeGems(List<Gem> gems) {
            foreach (Gem gem in gems) {
                _pickTargets.Remove(gem.GetComponent<RectTransform>());
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
            if (x >= _width || y >= _height) return null;
            int idx = x + _width * y;
            Gem gem = _gems[idx];
            if (gem.isEnable == false) return null;
            return gem;
        }

        public void computeCandidates() {
            Stopwatch sw = Stopwatch.StartNew();

            HashSet<(int addX, int addY)> addPostionList = new HashSet<(int addX, int addY)>();

            for (int y = 0; y < _height; ++y) {
                for (int x = 1; x < _width; ++x) {
                    addPostionList.Add((x, y));
                    addPostionList.Add((y, x));
                }
            }

            _candidates.Clear();
            for (int y = 0; y < _height; ++y) {
                for (int x = 0; x < _width; ++x) {
                    foreach (var (addX, addY) in addPostionList) {
                        if (x + addX >= _width)
                            continue;
                        if (y + addY >= _height)
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
            int idx = UnityEngine.Random.Range(0, candidate.positions.Count);
            var result = candidate.positions[idx];
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
            for (int y = 0; y < _height; ++y) {
                for (int x = 0; x < _width; ++x) {
                    Gem gem = getGem(x, y);
                    if(gem == null)
                        continue;
                    gem.revertColor();
                }
            }
        }
    }
}
