namespace GameServer;

public class GameLogicDispatcher {
    public enum LogicType {
        startGame,
        activeTurnEnd,
        activeTurnStart,
    }

    public void gameLogicRun(JobQueue.GameLogicJobParam param) {
        switch (param.logicType) {
            case LogicType.startGame: onStartGame(param); break;
        }
    }

    private void onStartGame(JobQueue.GameLogicJobParam param) {
        Game game = (Game)param.sender;
        
        lock (game) {
            game.startGame();
        }
    }
}
