using System.Collections.Generic;

namespace qiyana.Models.MatchData;

public class MatchHistoryResponse
{
    public MatchHistoryGames Games { get; set; } = new();
}

public class MatchHistoryGames
{
    public List<GameDetail> Games { get; set; } = [];
}
