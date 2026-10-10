using System;
using System.Collections.Generic;
using System.Linq;

namespace TournamentPairings
{
    public enum Side
    {
        Axis = 0,
        Allies = 1
    }

    public sealed class Player
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string FullName { get; set; } = "";
        public Side Side { get; set; } = Side.Axis;
        public bool IsSpecial { get; set; } = false;

        // Tie-breaker value for random draw (stable per session/file)
        public int RandomTieBreaker { get; set; } = Guid.NewGuid().GetHashCode();

        // NEW: Late Entry (Delay) player
        // When true, player does not participate in Round 1 draw
        public bool IsLateEntry { get; set; } = false;

        // Round 2 participation (set in Round 2 Draw page)
        public bool IsWithdrawnForRound2 { get; set; } = false;

        // Results
        public int? Round1Score { get; set; }
        public int? Round1Units { get; set; }
        public int? Round2Score { get; set; }
        public int? Round2Units { get; set; }

        public int TotalScoreSoFar => (Round1Score ?? 0) + (Round2Score ?? 0);
        public int TotalUnits => (Round1Units ?? 0) + (Round2Units ?? 0);
    }

    public sealed class Match
    {
        public int RoundIndex { get; set; } // 1 or 2
        public int? TableNumber { get; set; } // null for BYE
        public Guid PlayerAId { get; set; }
        public Guid? PlayerBId { get; set; } // null => BYE

        public int? ScoreA { get; set; }
        public int? UnitsA { get; set; }
        public int? ScoreB { get; set; }
        public int? UnitsB { get; set; }

        // History flags only (not used in scoring)
        public bool WinnerA { get; set; }
        public bool WinnerB { get; set; }
        public bool IsDraw { get; set; }

        public bool IsBye => PlayerBId == null;
    }

    public sealed class TournamentState
    {
        public string TournamentName { get; set; } = "";
        public int TableCount { get; set; } = 1;

        public List<Player> Players { get; set; } = new();

        // Matches per round
        public List<Match> Round1Matches { get; set; } = new();
        public List<Match> Round2Matches { get; set; } = new();

        // File
        public string? FilePath { get; set; }
        public DateTime LastSavedUtc { get; set; } = DateTime.MinValue;

        // Progress flags
        public bool Round1Drawn { get; set; } = false;
        public bool Round1Completed { get; set; } = false;

        public bool Round2Drawn { get; set; } = false;
        public bool Round2Completed { get; set; } = false;

        public bool IsDirty { get; set; } = false;

        public IEnumerable<Player> GetStandings()
        {
            // Late Entry players that never joined should not appear
            var eligiblePlayers = Players
                .Where(p => !p.IsLateEntry || p.Round2Score.HasValue || p.Round2Units.HasValue)
                .ToList();

            // Withdrawn players appear at bottom
            var active = eligiblePlayers.Where(p => !p.IsWithdrawnForRound2).ToList();
            var withdrawn = eligiblePlayers.Where(p => p.IsWithdrawnForRound2).ToList();

            active = active
                .OrderByDescending(p => p.TotalScoreSoFar)
                .ThenByDescending(p => p.TotalUnits)
                .ThenBy(p => p.RandomTieBreaker)
                .ThenBy(p => p.FullName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            withdrawn = withdrawn
                .OrderBy(p => p.FullName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            return active.Concat(withdrawn);
        }
    }
}