using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;
using TMPro;
using System.Linq;
using Mono.Cecil;

public class NumberPanel : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler {
    enum Direction {
        none = 0,
        left = 1,
        right = 2,
        up = 4,
        down = 8,
    }

    [SerializeField] RectTransform _rectTransform;
    [SerializeField] TMP_Text[] _targetNumberTexts = new TMP_Text[3];

    const int xCount = 9;
    const int yCount = 7;

    float gemWidth = 100;
    float gemHeight = 100;

    float _horizontalPadding;
    float verticalPadding = 10;
    
    int[] _targetNumbers = new int[3];
    Gem[,] _gems;
    Gem _lastSelectedGem;
    List<Gem> _selectedGems = new List<Gem>(xCount * yCount);

    Action<int> _onMatchedNumber;
    public  void setOnMatchedNumber(Action<int> onMatchedNumber) { _onMatchedNumber = onMatchedNumber; }

    private bool _enabled = false;
    public void setEnabled(bool enabled) => _enabled = enabled;

    public void initNumberPanel() {
        createGems();

        int generateTargetNumberCount = 3;
        var results = generateTargetNumbers(generateTargetNumberCount);
        if(results == null) {
            return;
        }

        for(int i = 0; i < generateTargetNumberCount; i++) {
            _targetNumbers[i] = results[i];
            _targetNumberTexts[i].text = _targetNumbers[i].ToString();
        }

        setOnMatchedNumber(GameManager.instance.onMatchedNumber);

        //Logger.debug($"width : {_rectTransform.rect.width}, height : {_rectTransform.rect.height}");
        //foreach (var target in _targetNumbers) {
        //    Logger.debug($"Target Number: {target}");
        //}
    }

    private bool createGems() {
        float totalGemWidth = gemWidth * xCount;
        float totalGemHeight = gemHeight * yCount;

        float horizontalRemainSpace = _rectTransform.rect.width - totalGemWidth;
        if (horizontalRemainSpace > 0) {
            _horizontalPadding = (_rectTransform.rect.width - totalGemWidth) * 0.5f;
        }
        else {
            _horizontalPadding = 0;
            // gem은 정사각형이라고 가정하고 width기준으로 사이즈를 계산.
            gemWidth = (_rectTransform.rect.width - _horizontalPadding) / xCount;
            gemHeight = gemWidth;
        }

        // gemWidth, gemHeight가 크기가 바뀌었을 수 있으니 재계산.
        totalGemWidth = gemWidth * xCount;
        totalGemHeight = gemHeight * yCount;

        float verticalRemainSpace = _rectTransform.rect.height - totalGemHeight;
        if(verticalRemainSpace > 0) {
            verticalPadding = verticalRemainSpace * 0.5f;
        }
        else {
            verticalPadding = 0;
        }

        _gems = new Gem[xCount, yCount];
        for (int y = 0; y < yCount; y++) {
            for (int x = 0; x < xCount; x++) {
                string assetKey = "Assets/Gem/Gem.prefab";
                GameObject gemObj = ResourceManager.instantiateSync(assetKey, Vector3.zero, Quaternion.identity, _rectTransform);
                if (gemObj == null) {
                    return false;
                }
                RectTransform gemRectTransform = gemObj.GetComponent<RectTransform>();
                gemRectTransform.sizeDelta = new Vector2(gemWidth, gemHeight);
                // Logger.debug($"gem size : {gemWidth}");
                Vector2 startPos = new Vector2(_horizontalPadding, -verticalPadding);
                gemRectTransform.anchoredPosition = startPos + new Vector2(x * gemWidth, -y * gemHeight);
                var gem = gemObj.GetComponent<Gem>();
                _gems[x, y] = gem;
                gem.initGem(x, y);
            }
        }

        return true;
    }

    (int x, int y) calculateGemIndex(PointerEventData eventData) {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform,
            eventData.position,
            eventData.pressEventCamera,  // UI 카메라 (Canvas가 Screen Space - Camera일 때 필요)
            out Vector2 localPos
        );
        float width = _rectTransform.rect.width;
        float height = _rectTransform.rect.height;

        // NumberPanel의 pivot세팅때문에 중앙이 0이고 하단이 0인 좌표계라서 좌상단을 기준으로 normalize해줌
        Vector2 normalizedLocalPos = new Vector2(localPos.x + width * 0.5f, (localPos.y - height) * -1);
        if (normalizedLocalPos.x <= _horizontalPadding)
            return (-1, -1);
        if (normalizedLocalPos.y <= verticalPadding)
            return (-1, -1);
        if (normalizedLocalPos.x >= _horizontalPadding + gemWidth * xCount)
            return (-1, -1);
        if (normalizedLocalPos.y >= verticalPadding + gemHeight * yCount)
            return (-1, -1);

        int x = (int)((normalizedLocalPos.x - _horizontalPadding) / gemWidth);
        int y = (int)((normalizedLocalPos.y - verticalPadding) / gemHeight);
        return (x, y);
    }

    void tryAddSelectedGem(PointerEventData eventData) {
        (int x, int y) = calculateGemIndex(eventData);
        if (x == -1 || y == -1) {
            return;
        }

        Gem currentGem = _gems[x, y];
        if(_lastSelectedGem == currentGem)
            return;

        if (_selectedGems.Contains(currentGem) == true)
            return;

        if(_lastSelectedGem != null && isNeighborGem(currentGem, _lastSelectedGem) == false) {
            return;
        }

        currentGem.setSelectColor();
        _lastSelectedGem = currentGem;
        _selectedGems.Add(currentGem);
    }

    bool isNeighborGem(Gem lhs, Gem rhs) {
        var leftIndex = lhs.getIndex();
        var rightIndex = rhs.getIndex();
        
        (int x, int y) checkIndex = (leftIndex.x + 1, leftIndex.y);
        if (checkIndex == rightIndex) 
            return true;
        
        checkIndex = (leftIndex.x - 1, leftIndex.y);
        if (checkIndex == rightIndex) 
            return true;
        
        checkIndex = (leftIndex.x, leftIndex.y + 1);
        if(checkIndex == rightIndex) 
            return true;
        
        checkIndex = (leftIndex.x, leftIndex.y - 1);
        if (checkIndex == rightIndex)
            return true;

        return false;
    }

    void IPointerDownHandler.OnPointerDown(PointerEventData eventData) {
        if(_enabled == false)
            return;

        tryAddSelectedGem(eventData);
    }

    void IDragHandler.OnDrag(PointerEventData eventData) {
        if (_enabled == false)
            return;

        tryAddSelectedGem(eventData);
    }

    void IPointerUpHandler.OnPointerUp(PointerEventData eventData) {
        if (_enabled == false)
            return;

        int sum = getCurrentSum(true);

        if(_targetNumbers.Contains(sum)) {
            int generateCount = 3;
            var result = generateTargetNumbers(generateCount);
            if (result == null)
                return;

            for(int i = 0; i <  generateCount; i++) {
                _targetNumbers[i] = result[i];
                _targetNumberTexts[i].text = _targetNumbers[i].ToString();
                Logger.debug($"new targetnumber generated. {_targetNumbers[i]}");
            }
            _onMatchedNumber?.Invoke(sum);
        }

        _lastSelectedGem = null;
        _selectedGems.Clear();
    }

    public int getCurrentSum(bool revertColor) {
        int sum = 0;
        foreach (var gem in _selectedGems) {
            sum += gem.getNumber();
            if(revertColor)
                gem.revertColor();
        }
        return sum;
    }

    
    List<int> generateTargetNumbers(int count) {
        HashSet<int> candidates = new();
        
        List<(int x, int y)> closedListForTargetNumber = new();

        int rangeMin = 10;
        int rangeMax = 27;

        // todo : resetCount를 굳이 10번 안돌아도 될수있음.혹은 10번돌아도 안될수도있음(그럴일은 잘 없겠지만?) 이거 해결할 것
        int resetCount = 10;

        int currentSum = 0;
        (int x, int y) startPos = (Random.Range(0, xCount), Random.Range(0, yCount));
        (int x, int y) currentPos = startPos;
        closedListForTargetNumber.Clear();

        while (resetCount > 0) {
            Gem currentGem = _gems[currentPos.x, currentPos.y];
            int currentNumber = currentGem.getNumber();
            currentSum += currentNumber;

            if(currentSum >= rangeMin && currentSum <= rangeMax) {
                candidates.Add(currentSum);
            }

            if(currentSum > rangeMax) {
                // retry
                resetCount--;
                currentSum = 0;
                startPos = (Random.Range(0, xCount), Random.Range(0, yCount));
                currentPos = startPos;
                closedListForTargetNumber.Clear();
                continue;
            }

            var availabledirection = checkAvailableDirection(currentPos, closedListForTargetNumber);
            if (availabledirection == Direction.none) {
                // retry
                resetCount--;
                currentSum = 0;
                startPos = (Random.Range(0, xCount), Random.Range(0, yCount));
                currentPos = startPos;
                closedListForTargetNumber.Clear();
                continue;
            }
            
            closedListForTargetNumber.Add(currentPos);
            Direction targetDirection = getRandomActiveDirection(availabledirection);
            currentPos = getPositionValue(currentPos, targetDirection);
        }

        if(candidates.Count < count) {
            Logger.error($"can't generate target numbers. please check algorithm");
            return null;
        }
        var candidateList = candidates.ToList();
        List<int> result = new List<int>(count);
        for (int i = 0; i < count; i++) {
            int randomIndex = Random.Range(i, candidateList.Count);

            // 현재 위치(i)와 랜덤 위치(randomIndex)의 값을 교환 (Swap)
            int temp = candidateList[i];
            candidateList[i] = candidateList[randomIndex];
            candidateList[randomIndex] = temp;

            // 랜덤하게 섞인 i번째 요소를 결과에 추가
            result.Add(candidateList[i]);
        }
        return result;
    }

    // todo : 이전버전 일단 주석처리. 추후 삭제
    //int generateTargetNumber(int iterationCount) {
    //    int targetNumber = 0;
    //    bool exist = false;
        

    //    do {
    //        _closedNodeListForTargetNumber.Clear();

    //        (int x, int y) startPos = (Random.Range(0, xCount), Random.Range(0, yCount));
    //        (int x, int y) targetPos = startPos;

    //        for (int i = 0; i < iterationCount; i++) {
    //            Gem currentGem = _gems[targetPos.x, targetPos.y];
    //            // currentGem.setTempColor();
    //            targetNumber += currentGem.getNumber();

    //            var availabledirection = checkAvailableDirection(targetPos);
    //            if (availabledirection == Direction.none)
    //                break;

    //            _closedNodeListForTargetNumber.Add(targetPos);

    //            Direction targetDirection = getRandomActiveDirection(availabledirection);
    //            targetPos = getPositionValue(targetPos, targetDirection);
    //        }
    //        Logger.debug($"target number : {targetNumber}, actual iteration count : {_closedNodeListForTargetNumber.Count}");

    //        exist = _targetNumbers.Contains(targetNumber);
    //    } while (exist == true);

    //    return targetNumber;
    //}

    (int x, int y) getPositionValue((int x, int y) pos, Direction dir) {
        if (dir == Direction.none)
            return (-1, -1);

        if (dir == Direction.up)
            return (pos.x, pos.y - 1);
        if(dir == Direction.down)
            return (pos.x, pos.y + 1);
        if(dir == Direction.left)
            return (pos.x - 1, pos.y);
        if(dir == Direction.right)
            return (pos.x + 1, pos.y);
        return pos;
    }

    // gc있음.
    Direction getRandomActiveDirection(Direction current) {
        List<Direction> activeList = new List<Direction>();
        foreach (Direction dir in Enum.GetValues(typeof(Direction))) {
            if(dir == Direction.none)
                continue;

            if ((current & dir) == dir)
                activeList.Add(dir);
        }

        // 리스트가 비어있다면 기본값 반환
        if (activeList.Count == 0)
            return 0;

        // 랜덤 선택
        int index = Random.Range(0, activeList.Count);
        return activeList[index];
    }

    Direction checkAvailableDirection((int x, int y) pos, List<(int x, int y)> closedNodeListForTargetNumber) {
        Direction availableDirection = Direction.left | Direction.right | Direction.up | Direction.down;

        // left
        (int x, int y) checkPos = (pos.x - 1, pos.y);
        if(isOutOfBound(checkPos) == true || closedNodeListForTargetNumber.Contains(checkPos) == true)
            availableDirection = availableDirection & ~Direction.left;

        // right
        checkPos = (pos.x + 1, pos.y);
        if (isOutOfBound(checkPos) == true || closedNodeListForTargetNumber.Contains(checkPos) == true)
            availableDirection = availableDirection & ~Direction.right;

        // up
        checkPos = (pos.x, pos.y - 1);
        if (isOutOfBound(checkPos) == true || closedNodeListForTargetNumber.Contains(checkPos) == true)
            availableDirection = availableDirection & ~Direction.up;

        // down
        checkPos = (pos.x, pos.y + 1);
        if (isOutOfBound(checkPos) == true || closedNodeListForTargetNumber.Contains(checkPos) == true)
            availableDirection = availableDirection & ~Direction.down;

        return availableDirection;
    }

    bool isOutOfBound((int x, int y) pos) {
        if (pos.x < 0)
            return true;
        if (pos.x >= xCount) 
            return true;
        if(pos.y < 0)
            return true;
        if(pos.y >= yCount)
            return true;
        return false;
    }
}
