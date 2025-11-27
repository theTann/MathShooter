using System;
using UnityEngine;

public class Monster : MonoBehaviour {
    //private float currentDistance;
    //public float getCurrentDistance() { return currentDistance; }

    [SerializeField] private HpBar _hpBar;

    private MonsterData _monsterData;
    private float _currentSpeed;
    private float _currentHp;

    private Action _onReachedEnd;

    public void initMonster(MonsterData monsterData) {
        _monsterData = monsterData;
        
        _currentHp = _monsterData.hp;
        _currentSpeed = _monsterData.spd;
        refreshHpBar();

        GameEventBus.register<AttackEvent>(onAttackEvent);
    }

    public void destroyMonster() {
        GameEventBus.unregister<AttackEvent>(onAttackEvent);
    }

    public float updateMonster(float deltaTime, bool doMove) {
        Vector3 currentPos = transform.position;
        if (doMove == true) {
            float currentDelta = _currentSpeed * deltaTime;
            currentPos.z -= currentDelta;
            transform.position = currentPos;
            if (currentPos.z <= 0) {
                _onReachedEnd?.Invoke();
            }
        }
        return currentPos.z;
    }

    public void onAttackEvent(ref AttackEvent eventData) {
        if(eventData.targetMonster != this) {
            return;
        }

        _currentHp -= eventData.damage;
        refreshHpBar();
        if (_currentHp <= 0) {
            var dieEvent = new MonsterDieEvent();
            dieEvent.killer = eventData.attacker;
            dieEvent.dieMonster = this;
            GameEventBus.broadcast(ref dieEvent);
            return;
        }
    }

    private void refreshHpBar() {
        float ratio = _currentHp / _monsterData.hp;
        _hpBar.setRatio(ratio);
    }

    #region MonsterData Accessors
    public ulong getExpReward() {
        return _monsterData.gainExp;
    }

    public int getDamageToBase() {
        return _monsterData.damageToBase;
    }
    #endregion
}
