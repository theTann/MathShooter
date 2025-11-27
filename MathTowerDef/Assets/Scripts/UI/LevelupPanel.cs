using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;

public class LevelupPanel : MonoBehaviour {
    [SerializeField] private TMP_Text[] _skillDescTxt;

    List<SkillObjectBase> _skills;
    int _bindIdx;

    public void bindSkill(List<SkillObjectBase> randomSkills) {
        _skillDescTxt[0].text = randomSkills[0].getSkillDescription();
        _skillDescTxt[1].text = randomSkills[1].getSkillDescription();
        _skillDescTxt[2].text = randomSkills[2].getSkillDescription();

        _skills = randomSkills.ToList();
    }

    public void showPanel(bool show) {
        gameObject.SetActive(show);
        _bindIdx = -1;
    }

    public void onLevelupBtn0() {
        _bindIdx = 0;
        onLevelup();
    }

    public void onLevelupBtn1() {
        _bindIdx = 1;
        onLevelup();
    }

    public void onLevelupBtn2() {
        _bindIdx = 2;
        onLevelup();
    }

    private void onLevelup() {
        
        var skillManager = GameManager.instance.getSkillManager();

        var skill = _skills[_bindIdx];

        skillManager.onSkillSelect(skill);

        showPanel(false);

        GameManager.instance.changeState(GameState.playing);
    }
}
