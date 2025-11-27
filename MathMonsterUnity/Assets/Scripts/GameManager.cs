using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

using System;
using DG.Tweening;

public enum PraiseLevel {
    none = -1,
    good,
    great,
    awesome,
    amazing,
}

public struct AttackData {
    public List<Monster> target;
    public float attackPoint;
    public float damageMultiplier;
    public float spIncrease;
}

public class GameManager : MonoBehaviour {
    const float maxComboCount = 10;

    private const float basePlayerHp = 100;
    private const float skillIncrease = 10;
    private const float playerTurnTime = 15.0f;
    private const float increaseTimeByAttack = 1f;

    private enum GameState {
        loading,
        playerTurn,
        attackAnimation,
        monsterAttackTurn,
        clear,
        regenGems,
        gameOver,
        count,
    }

    private class GameStateMachine {
        delegate void EnterDelegate();
        delegate void ExitDelegate();
        delegate Awaitable OnEnterAnimateDelegate();

        public GameState state;
        public float stateTime;

        EnterDelegate _commonEnter;
        EnterDelegate[] _enters;
        OnEnterAnimateDelegate[] _onEnterAnims;
        ExitDelegate[] _exits;

        bool _flushQueueNow = false;
        Queue<GameState> _reservedStates = new Queue<GameState>();

        public void init(GameManager gameManager) {
            int count = (int)GameState.count;
            _enters = new EnterDelegate[count];
            _onEnterAnims = new OnEnterAnimateDelegate[count];
            _exits = new ExitDelegate[count];

            _commonEnter = gameManager.commonEnterState;

            _enters[(int)GameState.playerTurn] = gameManager.onEnterPlayerTurn;
            _enters[(int)GameState.monsterAttackTurn] = gameManager.onEnterMonsterAttackTurn;

            _onEnterAnims[(int)GameState.loading] = gameManager.loadingAsync;
            _onEnterAnims[(int)GameState.regenGems] = gameManager.regenGemsAsync;
            _onEnterAnims[(int)GameState.clear] = gameManager.clearAsync;
            _onEnterAnims[(int)GameState.attackAnimation] = gameManager.playerAttackAsync;
            _onEnterAnims[(int)GameState.monsterAttackTurn] = gameManager.animateMonsterAttack;
            _onEnterAnims[(int)GameState.gameOver] = gameManager.gameOverAsync;
        }

        public bool needToFlushState() {
            if(_flushQueueNow == false && _reservedStates.Count > 0)
                return true;
            return false;
        }

        public void pushState(GameState newState) {
            _reservedStates.Enqueue(newState);
        }

        public async Awaitable<GameState> pushStateAsync(GameState newState) {
            try {
                _reservedStates.Enqueue(newState);
                if (_flushQueueNow == false) {
                    await popState();
                }
            } catch (Exception e) {
                Debug.LogError(e);
            }
            return state;
        }

        async public Awaitable popState() {
            try {
                _flushQueueNow = true;

                int prevStateIdx = (int)state;
                GameState newState = _reservedStates.Dequeue();
                int newStateIdx = (int)newState;

                state = newState;
                stateTime = 0.0f;
                _exits[prevStateIdx]?.Invoke();
                _commonEnter?.Invoke();
                _enters[newStateIdx]?.Invoke();
                var onEnterAnim = _onEnterAnims[newStateIdx];
                if (onEnterAnim != null)
                    await onEnterAnim();

                while (_reservedStates.Count != 0) {
                    await popState();
                }

                _flushQueueNow = false;
            }
            catch (Exception e) {
                Debug.LogError(e);
            }
        }
    }

    // from editor
    [SerializeField] private AudioClip _monsterAttack;
    [SerializeField] private AudioClip[] _playerAttacks;
    [SerializeField] private AudioClip[] _praiseSound;
    [SerializeField] private AudioClip _gemSelect;

    [SerializeField] private GameObject _hideGameScreen;
    [SerializeField] private TMP_Text _centralText;
    [SerializeField] private Image _waitImg;
    [SerializeField] private BottomPanel _bottomPanel;
    [SerializeField] private Transform _monsterPanel;
    [SerializeField] private Bar _monsterTotalBar;
    [SerializeField] private Image _targetImge;
    [SerializeField] private TMP_Text _targetNumberTxt;
    [SerializeField] private Bar _skillBar;
    [SerializeField] private Bar _playerBar;
    [SerializeField] private LongTouchDetector[] _heroSlots;
    [SerializeField] private Transform _praiseParent;
    [SerializeField] private GameObject[] _praiseImges;
    [SerializeField] private Sprite[] _praiseCheckImages;
    [SerializeField] private GameObject _comboPanel;
    [SerializeField] private ComboItem[] _comboItems;
    [SerializeField] private TMP_Text _comboMultiplierTxt;
    [SerializeField] private Bar _remainTimeBar;
    [SerializeField] private GameObject _replayBtn;
    [SerializeField] private TMP_Text _waveTxt;
    [SerializeField] private TMP_Text _scoreTxt;
    [SerializeField] private TMP_Text _centerComboTxt;

    // ï¿½ï¿½ï¿½Ó½ï¿½ï¿½åº¯ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ ï¿½È¾ï¿½ï¿½ï¿½..
    // int _targetNumber;
    // int _currentTargetIdx = 0;
    AudioSource _audioSource;

    public BottomPanel getBottomPanel() { return _bottomPanel; }
    private GameStateMachine _gameStateMachine = new GameStateMachine();
    private int _currentWave;
    private Player _player = new Player();
    private List<Monster> _monsters = new List<Monster>();
    private int _currentComboCount = 0;
    List<AttackData> _currentTurnAttack = new List<AttackData>();
    float _totalScore = 0;

    async void Start() {
        initStage();
        await startWave();
    }

    private void Update() {
        float deltaTime = Time.deltaTime;
        var state = _gameStateMachine.state;
        _gameStateMachine.stateTime += deltaTime;

        if (state == GameState.playerTurn)
            updatePlayerTurn(deltaTime);
        else if (state == GameState.attackAnimation)
            updateAttackAnimation(deltaTime);
        else if (state == GameState.monsterAttackTurn)
            updateMonsterTurn(deltaTime);

        updateCommon(deltaTime);

        //foreach (var monster in _monsters) {
        //    monster.updateMonster(deltaTime);
        //}
    }

    void initStage() {
        _audioSource = GetComponent<AudioSource>();
        _currentWave = 1;
        
        _gameStateMachine.init(this);
        _bottomPanel.onSelectGems = onPlayerAttack;

        _player.gameManager = this;
        _player.skillBar = _skillBar;
        _player.playerBar = _playerBar;
        _player.heroSlots = _heroSlots;
        _player.init();
    }

    async Awaitable startWave() {
        await _gameStateMachine.pushStateAsync(GameState.loading);
    }

    void commonEnterState() {
        _bottomPanel.setEnable(_gameStateMachine.state == GameState.playerTurn);
    }

    void onEnterPlayerTurn() {
        _currentComboCount = 0;
        
        _comboPanel.SetActive(true);
        foreach (var comboItem in _comboItems) {
            comboItem.setText(null);
            comboItem.setCheck(false);
        }

        //foreach (var attackIcon in _attackIcons) {
        //    attackIcon.gameObject.SetActive(false);
        //}

        _remainTimeBar.gameObject.SetActive(true);
        _remainTimeBar.setMaxVal(playerTurnTime);
        _remainTimeBar.setRatio(1);
        _remainTimeBar.setText($"remain : {playerTurnTime}s");

        _praiseParent.gameObject.SetActive(true);
        foreach (var praiseImg in _praiseImges) {
            praiseImg?.SetActive(false);
        }
        _centerComboTxt.gameObject.SetActive(false);
    }

    void onEnterMonsterAttackTurn() {
        _comboPanel.SetActive(false);
        _remainTimeBar.gameObject.SetActive(false);
        _praiseParent.gameObject.SetActive(false);
    }


    async Awaitable loadingAsync() {
        _replayBtn.SetActive(false);
        
        _waveTxt.text = $"Wave : {_currentWave}";
        _scoreTxt.text = $"Score {(int)_totalScore:N0}";

        _hideGameScreen.SetActive(true);
        _centralText.gameObject.SetActive(true);
        _centralText.text = $"current wave :{_currentWave}";
        _bottomPanel.setEnable(false);

        await Awaitable.WaitForSecondsAsync(0.5f);

        _player.setState(PlayerState.alive);

        if(_currentWave == 1) {
            _totalScore = 0;
            _player.setMaxHp(basePlayerHp);
            _player.setCurrentHp(basePlayerHp);

            _player.setMaxSp(100);
            _player.setCurrentSp(0);
        }

        await _bottomPanel.loadGem();
        _bottomPanel.computeCandidates();

        await monsterLoad();

        // _currentTargetIdx = 0;
        // refreshTargetUI();

        refreshMonsterTotalBar();

        // var result = _bottomPanel.pickTargetNumber();
        // int resultIdx = Random.Range(0, result.Count);
        // setTargetNumber(result[resultIdx].Key);

        //if (result.Count <= 0)
        //    Debug.LogError("not exist BottomPanel Candidates");
        //else {
        //    for(int i = 0; i < _monsters.Count; i++) {
        //        _monsters[i].setTargetNumber(result[i].Key);
        //    }
        //}

        _centralText.text = "go!";
        await Awaitable.WaitForSecondsAsync(0.5f);// Task.Delay(500);

        _hideGameScreen.SetActive(false);
        _centralText.gameObject.SetActive(false);
        await _gameStateMachine.pushStateAsync(GameState.playerTurn);
    }

    async Awaitable regenGemsAsync() {
        _hideGameScreen.SetActive(true);
        _centralText.gameObject.SetActive(true);
        _centralText.text = $"not exist available gems.";
        _bottomPanel.setEnable(false);
        await Awaitable.WaitForSecondsAsync(1.0f);

        // todo : gems ï¿½ï¿½ï¿½ï¿½.
        await _bottomPanel.loadGem();

        await Awaitable.WaitForSecondsAsync(1.0f);

        _hideGameScreen.SetActive(false);
        _centralText.gameObject.SetActive(false);
        _bottomPanel.setEnable(true);

        await _gameStateMachine.pushStateAsync(GameState.playerTurn);
    }

    async Awaitable clearAsync() {
        _hideGameScreen.SetActive(true);
        _centralText.gameObject.SetActive(true);
        _centralText.text = $"clear wave.";
        _bottomPanel.setEnable(false);
        await Awaitable.WaitForSecondsAsync(1.0f);
        
        _currentWave++;

        await startWave();
    }

    async Awaitable playerAttackAsync() {
        var hlg = _comboItems[0].GetComponentInParent<HorizontalLayoutGroup>();
        hlg.enabled = false;
        Monster monster = _monsters[0];
        for (int i = 0; i < _currentTurnAttack.Count; i++) {
            AttackData attackData = _currentTurnAttack[i];
            // _attackIcons[i].gameObject.SetActive(true);
            UIBezierFly bezier = _comboItems[i].GetComponent<UIBezierFly>();

            int soundIdx = UnityEngine.Random.Range(0, _playerAttacks.Length);
            _audioSource.PlayOneShot(_playerAttacks[soundIdx]);

            bezier.init(_comboItems[i].transform.position, monster.GetComponent<RectTransform>(), 0.4f, (fly) => {
                processAttack(attackData);
                ComboItem item = fly.GetComponent<ComboItem>();
                item.setCheck(false);
                item.setText(null);
            });
            await Awaitable.WaitForSecondsAsync(0.3f);
        }
        _currentTurnAttack.Clear();
        
        await Awaitable.WaitForSecondsAsync(1.3f);

        if (monster.getCurrentHp() <= 0) {
            monster.changeState(MonsterState.death);
        }

        hlg.enabled = true;

        if (checkEveryMonsterDie() == true) {
            await _gameStateMachine.pushStateAsync(GameState.clear);
        } else {
            await _gameStateMachine.pushStateAsync(GameState.monsterAttackTurn);
        }
    }

    async Awaitable animateMonsterAttack() {
        // ï¿½Ï´ï¿½ ï¿½Ñ¸ï¿½ï¿½ï¿½.
        Monster monster = _monsters[0];
        
        _audioSource.PlayOneShot(_monsterAttack);

        await monster.doAttackTween();
        await monster.doFlickerTween();

        onMonsterAttack(monster);

        await Awaitable.WaitForSecondsAsync(0.5f);

        if (_player.getState() == PlayerState.death) {
            await _gameStateMachine.pushStateAsync(GameState.gameOver);
        }
        else {
            await _gameStateMachine.pushStateAsync(GameState.playerTurn);
        }
    }

    async Awaitable gameOverAsync() {
        _hideGameScreen.SetActive(true);
        _centralText.gameObject.SetActive(true);
        _centralText.text = "GameOver!";
        highlightAvailableGem(3);
        await Awaitable.WaitForSecondsAsync(3f);
        _replayBtn.SetActive(true);
    }

    float getCurrentComboMultiplier(int idx) {
        Span<float> comboMultiplier = stackalloc float[] { 1.0f, 1.1f, 1.3f, 1.5f, 2.0f, 2.5f, 3.0f, 3.5f, 4.0f, 5.0f };
        
        idx = Math.Max(0, idx);
        idx = Math.Min(comboMultiplier.Length - 1, idx);

        return comboMultiplier[idx];
    }

    void refreshComboMultiplierText() {
        _comboMultiplierTxt.text = $"x {getCurrentComboMultiplier(_currentComboCount)}";
    }

    bool checkEveryMonsterDie() {
        foreach(Monster monster in _monsters) {
            if(monster.getState() != MonsterState.death) { 
                return false; 
            }
        }
        return true;
    }

    //void setTargetNumber(int targetNumber) {
    //    _targetNumber = targetNumber;
    //    _targetNumberTxt.text = _targetNumber.ToString();
    //}

    //void refreshTargetUI() {
    //    Monster monster = _monsters[_currentTargetIdx];
    //    RectTransform transform = monster.GetComponent<RectTransform>();
    //    _targetImge.rectTransform.anchoredPosition = transform.anchoredPosition;//  + new Vector2(0, 80);
    //}

    async Awaitable monsterLoad() {
        foreach(var monster in _monsters) {
            Addressables.ReleaseInstance(monster.gameObject);
        }
        _monsters.Clear();

        float baseHp = 30;
        double additiveHp = Math.Pow((_currentWave - 1), 2);
        float monsterHp = baseHp + (float)additiveHp;

        float baseAtk = 10;
        double additiveAtk = Math.Pow((_currentWave - 1), 1.2);
        float monsterAtk = baseAtk + (float)additiveAtk;

        //if(_currentWave <= 2) {
        //    monsterHp = UnityEngine.Random.Range(60.0f, 140.0f);
        //} else if(_currentWave <= 5) {
        //    monsterHp = UnityEngine.Random.Range(150.0f, 230.0f);
        //} else if(_currentWave <= 10) {
        //    monsterHp = UnityEngine.Random.Range(250.0f, 350.0f);
        //} else {
        //    monsterHp = UnityEngine.Random.Range(400.0f, 600.0f);
        //}

        var monsterLoadDatas = new (string address, float hp, float attack, /*float attackTime,*/ float x, float y, float scale)[] {
            (
                address: "Assets/Prefabs/dragon.prefab",
                hp: monsterHp,
                attack: monsterAtk,
                // attackTime: 30.0f,
                x: 0,
                y: -170,
                scale: 1f
            ),
            //(
            //    address: "Assets/Prefabs/dragon.prefab",
            //    hp: 100 + _currentWave * 50,
            //    attack: 10 + _currentWave * 3,
            //    attackTime: 20.0f,
            //    x: -320,
            //    y: -65,
            //    scale: 0.7f
            //),

            //(
            //    address: "Assets/Prefabs/dragon.prefab",
            //    hp: 100 + _currentWave * 50,
            //    attack: 10 + _currentWave * 3,
            //    attackTime: 25.0f,
            //    x: 320,
            //    y: -65,
            //    scale: 0.7f
            //),
        };

        AsyncOperationHandle<GameObject>[] operations = new AsyncOperationHandle<GameObject>[monsterLoadDatas.Length];
        for (int i = 0; i < monsterLoadDatas.Length; i++) {
            var monsterLoadData = monsterLoadDatas[i];
            operations[i] = Addressables.InstantiateAsync(monsterLoadData.address, _monsterPanel);
        }

        for (int i = 0; i < operations.Length; i++) {
            await operations[i].Task;
        }
        
        for (int i = 0; i < monsterLoadDatas.Length; i++) {
            var monsterLoadData = monsterLoadDatas[i];
            if (operations[i].Status != AsyncOperationStatus.Succeeded) {
                Debug.LogError($"monster load errer. index {i}, address : {monsterLoadData.address}");
                return;
            }

            GameObject inst = operations[i].Result;
            RectTransform rect = inst.GetComponent<RectTransform>();
            Vector3 pos = new Vector3(monsterLoadData.x, monsterLoadData.y, 0.0f);
            rect.anchoredPosition = pos;
            rect.localScale = new Vector3(
                monsterLoadData.scale, 
                monsterLoadData.scale, 
                monsterLoadData.scale
            );

            Monster monster = inst.GetComponent<Monster>();
            monster.init(i, monsterLoadData.hp, monsterLoadData.attack, /*monsterLoadData.attackTime,*/ this);
            _monsters.Add(monster);
        }
        _targetImge.transform.SetAsLastSibling();
        _praiseParent.SetAsLastSibling();
    }

    public Monster getMonster(int idx) {
        return _monsters[idx];
    }

    void updatePlayerTurn(float deltaTime) {
        float remainTime = playerTurnTime - Math.Max(0, _gameStateMachine.stateTime);
        float remainRatio = remainTime / playerTurnTime;
        _remainTimeBar.setRatio(remainRatio);
        
        // todo : ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½ï¿½Ì·ï¿½ï¿½Â°ï¿½ ï¿½ï¿½ï¿½ï¿½È­
        _remainTimeBar.setText($"remainTime : {remainTime:0}s");

        if (remainTime <= 0.0f) {
            _ = _gameStateMachine.pushStateAsync(GameState.attackAnimation);
        }
    }

    void updateAttackAnimation(float deltaTime) {

    }

    void updateMonsterTurn(float deltaTime) {

    }

    void updateCommon(float deltaTime) {
    }

    // from gem board(bottomPanel)
    public void onPlayerAttack(List<Gem> gems) {
        Span<float> gemCountMultiplier = stackalloc float[] { 1.0f, 1.5f, 2.0f, 3.0f };

        int sum = 0;
        int gemCount = gems.Count;

        foreach (var gem in gems) {
            sum += gem.value;
        }

        bool attackSuccess = false;
        AttackData data = new AttackData();

        if (sum == 10) {
            attackSuccess = true;
            // todo : pool
            data.target = new List<Monster>() {
                // _monsters[_currentTargetIdx],
                _monsters[0],
            };
            data.attackPoint = _player.getAttackPoint();
            data.spIncrease = skillIncrease;
            int gemCountMultiplierIdx = Math.Min(gemCount - 2, gemCountMultiplier.Length - 1);
            data.damageMultiplier = gemCountMultiplier[gemCountMultiplierIdx];
        }

        //if(sum == _targetNumber) {
        //    attackSuccess = true;
        //    data.target = new List<Monster>();
        //    foreach (var monster in _monsters) {
        //        if (monster.getState() == MonsterState.death)
        //            continue;
        //        data.target.Add(monster);
        //    }
        //    data.attackPoint = playerAttack * 3 / data.target.Count;
        //    data.spIncrease = skillIncrease * 2;
        //}

        //foreach(Monster monster in _monsters) {
        //    if(monster.getTargetNumber() == sum) {
        //        attackSuccess = true;
        //        data.target = monster;
        //        data.attackPoint = playerAttack * 3;
        //        data.spIncrease = skillIncrease * 2;
        //    }
        //}

        if (attackSuccess == true) {
            _gameStateMachine.stateTime -= increaseTimeByAttack;

            PraiseLevel level = processPraise(gemCount);
            _currentTurnAttack.Add(data);

            int idx = (int)level;
            Sprite checkImg = _praiseCheckImages[idx];
            _comboItems[_currentComboCount].setPraiseLevel(checkImg);
            _comboItems[_currentComboCount].setCheck(true);
            _comboItems[_currentComboCount].setText((data.attackPoint * data.damageMultiplier).ToString());
            refreshComboMultiplierText();

            _currentComboCount++;
            
            _bottomPanel.removeGems(gems);
            _bottomPanel.computeCandidates();

            if (_currentComboCount >= maxComboCount) {
                _ = _gameStateMachine.pushStateAsync(GameState.attackAnimation);
                return;
            }

            //var list = _bottomPanel.pickTargetNumber();
            //if (list != null && list.Count > 0) {
            //    int listIdx = UnityEngine.Random.Range(0, list.Count);
            //    if (sum != 10) {
            //        setTargetNumber(list[listIdx].Key);
            //    }
            //}
            do {
                var (x, y, w, h) = _bottomPanel.isExistGemCombine(10);
                if (x != -1) {
                    break;
                }

                //bool existCombine = false;
                //(x, y, w, h) = _bottomPanel.isExistGemCombine(_targetNumber);
                //if (x != -1) {
                //    existCombine = true;
                //    break;
                //}
                //if (existCombine == true)
                //    break;

                _ = _gameStateMachine.pushStateAsync(GameState.regenGems);
            } while (false);

        }
    }

    private PraiseLevel processPraise(int gemCount) {
        PraiseLevel praiseLevel = PraiseLevel.none;
        foreach(var praiseImg in _praiseImges) {
            praiseImg?.SetActive(false);
        }
        _centerComboTxt.gameObject.SetActive(true);
        _centerComboTxt.text = $"{_currentComboCount + 1} Combo!";
        CanvasGroup canvasGroup = _praiseParent.GetComponent<CanvasGroup>();
        if (gemCount == 2) {
            praiseLevel = PraiseLevel.good;
            int idx = (int)(praiseLevel);
            _audioSource.PlayOneShot(_praiseSound[idx]);
            var img = _praiseImges[idx];
            img.SetActive(true);

            _praiseParent.localScale = Vector3.zero;
            canvasGroup.alpha = 1;
            Sequence s = DOTween.Sequence();
            s.Append(_praiseParent.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack))
             .Append(_praiseParent.DOScale(1.0f, 0.1f))
             .AppendInterval(0.5f)
             .Append(canvasGroup.DOFade(0f, 0.3f))                          // ¼­¼­È÷ »ç¶óÁü
             .OnComplete(() => {
                 img.SetActive(false);
                 _centerComboTxt.gameObject.SetActive(false);
             });

        } 
        else if (gemCount == 3) {
            praiseLevel = PraiseLevel.great;
            int idx = (int)(praiseLevel);
            _audioSource.PlayOneShot(_praiseSound[idx]);
            var img = _praiseImges[idx];
            img.SetActive(true);

            _praiseParent.localScale = Vector3.zero;
            canvasGroup.alpha = 1;
            Sequence s = DOTween.Sequence();
            s.Append(_praiseParent.DOScale(1.2f, 0.2f).SetEase(Ease.OutBack))   // ÆÎ! Æ¢¾î³ª¿È
             .Append(_praiseParent.DOScale(1.0f, 0.1f))                        // ¿ø·¡ Å©±â
             .AppendInterval(0.5f)
             .Append(canvasGroup.DOFade(0f, 0.3f))                          // ¼­¼­È÷ »ç¶óÁü
             .OnComplete(() => {
                 _centerComboTxt.gameObject.SetActive(false);
                 img.SetActive(false);
             });

        }
        else if (gemCount == 4) {
            praiseLevel = PraiseLevel.awesome;
            int idx = (int)(praiseLevel);
            var img = _praiseImges[idx];
            _audioSource.PlayOneShot(_praiseSound[idx]);
            img.SetActive(true);
            _praiseParent.localScale = Vector3.zero;
            _praiseParent.rotation = Quaternion.identity;
            canvasGroup.alpha = 1;
            Sequence s = DOTween.Sequence();
            s.Append(_praiseParent.DOScale(1.5f, 0.25f).SetEase(Ease.OutElastic))
             .Join(_praiseParent.DORotate(new Vector3(0, 0, 30f), 0.25f))
             .Append(_praiseParent.DOScale(1.0f, 0.1f))
            .AppendInterval(0.7f)
             .Append(canvasGroup.DOFade(0f, 0.3f))
             .OnComplete(() => {
                 _centerComboTxt.gameObject.SetActive(false);
                 img.SetActive(false);
             });
        }
        else if (gemCount >= 5) {
            praiseLevel = PraiseLevel.amazing;
            int idx = (int)(praiseLevel);
            var img = _praiseImges[idx];
            _audioSource.PlayOneShot(_praiseSound[idx]);
            img.SetActive(true);

            _praiseParent.localScale = Vector3.zero;
            canvasGroup.alpha = 1;
            Sequence s = DOTween.Sequence();
            s.Append(_praiseParent.DOScale(1.0f, 0.3f).SetEase(Ease.OutBounce))
             .Join(_praiseParent.DOShakeRotation(0.4f, 10f)) // Èçµé¸² È¿°ú
             .AppendInterval(0.5f)
             .Append(canvasGroup.DOFade(0f, 0.3f))
             .OnComplete(() => {
                 _centerComboTxt.gameObject.SetActive(false);
                 img.SetActive(false);
             });
        }
        
        return praiseLevel;
    }

    // 
    public void processAttack(AttackData data) {
        float damageSum = 0;
        
        damageSum = data.attackPoint * data.damageMultiplier;
        _player.increaseSp(data.spIncrease);

        damageSum *= getCurrentComboMultiplier(_currentComboCount - 1);
        
        // ï¿½Ï´ï¿½ ï¿½ï¿½ï¿½ï¿½ ï¿½Ï³ï¿½ï¿½ï¿½ï¿?ï¿½ï¿½ï¿½ï¿½
        Monster monster = _monsters[0];
        monster.onMonsterAttacked(damageSum);
        _totalScore += damageSum;
        _scoreTxt.text = $"Score {(int)_totalScore:N0}";
        refreshMonsterTotalBar();
    }

    public void onMonsterAttack(Monster attacker) {
        float attackPoint = attacker.getAttackPower();
        _player.increaseHp(-attackPoint);
    }

    // from ui click
    public void onTargetChange(int targetIdx) {
        // _currentTargetIdx = targetIdx;
        // refreshTargetUI();
    }

    public void onMonsterDeath(Monster deathMonster) {
        //int newTargetIdx = -1;
        
        //for(int i = 0; i < _monsters.Count; i++) {
        //    Monster monster = _monsters[i];
        //    if(monster.getState() != MonsterState.death) {
        //        newTargetIdx = i;
        //        break;
        //    }
        //}
        
        //bool allDie = newTargetIdx == -1;

        //if (deathMonster.getMonsterIdx() == _currentTargetIdx) {
        //    _currentTargetIdx = newTargetIdx;

        //    if(allDie) {
        //        _ = setState(GameState.clear);
        //        return;
        //    }
        //    refreshTargetUI();
        //}
    }

    public void refreshMonsterTotalBar() {
        var (max, cur) = getMonsterTotalHp();
        _monsterTotalBar.setMaxVal(max);
        _monsterTotalBar.setCurVal(cur);
    }

    public (float max, float cur) getMonsterTotalHp() {
        float currentHp = 0;
        float maxHp = 0;
        foreach (var monster in _monsters) {
            if (monster.getState() == MonsterState.death)
                continue;

            maxHp += monster.getMaxHp();
            currentHp += monster.getCurrentHp();
        }
        return (maxHp, currentHp);
    }

    
    public void highlightAvailableGem(float time = 1.0f) {
        int targetNumber = 10; // UnityEngine.Random.Range(0, 2) == 0 ? _targetNumber : 10;
        var (x, y, w, h) = _bottomPanel.isExistGemCombine(targetNumber);
        for (int posY = y; posY <= y + h; posY++) {
            for (int posX = x; posX <= x + w; posX++) {
                _bottomPanel.setColor(posX, posY, Color.red);
            }
        }
        Invoke("revertColor", time);
    }

    void revertColor() {
        _bottomPanel.revertAllGemColor();
    }

    async public void onReplayBtn() {
        _currentWave = 1;
        await startWave();
    }

    public void playSelectGemSound() {
        _audioSource.PlayOneShot(_gemSelect);
    }

}
