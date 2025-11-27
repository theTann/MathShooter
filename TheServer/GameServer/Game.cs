using System.Data;
using System.Diagnostics;

namespace GameServer;

public class Player {
    public int idx { get; set; }
    public int score;
    
    public enum State {
        loading,
        loadingComplete,
    }

    public State state;

    public Player(int playerIdx) {
        idx = playerIdx;
        state = State.loading;
        score = 0;
    }
}

public class BottomPanelCandidate {
    public int count;
    public List<(int x, int y, int w, int h)> positions;
    public List<int> gemCount;
}

public class Game {
    private const int _tickIntervalMs = 33; // 30 fps
    private static readonly long _intervalTicks = (long)(Stopwatch.Frequency * (_tickIntervalMs / 1000.0));
    public static readonly double tickToMs = 1000.0 / Stopwatch.Frequency;
    
    private const int _boardWidth = 9;
    private const int _boardHeight = 9;
    private const int _maxPlayer = 2;

    public long lastTick { get; private set; }
    public bool runUpdate = false;
    
    public enum State {
        wait,
        waitForLoading,
        activeTurn,
        settlementTurn,
    }

    private readonly Player[] _players;
    private readonly Dictionary<Session, Player> _playerDict = new(); 
    private readonly int[] _board;
    private readonly Random _random;
    private readonly Dictionary<int, BottomPanelCandidate> _candidates = new Dictionary<int, BottomPanelCandidate>();
    private State _state;
    
    public Game() {
        changeState(State.wait);
        lastTick = Stopwatch.GetTimestamp();
        _board = new  int[_boardWidth * _boardHeight];
        _random = new Random();
        _players = new Player[_maxPlayer];
        for (int i = 0; i < _maxPlayer; i++) {
            _players[i] = new Player(i);
        }
    }

    public void changeState(State newState) {
        _state = newState;
        
        // 일단 업데이트 테스트.
        // if (_state == State.wait) {
        //     runUpdate = false;
        // }

    }

    public void init(SessionList players) {
        if (players.Count > _maxPlayer) {
            Console.Error.WriteLine("Too many players");
            return;
        }

        // bind session
        for (int i = 0; i < players.Count; i++) {
            Session playerSession = players[i];
            _playerDict.TryAdd(playerSession, _players[i]);
        }
        
        resetGame();
    }

    public void onExitGame() {
        _playerDict.Clear();
        changeState(State.wait);
    }

    public bool tryUpdate(long nowTick) {
        if (runUpdate == false)
            return false;

        long deltaTick = nowTick - lastTick;
        if (deltaTick < _intervalTicks) {
            return false;
        }

        double deltaTimeInMs = deltaTick * tickToMs;
        
        lock (this) {
            update(deltaTimeInMs);
        }
        
        lastTick = Stopwatch.GetTimestamp();

        return true;
    }

    private void update(double deltaTimeInMs) {
        // Stopwatch stopwatch = Stopwatch.StartNew();
        // 10ms~15ms
        int sum = 0;
        for (int i = 0; i < 5000000; i++) {
            sum += i;
        }

        // stopwatch.Stop();
        // Console.WriteLine($"update : {stopwatch.ElapsedMilliseconds}ms");
        // Console.WriteLine($"deltaTime : {deltaTimeInMs}ms");
    }
    
    public bool onLoadComplete(Session session) {
        _playerDict.TryGetValue(session, out Player? player);
        if(player == null) {
            // todo : what?
            return false;
        }

        player.state = Player.State.loadingComplete;
        return checkReadyToPlay();
    }

    private bool checkReadyToPlay() {
        foreach (var kvp in _players) {
            if (kvp.state != Player.State.loadingComplete)
                return false;
        }

        return true;
    }

    public void startGame() {
        Console.WriteLine($"gameStart");
        _state = State.activeTurn;
        GameServer.jobQueue.push(new JobQueue.GameLogicJobParam() {
            logicType = GameLogicDispatcher.LogicType.activeTurnEnd,
            sender = this,
        });
    }

    public int[] getCurrentBoard() {
        return _board;
    }

    private void resetGame() {
        changeState(State.waitForLoading);
        
        shuffleBoard();
        // computeCandidates();
    }

    public void onLeavePlayer(Session session) {
        _playerDict.Remove(session);
        if (_playerDict.Count == 0) {
            changeState(State.wait);
        }
    }
    
    void shuffleBoard() {
        for (int y = 0; y < _boardHeight; y++) {
            for (int x = 0; x < _boardWidth; x++) {
                _board[y * _boardWidth + x] = _random.Next(1, 10);
            }
        }
    }

    private int getValue(int x, int y) {
        if (x < 0 || y < 0) return -1;
        if (x >= _boardWidth || y >= _boardHeight) return -1;
        int idx = x + _boardWidth * y;
        return _board[idx];
    }
    
    public void computeCandidates() {
        // todo : gc 최적화.
        Stopwatch sw = Stopwatch.StartNew();
        
        HashSet<(int addX, int addY)> addPostionList = new HashSet<(int addX, int addY)>();

        for (int y = 0; y < _boardHeight; ++y) {
            for (int x = 1; x < _boardWidth; ++x) {
                addPostionList.Add((x, y));
                addPostionList.Add((y, x));
            }
        }

        _candidates.Clear();
        for (int y = 0; y < _boardHeight; ++y) {
            for (int x = 0; x < _boardWidth; ++x) {
                foreach (var (addX, addY) in addPostionList) {
                    if (x + addX >= _boardWidth)
                        continue;
                    if (y + addY >= _boardHeight)
                        continue;

                    int sum = 0;
                    int gemCount = 0;

                    for (int checkYIdx = y; checkYIdx <= y + addY; ++checkYIdx) {
                        for (int checkXIdx = x; checkXIdx <= x + addX; ++checkXIdx) {
                            
                            int value = getValue(checkXIdx, checkYIdx);
                            if (value == -1)
                                continue;
                            
                            int currentVal = value;
                            sum += currentVal;
                            gemCount++;
                        }
                    }

                    bool exist = _candidates.TryGetValue(sum, out BottomPanelCandidate statisticsVal);
                    if (exist == false) {
                        statisticsVal = new BottomPanelCandidate();
                        statisticsVal.count = 1;
                        statisticsVal.positions = new List<(int x, int y, int w, int h)>();
                        statisticsVal.gemCount = new List<int>();

                        statisticsVal.positions.Add((x, y, addX, addY));
                        statisticsVal.gemCount.Add(gemCount);
                        _candidates.Add(sum, statisticsVal);
                    }
                    else {
                        statisticsVal.count++;
                        statisticsVal.positions.Add((x, y, addX, addY));
                        statisticsVal.gemCount.Add(gemCount);
                    }
                }
            }
        }
        sw.Stop();
        Console.WriteLine($"{sw.ElapsedMilliseconds}ms elapsed");
    }

    public int removeGems(Session session, List<byte> toRemove) {
        // todo : 사각형이 왔는지 체크
        int sum = 0;
        foreach (var pos in toRemove) {
            int idx = (int)pos; // pos.x + _boardWidth * pos.y;
            
            // 이건 동시에 요청이 오면 가능한 상황.
            if (_board[idx] == -1)
                return -1;
            
            sum += _board[idx];
            _board[idx] = -1;
        }

        if (sum != 10) {
            // todo : 이건 해킹아닌가?
            return -1;
        }
        // todo : 남의 점수도 보내줘야할듯?
        var player = _playerDict[session];
        player.score += sum;
        return player.score;
    }
}
