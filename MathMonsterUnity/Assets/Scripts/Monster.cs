using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum MonsterState {
    idle,
    attack,
    death,
}

public class Monster : MonoBehaviour {
    [SerializeField] private Slider _monsterAttackSlider;
    [SerializeField] private Bar _monsterHpBar;
    [SerializeField] private Image _monsterImg;
    [SerializeField] private TMP_Text _targetNumTxt;

    GameManager _gameManager;

    int _monsterIdx;
    public int getMonsterIdx() {  return _monsterIdx; }

    MonsterState _state;
    public MonsterState getState() { return _state; }

    float _maxHp;
    public float getMaxHp() { return _maxHp; }

    float _currentHp;
    public float getCurrentHp() { return _currentHp; }
    
    float _monsterAttack;
    public float getAttackPower() { return _monsterAttack; }

    //int _targetNumber;
    //public int getTargetNumber() { return _targetNumber; }

    //float _monsterStateTime;
    //public void setStateTime(float time) { _monsterStateTime = time; }

    // float _attackTime;

    public void init(int idx, float hp, float monsterAttack, /*float attackTime,*/ GameManager gameManager) {
        _monsterIdx = idx;
        _gameManager = gameManager;
        // _attackTime = attackTime;
        // _state = MonsterState.idle;
        _maxHp = hp;
        _currentHp = hp;
        _monsterHpBar.setMaxVal(_maxHp);
        _monsterHpBar.setCurVal(hp);

        _monsterAttack = monsterAttack;
    }

    //public void setTargetNumber(int number) {
    //    _targetNumber = number;
    //    _targetNumTxt.text = number.ToString();
    //}

    public void changeState(MonsterState newState) {
        _state = newState;
        if(_state == MonsterState.death) {
            gameObject.SetActive(false);
            _gameManager.onMonsterDeath(this);
        }
        // _monsterStateTime = 0.0f;
    }

    public void updateMonster(float deltaTime)
    {
        //if (_state == MonsterState.idle) {
        //    _monsterStateTime += deltaTime;
        //    float attackRatio = Mathf.Min(_monsterStateTime / _attackTime, 1.0f);
        //    _monsterAttackSlider.value = attackRatio;
        //    if (attackRatio >= 1.0f) {
        //        changeState(MonsterState.attack);
        //    }
        //}
        //else if (_state == MonsterState.attack) {
        //    _monsterStateTime += deltaTime;
        //    float colorRatio = _monsterStateTime / 1.0f;
        //    Color red = Color.red;
        //    red.a = colorRatio;
        //    _monsterImg.color = red;

        //    if (_monsterStateTime > 1.0f) {
        //        _monsterStateTime = 0.0f;
        //        _monsterImg.color = Color.white;
        //        _gameManager.onMonsterAttack(this);
        //        changeState(MonsterState.idle);
        //    }
        //}
        //else if (_state == MonsterState.death) {

        //}
    }

    public void onMonsterAttacked(float attackPower) {
        float newHp = _currentHp - attackPower;
        _currentHp = newHp;
        _monsterHpBar.setCurVal(_currentHp);

        //if(_state == MonsterState.idle) {
        //    _monsterStateTime -= 3.0f;
        //}

        //if (_currentHp <= 0) {
        //    changeState(MonsterState.death);
        //    gameObject.SetActive(false);
        //    _gameManager.onMonsterDeath(this);
        //}
    }

    public void onMonsterClick() {
        // _gameManager.onTargetChange(_monsterIdx);
    }

    // 0.31
    async public Awaitable doAttackTween() {
        Sequence seq = DOTween.Sequence();

        Vector3 originalScale = transform.localScale;

        // 정면으로 살짝 돌진하는 느낌 (스케일 증가 → 복귀 → 흔들림)
        seq.Append(transform.DOScale(originalScale * 1.2f, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(transform.DOScale(originalScale, 0.08f).SetEase(Ease.InQuad));
        seq.Append(transform.DOShakeRotation(0.15f, 10f, 20, 90f)); // 회전 흔들림

        seq.Play();
        await Awaitable.WaitForSecondsAsync(0.31f);
    }

    // 0.05
    async public Awaitable doFlickerTween() {
        _monsterImg.DOFade(0.3f, 0.05f).SetLoops(4, LoopType.Yoyo);
        await Awaitable.WaitForSecondsAsync(0.05f);
    }
}
