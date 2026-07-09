namespace qiyana.Models;

public enum GameflowPhase
{
    None,
    Lobby,
    Matchmaking,
    ReadyCheck,
    ChampSelect,
    InProgress,
    Reconnect,
    WaitingForStats,
    PreEndOfGame,
    EndOfGame
}

public static class GameflowPhaseExtensions
{
    public static GameflowPhase Parse(string phase) => phase switch
    {
        "None" => GameflowPhase.None,
        "Lobby" => GameflowPhase.Lobby,
        "Matchmaking" => GameflowPhase.Matchmaking,
        "ReadyCheck" => GameflowPhase.ReadyCheck,
        "ChampSelect" => GameflowPhase.ChampSelect,
        "InProgress" => GameflowPhase.InProgress,
        "Reconnect" => GameflowPhase.Reconnect,
        "WaitingForStats" => GameflowPhase.WaitingForStats,
        "PreEndOfGame" => GameflowPhase.PreEndOfGame,
        "EndOfGame" => GameflowPhase.EndOfGame,
        _ => GameflowPhase.None
    };
}
