using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine.UI;

public class SkillManager {
    private List<SkillObjectBase> _attachedSkills = new();
    private List<Type> _allSkillTypes = new();
    private List<SkillObjectBase> _randomResult = new(3) { null, null, null };
    private Dictionary<Type, SkillObjectBase> _skillObjectInstance = new();
    SkillObjectBase _activeSkill = null;
    Button _activeSkillButton = null;

    public void initSkillManager(Button _skillBtn) {
        // 일단 버튼으로 임시.
        _activeSkillButton = _skillBtn;
        _activeSkillButton.enabled = false;
        _skillBtn.onClick.AddListener(onActiveSkillUse);

        // 모든 SkillObjectBase의 리스트를 초기화 등록
        var assembly = Assembly.GetExecutingAssembly();
        var allTypes = assembly.GetTypes();
        var skillObjectsTypes = allTypes.Where(t =>
            t != typeof(SkillObjectBase) &&
            t.IsAbstract == false &&
            typeof(SkillObjectBase).IsAssignableFrom(t)
        );
        
        _allSkillTypes = skillObjectsTypes.ToList();

        foreach (var skillObjectType in _allSkillTypes) {
            Logger.debug($"{skillObjectType}");
        }
    }

    public void updateSkillManager(float deltaTime) {
        foreach (var skill in _attachedSkills) {
            skill.updateSkillObject(deltaTime);
        }
        if(_activeSkill != null) {
            if(_activeSkill.getSkillCooltime() >= 0.0f) {
                _activeSkillButton.enabled = false;
            }
            else {
                _activeSkillButton.enabled = true;
            }
        }
    }

    public void attachSkill(SkillObjectBase skillObject, Player owner, int skillLevel) {
        _attachedSkills.Add(skillObject);
        skillObject.onSkillAttached(owner, skillLevel);
    }

    public void detachSkill(SkillObjectBase skillObject) {
        skillObject.onSkillDetached();

        // todo : swap remove for performance
        _attachedSkills.Remove(skillObject);
    }

    public List<SkillObjectBase> getRandomSkillList() {
        _allSkillTypes.shuffle();
        _randomResult[0] = getOrCreateSkillObjectInstance(_allSkillTypes[0]);
        _randomResult[1] = getOrCreateSkillObjectInstance(_allSkillTypes[1]);
        _randomResult[2] = getOrCreateSkillObjectInstance(_allSkillTypes[2]);
        return _randomResult;
    }

    private SkillObjectBase getOrCreateSkillObjectInstance(Type skillObjectType) {
        SkillObjectBase result = null;
        if(_skillObjectInstance.TryGetValue(skillObjectType, out result) == false) {
            result = Activator.CreateInstance(skillObjectType) as SkillObjectBase;
            if(result == null) {
                Logger.error($"Create SkillObjectInstance fail. type : {skillObjectType}");
                return null;
            }
            _skillObjectInstance.Add(skillObjectType, result);
        }
        return result;
    }

    public void onSkillSelect(SkillObjectBase selectedSkill) {
        var player = GameManager.instance.getPlayer();

        int skillLevel = selectedSkill.getSkillLevel();
        if (skillLevel == 0) {
            attachSkill(selectedSkill, player, skillLevel + 1);
        }
        else {
            selectedSkill.onSkillLevelup();
        }

        if(selectedSkill.isActiveSkill() == true) {
            if(_activeSkill != null) {
                detachSkill(_activeSkill);
            }
            _activeSkill = selectedSkill;
        }
    }

    public void onActiveSkillUse() {
        ActiveSkillUsedEvent activeSkillEvent = new();
        activeSkillEvent.player = GameManager.instance.getPlayer();
        activeSkillEvent.skillObject = _activeSkill;
        GameEventBus.broadcast(ref activeSkillEvent);
    }
}
