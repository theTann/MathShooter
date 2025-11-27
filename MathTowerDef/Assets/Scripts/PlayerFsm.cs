using UnityEngine;

public enum PlayerState {
    idle,
    attack,
}

[StateMapping(PlayerState.idle)]
public class PlayerIdle : IState<Player> {
    // float _stateTime = 0f;
    float _idleTimeForAttack = 0f;

    void IState<Player>.onEnter(Player owner) {
        // _stateTime = 0.0f;
        owner.changeAnimation(Player.idleAnimatorHash);
        _idleTimeForAttack = computeIdleTime(owner);
    }

    void IState<Player>.onExit(Player owner) {
    }

    void IState<Player>.onUpdate(Player owner, float deltaTime) {
        // _stateTime += deltaTime;

        if (_idleTimeForAttack < 0f) {
            owner.changeState(PlayerState.attack);
            return;
        }

        float remainIdleTime = _idleTimeForAttack - owner.getGlobalAttackTime();
        if (remainIdleTime <= 0) {
            owner.changeState(PlayerState.attack);
            return;
        }
    }

    private float computeIdleTime(Player owner) {
        float attackAnimTimeLength = owner.getAttackAnimDuration();
        float attackSpeed = owner.getAttackSpeed();

        float totalAttackTime = 1.0f / attackSpeed;

        // 애니메이션은 0.33초고 공격에 걸리는 시간이 1초라고 가정하면, idleTime은 0.67초가 되어야 함.
        float idleTime = totalAttackTime - attackAnimTimeLength;
        idleTime = Mathf.Max(idleTime, 0.0f); // 음수가 되는걸 방지.

        float remainIdleTime = idleTime - owner.getGlobalAttackTime();

        return remainIdleTime;
    }
}

[StateMapping(PlayerState.attack)]
public class PlayerAttack : IState<Player> {
    float _stateTime;
    float _attackStateTime = 0.0f;

    void IState<Player>.onEnter(Player owner) {
        _stateTime = 0;

        Monster targetMonster = GameManager.instance.getMonsterCenter().getFrontMonster();
        if(targetMonster == null) {
            owner.changeState(PlayerState.idle);
            return;
        }

        if(owner.getAmmoCount() <= 0) {
            owner.showNoAmmo();
            owner.changeState(PlayerState.idle);
            return;
        }
        
        owner.setCurrentTarget(targetMonster);

        float animSpeed = computeAttackAnimSpeed(owner);
        owner.setAnimSpeed(animSpeed);
        owner.changeAnimation(Player.attackAnimatorHash);
    }

    void IState<Player>.onExit(Player owner) {
        owner.setCurrentTarget(null);
        owner.resetGlobalAttackTime();
        owner.setAnimSpeed(1.0f);
    }

    void IState<Player>.onUpdate(Player owner, float deltaTime) {
        _stateTime += deltaTime;
        
        if(_stateTime >= _attackStateTime) {
            owner.changeState(PlayerState.idle);
            return;
        }
    }

    private float computeAttackAnimSpeed(Player owner) {
        float attackAnimTimeLength = owner.getAttackAnimDuration();
        float attackSpeed = owner.getAttackSpeed();

        float totalAttackTime = 1.0f / attackSpeed;

        // 애니메이션은 0.33초고 공격에 걸리는 시간이 1초라고 가정하면, idleTime은 0.67초가 되어야 함.
        float idleTime = totalAttackTime - attackAnimTimeLength;
        idleTime = Mathf.Max(idleTime, 0.0f); // 음수가 되는걸 방지.

        // 애니메이션은 0.33초고 공격에 걸리는시간이 0.1초라면, 애니메이션 속도를 3.3배로 올려서 0.11초만에 재생되도록 해야함.
        float onlyAttackTime = totalAttackTime - idleTime;
        onlyAttackTime = Mathf.Max(onlyAttackTime, 0.01f); // 너무 짧아지는걸 방지.

        float attackAnimSpeed = attackAnimTimeLength / onlyAttackTime;
        _attackStateTime = attackAnimTimeLength / attackAnimSpeed;

        return attackAnimSpeed;
    }
}
