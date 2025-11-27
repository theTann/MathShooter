public enum GameState {
    playing,
    levelUp,
    gameOver,
}

[StateMapping(GameState.playing)]
public class GamePlayingState : IState<GameManager> {
    MonsterCenter _monsterCenter;
    NumberPanel _numberPanel;
    SkillManager _skillManager;

    void IState<GameManager>.onEnter(GameManager gameManager) {
        _monsterCenter = gameManager.getMonsterCenter();
        _numberPanel = gameManager.getNumberPanel();
        _numberPanel.setEnabled(true);
        _skillManager = gameManager.getSkillManager();
    }

    void IState<GameManager>.onExit(GameManager owner) {
        _numberPanel.setEnabled(false);
    }

    void IState<GameManager>.onUpdate(GameManager owner, float deltaTime) {
        _monsterCenter.update(deltaTime);
        _skillManager.updateSkillManager(deltaTime);
    }
}

[StateMapping(GameState.gameOver)]
public class GameOverState : IState<GameManager> {
    void IState<GameManager>.onEnter(GameManager owner) {
        Logger.debug("Game Over!");
    }

    void IState<GameManager>.onExit(GameManager owner) {
    }

    void IState<GameManager>.onUpdate(GameManager owner, float deltaTime) {
    }
}

[StateMapping(GameState.levelUp)]
public class SelectLevelupState : IState<GameManager> {
    void IState<GameManager>.onEnter(GameManager owner) {
        var levelupPanel = owner.getLevelupPanel();
        var skillManager = owner.getSkillManager();

        var skillList = skillManager.getRandomSkillList();

        levelupPanel.bindSkill(skillList);
        owner.getLevelupPanel().showPanel(true);

    }

    void IState<GameManager>.onExit(GameManager owner) {

    }

    void IState<GameManager>.onUpdate(GameManager owner, float deltaTime) {

    }
}
