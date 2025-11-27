public enum GameEventType {
    attack,
    monsterDie,
    levelup,
    monsterReachDestination,
    skillAttached,
    skillDetached,

    activeSkillUsed,

    max,
}

public struct AttackEvent : IGameEventData {
    public GameEventType eventType => GameEventType.attack;
    public Player attacker;
    public float damage;
    public Monster targetMonster;
}

public struct MonsterDieEvent : IGameEventData {
    public GameEventType eventType => GameEventType.monsterDie;
    public Player killer;
    public Monster dieMonster;
}

public struct LevelupEvent : IGameEventData {
    public GameEventType eventType => GameEventType.levelup;
    public Player player;
    public uint newLevel;
}

public struct SkillAttachedEvent : IGameEventData {
    public GameEventType eventType => GameEventType.skillAttached;
    public Player player;
    public SkillObjectBase skillObject;
}

public struct SkillDetachedEvent : IGameEventData {
    public GameEventType eventType => GameEventType.skillDetached;
    public Player player;
    public SkillObjectBase skillObject;
}

public struct ActiveSkillUsedEvent : IGameEventData {
    public GameEventType eventType => GameEventType.activeSkillUsed;
    public Player player;
    public SkillObjectBase skillObject;
}

