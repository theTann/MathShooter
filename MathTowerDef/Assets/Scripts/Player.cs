using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum AnimationEvent {
    attackHit,
}

public class Player : MonoBehaviour, IAnimEventReceiver {
    public static int attackAnimatorHash = Animator.StringToHash("attack");
    public static int idleAnimatorHash = Animator.StringToHash("idle");

    // todo : 이건 UI쪽에있어서 SerializeField가 쪼끔 안맞음.
    [SerializeField] private TMP_Text _ammoCountTxt;
    [SerializeField] private Animator _animator;
    [SerializeField] private SpriteRenderer _ooaImage;
    [SerializeField] private TMP_Text _currentExpTxt;
    [SerializeField] private Image _expBarImage;

    private ulong _currentExp;
    public ulong getCurrentExp() => _currentExp;

    private uint _currentLevel;

    private float _attackPower = 7f;

    private float _attackSpeed = 1f; // 초당 공격횟수.
    public float getAttackSpeed() => _attackSpeed;
    public void setAttackSpeed(float speed) => _attackSpeed = speed;
    public void addAttackSpeed(float addSpeed) => _attackSpeed += addSpeed;

    private float _attackAnimDuration = -1f;
    public float getAttackAnimDuration() => _attackAnimDuration;

    int _ammoCount = 0;
    public int getAmmoCount() => _ammoCount;
    
    private Tween _ooaTween;
    
    private float _globalAttackTime = 0.0f; // PlayerState와 상관없이 공격가능 여부를 체크하기 위해 global이라는 이름이 붙음.
    public float getGlobalAttackTime() => _globalAttackTime;
    public void resetGlobalAttackTime() => _globalAttackTime = 0.0f;

    private Fsm<Player, PlayerState> _fsm;

    private Monster _currentTarget;
    public Monster getCurrentTarget() => _currentTarget;
    public void setCurrentTarget(Monster target) {
         _currentTarget = target;
        if(_currentTarget != null) {
            lookAtTargetYawOnly(target.transform.position);
        }
    }

    private void Update() {
        float deltaTime = GameManager.deltaTime;
        _globalAttackTime += deltaTime;
        _fsm.update(deltaTime);
    }

    public void initPlayer() {
        _attackAnimDuration = getAttackAnimClipDuration();
        Logger.debug($"Attack animation duration: {_attackAnimDuration}");

        _attackPower = TableManager.instance.getDefineValue("attack_power");
        _attackSpeed = TableManager.instance.getDefineValue("attack_speed");

        GameEventBus.register<MonsterDieEvent>(onMonsterDie);

        initFsm();
        initPlayerContent();
    }

    public void destroyPlayer() {
        GameEventBus.unregister<MonsterDieEvent>(onMonsterDie);
    }

    private void initFsm() {
        _fsm = new Fsm<Player, PlayerState>(this);
        _fsm.changeState(PlayerState.idle);
    }

    void initPlayerContent() {
        _currentLevel = 1;
        setAmmoCount(0);
        setCurrentExp(0);
        _ooaImage.gameObject.SetActive(false);
    }

    public void setAmmoCount(int count) {
        _ammoCount = count;
        _ammoCountTxt.text = _ammoCount.ToString();
    }

    public void addAmmoCount(int addCount) {
        setAmmoCount(_ammoCount + addCount);
    }

    private float getAttackAnimClipDuration() {
        AnimationClip[] clips = _animator.runtimeAnimatorController.animationClips;
        foreach (var clip in clips) {
            if (clip.name == "PlayerAttack") {
                return clip.length;
            }
        }
        Logger.error("Attack animation clip not found!");
        return -1f;
    }

    public void showNoAmmo() {
        if (_ooaTween != null && _ooaTween.IsActive() && _ooaTween.IsPlaying()) {
            // 이미 팝업 애니메이션이 진행 중
            return;
        }

        _ooaImage.gameObject.SetActive(true);
        _ooaTween = DOTween.Sequence()
            .AppendCallback(() => {
                _ooaImage.transform.localScale = new Vector3(0.5f, 0.5f, 1);
                Color color = _ooaImage.color;
                color.a = 1.0f;
                _ooaImage.color = color;
            })

            // 팡! 하고 튀어나오는 효과
            .Append(_ooaImage.transform.DOScale(0.6f, 0.15f).SetEase(Ease.OutBack))
            
            // 살짝 줄어들며 안정화
            .Append(_ooaImage.transform.DOScale(0.5f, 0.1f))
            
            // 잠깐 유지
            .AppendInterval(0.4f)
            
            // 서서히 사라짐 (알파 줄이기 + 약간 축소)
            .Append(_ooaImage.DOFade(0f, 0.25f))
            .Join(_ooaImage.transform.DOScale(0.4f, 0.25f))
            .OnComplete(() => {
                _ooaImage.gameObject.SetActive(false);
            });
    }

    private void lookAtTargetYawOnly(Vector3 targetPos) {
        Vector3 to = targetPos - transform.position;
        to.y = 0f;                                // XZ 평면 투영
        if (to.sqrMagnitude < 1e-6f) return;      // 0 벡터 예외
        transform.rotation = Quaternion.LookRotation(to, Vector3.up);
    }

    public void doAttack(Monster target) {
        addAmmoCount(-1);
        AttackEvent attackEvent = new AttackEvent();
        attackEvent.attacker = this;
        attackEvent.damage = _attackPower;
        attackEvent.targetMonster = target;
        GameEventBus.broadcast(ref attackEvent);
    }

    public void addExp(ulong gainExp) {
        ulong nextLevelExp = TableManager.instance.getLevelExp(_currentLevel);
        bool isLevelUp = false;
        ulong currentExp = _currentExp + gainExp;

        if (nextLevelExp <= currentExp) {
            _currentLevel += 1;
            isLevelUp = true;
            currentExp = currentExp - nextLevelExp;
        }

        setCurrentExp(currentExp);
        if (isLevelUp == true) {
            var levelupEvent = new LevelupEvent();
            levelupEvent.player = this;
            levelupEvent.newLevel = _currentLevel;
            
            GameEventBus.broadcast(ref levelupEvent);
            Logger.debug($"Level up!");
            //GameManager.instance.changeState(GameState.levelUp);
        }
    }

    void IAnimEventReceiver.onAnimEvent(AnimationEvent animEvent) {
        if (_currentTarget == null) {
            // attack state에 진입했는데 타겟이 끝에 도착하면 없을 수 있음.
            return;
        }
        doAttack(_currentTarget);
    }

    public void setCurrentExp(ulong exp) {
        _currentExp = exp;
        _currentExpTxt.text = _currentExp.ToString();
        ulong nextLevelExp = TableManager.instance.getLevelExp(_currentLevel);
        float fillAmount = (float)_currentExp / nextLevelExp;
        _expBarImage.fillAmount = fillAmount;
        _currentExpTxt.text = $"{_currentExp} / {nextLevelExp}";
    }

    public void onMonsterDie(ref MonsterDieEvent dieEvent) {
        if (dieEvent.killer != this) {
            return;
        }
        Monster monster = dieEvent.dieMonster;
        ulong exp = monster.getExpReward();
        addExp(exp);
    }

    #region wrapper functions
    public void changeState(PlayerState newState) {
        _fsm.changeState(newState);
    }

    public void setTrigger(int triggerHash) {
        _animator.SetTrigger(triggerHash);
    }

    public void changeAnimation(int animationHash) {
        _animator.Play(animationHash);
    }

    public void setAnimSpeed(float speed) {
        _animator.speed = speed;
    }
    #endregion
}
