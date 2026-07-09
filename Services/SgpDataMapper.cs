using System;
using System.Collections.Generic;
using System.Linq;
using qiyana.Models.MatchData;
using qiyana.Models.SgpData;

namespace qiyana.Services;

public static class SgpDataMapper
{
    public static GameDetail ToGameDetail(SgpGameSummaryJsonLol sgp, string? currentPuuid)
    {
        var detail = new GameDetail
        {
            GameId = sgp.GameId,
            GameMode = sgp.GameMode,
            GameDuration = sgp.GameDuration,
            GameCreation = sgp.GameCreation,
            GameVersion = sgp.GameVersion,
            QueueId = sgp.QueueId,
            MapId = sgp.MapId,
        };

        MapParticipants(sgp, detail, currentPuuid);
        MapTeams(sgp, detail);
        detail.ApplyTeamHighlights();

        return detail;
    }

    private static void MapParticipants(SgpGameSummaryJsonLol sgp, GameDetail detail, string? currentPuuid)
    {
        foreach (var sp in sgp.Participants)
        {
            var participant = new Participant
            {
                ParticipantId = sp.ParticipantId,
                ChampionId = sp.ChampionId,
                ChampionName = sp.ChampionName,
                Spell1Id = sp.Spell1Id,
                Spell2Id = sp.Spell2Id,
                TeamId = sp.TeamId,
                SummonerName = sp.RiotIdGameName,
                SummonerTagline = sp.RiotIdTagline,
                Puuid = sp.Puuid,
                IsCurrentSummoner = string.Equals(sp.Puuid, currentPuuid, StringComparison.OrdinalIgnoreCase),
                Stats = new ParticipantStats
                {
                    Kills = sp.Kills,
                    Deaths = sp.Deaths,
                    Assists = sp.Assists,
                    ChampLevel = sp.ChampLevel,
                    TotalDamageDealtToChampions = sp.TotalDamageDealtToChampions,
                    TotalDamageTaken = sp.TotalDamageTaken,
                    GoldEarned = sp.GoldEarned,
                    TotalMinionsKilled = sp.TotalMinionsKilled,
                    VisionScore = sp.VisionScore,
                    Win = sp.Win,
                    Item0 = sp.Item0,
                    Item1 = sp.Item1,
                    Item2 = sp.Item2,
                    Item3 = sp.Item3,
                    Item4 = sp.Item4,
                    Item5 = sp.Item5,
                    Item6 = sp.Item6,
                    DoubleKills = sp.DoubleKills,
                    TripleKills = sp.TripleKills,
                    QuadraKills = sp.QuadraKills,
                    PentaKills = sp.PentaKills,
                    FirstBloodKill = sp.FirstBloodKill,
                    FirstBloodAssist = sp.FirstBloodAssist,
                    FirstTowerKill = sp.FirstTowerKill,
                    FirstTowerAssist = sp.FirstTowerAssist,
                    MagicDamageDealtToChampions = sp.MagicDamageDealtToChampions,
                    PhysicalDamageDealtToChampions = sp.PhysicalDamageDealtToChampions,
                    TrueDamageDealtToChampions = sp.TrueDamageDealtToChampions,
                    TotalHeal = sp.TotalHeal,
                    TotalDamageDealt = sp.TotalDamageDealt,
                    TotalTimeCrowdControlDealt = sp.TotalTimeCCDealt,
                    VisionWardsBoughtInGame = sp.VisionWardsBoughtInGame,
                    SightWardsBoughtInGame = sp.SightWardsBoughtInGame,
                    WardsPlaced = sp.WardsPlaced,
                    WardsKilled = sp.WardsKilled,
                    NeutralMinionsKilled = sp.NeutralMinionsKilled,
                    NeutralMinionsKilledTeamJungle = sp.TotalAllyJungleMinionsKilled,
                    NeutralMinionsKilledEnemyJungle = sp.TotalEnemyJungleMinionsKilled,
                    DamageDealtToObjectives = sp.DamageDealtToObjectives,
                    DamageDealtToTurrets = sp.DamageDealtToTurrets,
                    DamageSelfMitigated = sp.DamageSelfMitigated,
                    TimeCCingOthers = sp.TimeCCingOthers,
                    LongestTimeSpentLiving = sp.LongestTimeSpentLiving,
                    TurretKills = sp.TurretKills,
                    InhibitorKills = sp.InhibitorKills,
                    TotalUnitsHealed = sp.TotalUnitsHealed,
                    ConsumablesPurchased = sp.ConsumablesPurchased,
                    ItemsPurchased = sp.ItemsPurchased,
                    LargestMultiKill = sp.LargestMultiKill,
                    LargestKillingSpree = sp.LargestKillingSpree,
                    LargestCriticalStrike = sp.LargestCriticalStrike,
                },
                Timeline = new ParticipantTimeline
                {
                    Lane = sp.Lane,
                    Role = sp.Role,
                }
            };

            // SGP doesn't provide per-keystone/perk-style info in the summary
            // These will be left at defaults (0)
            // Perk details can be fetched separately if needed

            detail.Participants.Add(participant);

            detail.ParticipantIdentities.Add(new ParticipantIdentity
            {
                ParticipantId = sp.ParticipantId,
                Player = new PlayerInfo
                {
                    SummonerName = sp.RiotIdGameName,
                    TagLine = sp.RiotIdTagline,
                    SummonerId = sp.SummonerId,
                    Puuid = sp.Puuid,
                    ProfileIcon = sp.ProfileIcon,
                }
            });
        }
    }

    private static void MapTeams(SgpGameSummaryJsonLol sgp, GameDetail detail)
    {
        foreach (var st in sgp.Teams)
        {
            var team = new Team
            {
                TeamId = st.TeamId,
                Win = st.Win ? "Win" : "Fail",
                Bans = st.Bans.Select(b => new Ban
                {
                    ChampionId = b.ChampionId,
                    PickTurn = b.PickTurn,
                }).ToList(),
            };

            if (st.Objectives is not null)
            {
                var o = st.Objectives;
                team.FirstBlood = o.Champion?.First ?? false;
                team.FirstTower = o.Tower?.First ?? false;
                team.FirstBaron = o.Baron?.First ?? false;
                team.FirstDragon = o.Dragon?.First ?? false;
                team.TowerKills = o.Tower?.Kills ?? 0;
                team.InhibitorKills = o.Inhibitor?.Kills ?? 0;
                team.BaronKills = o.Baron?.Kills ?? 0;
                team.DragonKills = o.Dragon?.Kills ?? 0;
                team.RiftHeraldKills = o.RiftHerald?.Kills ?? 0;
            }

            detail.Teams.Add(team);
        }
    }
}
