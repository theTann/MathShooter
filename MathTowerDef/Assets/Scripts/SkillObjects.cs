using System.Collections.Generic;

public class SkillObjectBase {
    protected Player _owner;
    protected int _skillLevel;

    public int getSkillLevel() { return _skillLevel; }

    public virtual string getSkillName() { return string.Empty; }
    public virtual bool isActiveSkill() { return false; }
    public virtual void computeSkillVariables() {}
    public virtual void updateSkillObject(float deltaTime) {}
    public virtual void destroySkillObject() {}
    public virtual string getSkillDescription() { return string.Empty; }
    public virtual float getSkillCooltime() { return 0f; }
    public virtual void onSkillAttached(Player owner, int skillLevel) {
        _owner = owner;
        _skillLevel = skillLevel;

        computeSkillVariables();
    }

    public virtual void onSkillDetached() {
        _skillLevel = 0;
        _owner = null;
    }

    // 최초 attach 시에도 호출됨
    public virtual void onSkillLevelup() {
        _skillLevel += 1;
        computeSkillVariables();
    }
}

public class AttackPowerSkill : SkillObjectBase {
    const float increaseAttackPowerPerLevel = 5f;

    float _extraAttackPower = 0f;

    public override void onSkillAttached(Player owner, int skillLevel) {
        // onLevelup도 호출되므로 레벨 -1로 초기화
        base.onSkillAttached(owner, skillLevel);
        GameEventBus.registerPreProcess<AttackEvent>(onAttackEvent);
    }

    public override void onSkillDetached() {
        base.onSkillDetached();
        GameEventBus.unregisterPreProcess<AttackEvent>(onAttackEvent);
    }

    public override void computeSkillVariables() {
        base.computeSkillVariables();
        _extraAttackPower = formula(_skillLevel);
    }

    public float formula(int skillLevel) {
        return skillLevel * increaseAttackPowerPerLevel;
    }

    public void onAttackEvent(ref AttackEvent data) {
        data.damage += _extraAttackPower;
    }

    public override string getSkillDescription() {
        int targetLevel = _skillLevel + 1;
        return $"Level :{targetLevel}\nIncreases attack power {formula(targetLevel)}.";
    }
}

public class AttackSpeedSkill : SkillObjectBase {
    float _extraAttackSpeed = 0f;
    public override void onSkillAttached(Player owner, int skillLevel) {
        base.onSkillAttached(owner, skillLevel);
        _owner.addAttackSpeed(_extraAttackSpeed);
    }

    public override void onSkillDetached() {
        _owner.addAttackSpeed(-_extraAttackSpeed);
        
        // _owner = null을 하기때문에 뒤에 불려야함.
        base.onSkillDetached();
    }

    public override void computeSkillVariables() {
        base.computeSkillVariables();
        _extraAttackSpeed = formula(_skillLevel);
    }

    private float formula(int skillLevel) {
        return skillLevel * 0.5f;
    }

    public override string getSkillDescription() {
        int targetLevel = _skillLevel + 1;
        return $"Level :{targetLevel}\nIncreases attack speed by {formula(targetLevel)}.";
    }
}

public class FreezeeSkill : SkillObjectBase {
    float _freezeeDuration = 0f;
    float _skillCooltime = 20f;
    float _remainCooltime = 0f;
    float _remainFreezingTime = 0f;

    public override bool isActiveSkill() {
        return true;
    }

    public override void onSkillAttached(Player owner, int skillLevel) {
        base.onSkillAttached(owner, skillLevel);
        
        GameEventBus.registerPreProcess<ActiveSkillUsedEvent>(onActiveSkillUsed);
    }

    public override void onSkillDetached() {
        GameEventBus.unregisterPreProcess<ActiveSkillUsedEvent>(onActiveSkillUsed);

        base.onSkillDetached();
    }

    public override void computeSkillVariables() {
        base.computeSkillVariables();
        _freezeeDuration = formula(_skillLevel);
    }

    float formula(int skillLevel) {
        return skillLevel * 10; // 예시로 레벨당 0.5초의 프리즈 지속시간 증가
    }

    public void onActiveSkillUsed(ref ActiveSkillUsedEvent data) {
        // do freezee logic here
        var monsterCenter = GameManager.instance.getMonsterCenter();
        monsterCenter.setFreezeMove(true);
        _remainFreezingTime = _freezeeDuration;
    }

    public override void updateSkillObject(float deltaTime) {
        base.updateSkillObject(deltaTime);
        
        if(_remainFreezingTime > 0f) {
            _remainFreezingTime -= deltaTime;
            if (_remainFreezingTime <= 0f) {
                var monsterCenter = GameManager.instance.getMonsterCenter();
                monsterCenter.setFreezeMove(false);
            }
            _remainCooltime = _skillCooltime;
        } 
        else {
            _remainCooltime -= deltaTime;
        }
    }

    public override float getSkillCooltime() {
        return _remainCooltime;
    }

    public override string getSkillDescription() {
        int targetLevel = _skillLevel + 1;
        return $"Level :{targetLevel}\nFreezes enemies for {formula(targetLevel)} seconds on attack.";
    }

    public override string getSkillName() {
        return "Freeze!";
    }
}
