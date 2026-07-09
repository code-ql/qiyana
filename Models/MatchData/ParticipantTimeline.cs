using System.Collections.Generic;

namespace qiyana.Models.MatchData;

public class ParticipantTimeline
{
    public string Role { get; set; } = string.Empty;
    public string Lane { get; set; } = string.Empty;
    public Dictionary<string, double> CreepsPerMinDeltas { get; set; } = [];
    public Dictionary<string, double> XpPerMinDeltas { get; set; } = [];
    public Dictionary<string, double> GoldPerMinDeltas { get; set; } = [];
    public Dictionary<string, double> DamageTakenPerMinDeltas { get; set; } = [];
    public Dictionary<string, double>? CsDiffPerMinDeltas { get; set; }
    public Dictionary<string, double>? XpDiffPerMinDeltas { get; set; }
    public Dictionary<string, double>? DamageTakenDiffPerMinDeltas { get; set; }
}
