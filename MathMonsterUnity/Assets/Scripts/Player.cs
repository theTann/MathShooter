using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting;

public enum PlayerState {
    none,
    alive,
    death,
}

public class Player {
    const float playerAttack = 10;

    public Bar playerBar;
    public Bar skillBar;
    public LongTouchDetector[] heroSlots;
    public GameManager gameManager;

    float _maxHp;
    float _currentHp;
    float _maxSkillPoint;
    float _currentSkillPoint;

    // int _currentTargetIdx;

    bool _protect = false;

    PlayerState _state;
    public PlayerState getState() { return _state; }

    public void init() {
        // _currentTargetIdx = 0;

        int idx = 0;
        foreach (var heroSlot in heroSlots) {
            heroSlot.onTap = onSkillUse;
            heroSlot.onLongPress = onSkillDesc;
            heroSlot.idx = idx;
            idx++;
        }
    }

    public void setState(PlayerState newState) {
        _state = newState;
    }

    public float getAttackPoint() {
        return playerAttack;
    }

    public void setMaxHp(float maxHp) {
        _maxHp = maxHp;
        playerBar.setMaxVal(_maxHp);
    }

    public bool setCurrentHp(float currentHp) {
        if(_protect == true) {
            _protect = false;
            return false;
        }

        _currentHp = Mathf.Max(0, currentHp);
        playerBar.setCurVal(_currentHp);

        if (_currentHp <= 0) {
            setState(PlayerState.death);
            return true;
        }
        return false;
    }

    public bool increaseHp(float deltaHp) {
        return setCurrentHp(_currentHp + deltaHp);
    }

    public void setMaxSp(float spMax) {
        _maxSkillPoint = spMax;
        skillBar.setMaxVal(_maxSkillPoint);
    }

    public void setCurrentSp(float sp) {
        _currentSkillPoint = Mathf.Min(sp, _maxSkillPoint);
        skillBar.setCurVal(_currentSkillPoint);
    }

    public void increaseSp(float sp) {
        setCurrentSp(_currentSkillPoint + sp);
    }

    public void onSkillUse(LongTouchDetector heroSlot) {
        if (_currentSkillPoint < _maxSkillPoint)
            return;

        int idx = heroSlot.idx;
        if (idx == 0) {
            AttackData data = new AttackData() {
                target = new List<Monster>() {
                    gameManager.getMonster(0),
                },
                attackPoint = 30,
                spIncrease = 0,
            };
            gameManager.processAttack(data);
        }
        else if (idx == 1) {
            _protect = true;
        }
        else if (idx == 2) {
            //int targetNumber = -1;
            //foreach (var monster in _monsters) {
            //    if (monster.getState() != MonsterState.death) {
            //        targetNumber = monster.getTargetNumber();
            //        break;
            //    }
            //}
            gameManager.highlightAvailableGem();
        }
        else if (idx == 3) {
            setCurrentHp(_currentHp + 30);
        }
        else if (idx == 4) {
            gameManager.getBottomPanel().refreshGem();
            //foreach (var monster in _monsters) {
            //    if (monster.getState() == MonsterState.idle) {
            //        monster.setStateTime(0);
            //    }
            //}
        }
        setCurrentSp(0);
    }

    public void onSkillDesc(LongTouchDetector heroSlot) {
        // int idx = heroSlot.idx;
    }
}
