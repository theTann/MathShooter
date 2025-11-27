using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(int.MinValue)]
public class GameManager : MonoSingleton<GameManager> {
    public static float deltaTime;
    [SerializeField] private TMP_Text _debugText;

    [SerializeField] private NumberPanel _numberPanel;
    public NumberPanel getNumberPanel() => _numberPanel;

    [SerializeField] private Player _player;
    public Player getPlayer() => _player;

    [SerializeField] private LevelupPanel _levelupPanel;
    public LevelupPanel getLevelupPanel() => _levelupPanel;

    
    [SerializeField] private TMP_Text _hpText;
    [SerializeField] private Image _hpImage;
    [SerializeField] private Button _skillBtn;

    Fsm<GameManager, GameState> _fsm;

    float _fpsUpdateInterval = 1f;
    int _currentHp;
    int _maxHp;

    MonsterCenter _monsterCenter;
    public MonsterCenter getMonsterCenter() => _monsterCenter;

    SkillManager _skillManager;
    public SkillManager getSkillManager() => _skillManager;

    override protected void Awake() {
        base.Awake();
        Application.targetFrameRate = 999;

        Logger.init("tann", new UnityLogger());

        TableManager.instance.initTableManager();

        _fsm = new Fsm<GameManager, GameState>(this);

        registerEvents();

        _numberPanel.initNumberPanel();

        _monsterCenter = new();
        _monsterCenter.initMonsterCenter();

        _skillManager = new();
        _skillManager.initSkillManager(_skillBtn);

        _player.initPlayer();

        setHp(100, 100);

        _levelupPanel.showPanel(false);

        _fsm.changeState(GameState.playing);
    }
    
    private void Update() {
        deltaTime = Time.deltaTime;
        _fsm.update(deltaTime);
        updateDebugText();
    }

    private void registerEvents() {
        GameEventBus.register<LevelupEvent>(onLevelup);
    }

    protected override void OnDestroy() {
        base.OnDestroy();
        GameEventBus.unregister<LevelupEvent>(onLevelup);
    }

    public void onMatchedNumber(int madeNumber) {
        _player.addAmmoCount(madeNumber);
        // _towerCenter.createTower(madeNumber);
    }

    void updateDebugText() {
        _fpsUpdateInterval -= deltaTime;
        if (_fpsUpdateInterval > 0)
            return;

        _fpsUpdateInterval = 1f;
        float fps = 1.0f / Time.smoothDeltaTime;
        int sum = _numberPanel.getCurrentSum(false);
        _debugText.text = $"FPS: {fps:0.}, MonsterCount : {_monsterCenter.getActiveMonsterCount()}, sum : {sum}";
    }

    public void setHp(int currentHp, int maxHp = -1) {
        if(maxHp != -1) {
            _maxHp = maxHp;
        }
        _currentHp = currentHp;
        _hpText.text = $"{_currentHp} / {_maxHp}";
        float hpRatio = (float)_currentHp / (float)_maxHp;
        _hpImage.fillAmount = hpRatio;
    }

    public void onMonsterPassed(int damage) {
        setHp(_currentHp - damage);
        if(_currentHp <= 0) {
            _hpText.text = $"game over!";
            _fsm.changeState(GameState.gameOver);
        }
    }

    public void onLevelup(ref LevelupEvent levelupEvent) {
        changeState(GameState.levelUp);
    }

    #region wrapper methods for FSM
    public void changeState(GameState newState) {
        _fsm.changeState(newState);
    }
    #endregion
}
