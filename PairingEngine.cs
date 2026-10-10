using System;
using System.Collections.Generic;
using System.Linq;

namespace TournamentPairings
{
    public static class PairingEngine
    {
        private static readonly Random Rng = new Random();

        public static (List<Match> matches, Guid? byePlayerId) GenerateRound1(
            TournamentState state,
            Guid? voluntaryByePlayerId
        )
        {
            var active = state.Players
                .Where(p => !p.IsLateEntry)
                .ToList();

            bool byeNeeded = (active.Count % 2 == 1);
            Guid? byeId = null;

            if (byeNeeded)
            {
                if (voluntaryByePlayerId.HasValue)
                {
                    var vp = active.FirstOrDefault(p => p.Id == voluntaryByePlayerId.Value);
                    if (vp == null) throw new InvalidOperationException("Voluntary BYE player not found.");
                    if (vp.IsSpecial) throw new InvalidOperationException("Special players cannot be BYE.");
                    byeId = vp.Id;
                    active.Remove(vp);
                }
                else
                {
                    var candidates = active.Where(p => !p.IsSpecial).ToList();
                    if (candidates.Count == 0)
                        throw new InvalidOperationException("BYE is required but all players are Special.");

                    var pick = candidates[Rng.Next(candidates.Count)];
                    byeId = pick.Id;
                    active.Remove(pick);
                }
            }

            var axis = active.Where(p => p.Side == Side.Axis).ToList();
            var allies = active.Where(p => p.Side == Side.Allies).ToList();

            Shuffle(axis);
            Shuffle(allies);

            var pairs = new List<(Player A, Player B)>();

            while (axis.Count > 0 && allies.Count > 0)
            {
                var a = axis[0]; axis.RemoveAt(0);
                var b = allies[0]; allies.RemoveAt(0);
                pairs.Add((a, b));
            }

            var leftovers = axis.Concat(allies).ToList();
            Shuffle(leftovers);

            while (leftovers.Count >= 2)
            {
                var a = leftovers[0]; leftovers.RemoveAt(0);
                var b = leftovers[0]; leftovers.RemoveAt(0);
                pairs.Add((a, b));
            }

            if (leftovers.Count == 1)
                throw new InvalidOperationException("Unexpected leftover player after pairing.");

            var matches = new List<Match>();
            int table = 1;

            foreach (var (pa, pb) in pairs)
            {
                matches.Add(new Match
                {
                    RoundIndex = 1,
                    TableNumber = table,
                    PlayerAId = pa.Id,
                    PlayerBId = pb.Id
                });
                table++;
            }

            if (byeId.HasValue)
            {
                matches.Add(new Match
                {
                    RoundIndex = 1,
                    TableNumber = null,
                    PlayerAId = byeId.Value,
                    PlayerBId = null
                });
            }

            return (matches, byeId);
        }

        /// <summary>
        /// Round 2 pairing:
        /// - Excludes withdrawn players (IsWithdrawnForRound2=true)
        /// - Sorts Axis and Allies by Round1Score descending (ties randomized)
        /// - Pairs #1 vs #1, #2 vs #2... then leftovers same-side
        /// - Applies BYE if odd (voluntary if provided), Special cannot be BYE
        /// - Tries to avoid same table as Round 1 for both players (best effort)
        /// Returns: matches + bye id + warning string (empty if none)
        /// </summary>
        public static (List<Match> matches, Guid? byePlayerId, string warning) GenerateRound2(
            TournamentState state,
            Guid? voluntaryByePlayerId
        )
        {
            if (!state.Round1Completed)
                throw new InvalidOperationException("Round 1 results must be LOCKED before Round 2 draw.");

            // Active for R2 = not withdrawn
            var active = state.Players.Where(p => !p.IsWithdrawnForRound2).ToList();
            if (active.Count < 2)
                throw new InvalidOperationException("Not enough active players for Round 2.");

            // Map player -> table in round1 (for constraint)
            var r1TableMap = BuildPlayerToTableMap(state.Round1Matches);

            // Who had BYE in R1 (if any)
            Guid? r1Bye = state.Round1Matches.FirstOrDefault(m => m.IsBye)?.PlayerAId;

            bool byeNeeded = (active.Count % 2 == 1);
            Guid? byeId = null;

            if (byeNeeded)
            {
                // voluntary chosen
                if (voluntaryByePlayerId.HasValue)
                {
                    var vp = active.FirstOrDefault(p => p.Id == voluntaryByePlayerId.Value);
                    if (vp == null) throw new InvalidOperationException("Voluntary BYE player not found (or withdrawn).");
                    if (vp.IsSpecial) throw new InvalidOperationException("Special players cannot be BYE.");

                    // Prefer that R1 BYE plays in R2 if possible
                    if (r1Bye.HasValue && vp.Id == r1Bye.Value)
                    {
                        var alt = active.Where(p => !p.IsSpecial && p.Id != r1Bye.Value).ToList();
                        if (alt.Count > 0)
                            throw new InvalidOperationException("The player who had BYE in Round 1 should play in Round 2. Choose another player for BYE.");
                    }

                    byeId = vp.Id;
                    active.Remove(vp);
                }
                else
                {
                    var candidates = active.Where(p => !p.IsSpecial).ToList();
                    if (candidates.Count == 0)
                        throw new InvalidOperationException("BYE is required but all active players are Special.");

                    // Prefer not same BYE as round1 if possible
                    var preferred = candidates;
                    if (r1Bye.HasValue)
                    {
                        var notSame = candidates.Where(p => p.Id != r1Bye.Value).ToList();
                        if (notSame.Count > 0) preferred = notSame;
                    }

                    var pick = preferred[Rng.Next(preferred.Count)];
                    byeId = pick.Id;
                    active.Remove(pick);
                }
            }

            // Build side lists
            var axis = active.Where(p => p.Side == Side.Axis).ToList();
            var allies = active.Where(p => p.Side == Side.Allies).ToList();

            // Sort by Round1Score desc, randomize ties
            axis = SortByScoreWithRandomTies(axis);
            allies = SortByScoreWithRandomTies(allies);

            var pairs = new List<(Player A, Player B)>();

            // Pair rank vs rank cross-side
            int n = Math.Min(axis.Count, allies.Count);
            for (int i = 0; i < n; i++)
                pairs.Add((axis[i], allies[i]));

            // Leftovers same-side
            var leftovers = axis.Skip(n).Concat(allies.Skip(n)).ToList();

            // Keep leftovers order somewhat random but also score-ish:
            leftovers = SortByScoreWithRandomTies(leftovers);

            while (leftovers.Count >= 2)
            {
                var a = leftovers[0]; leftovers.RemoveAt(0);
                var b = leftovers[0]; leftovers.RemoveAt(0);
                pairs.Add((a, b));
            }

            if (leftovers.Count == 1)
            {
                // should not happen after BYE removal
                throw new InvalidOperationException("Unexpected leftover player after pairing (Round 2).");
            }

            // Create matches with table assignment (best effort to avoid same table)
            var matches = new List<Match>();
            var usedTables = new HashSet<int>();
            string warning = "";

            int matchIndex = 0;
            foreach (var (pa, pb) in pairs)
            {
                matchIndex++;

                int? assigned = TryAssignTable(
                    state.TableCount,
                    usedTables,
                    r1TableMap.TryGetValue(pa.Id, out var ta) ? ta : (int?)null,
                    r1TableMap.TryGetValue(pb.Id, out var tb) ? tb : (int?)null
                );

                if (assigned == null)
                {
                    // If tableCount < matchCount or constraints impossible, fall back:
                    assigned = GetAnyTable(state.TableCount, usedTables);
                    warning = string.IsNullOrWhiteSpace(warning)
                        ? "Some Round 2 matches could not avoid the same table as Round 1 (best effort applied)."
                        : warning;
                }

                matches.Add(new Match
                {
                    RoundIndex = 2,
                    TableNumber = assigned,
                    PlayerAId = pa.Id,
                    PlayerBId = pb.Id
                });
            }

            if (byeId.HasValue)
            {
                matches.Add(new Match
                {
                    RoundIndex = 2,
                    TableNumber = null,
                    PlayerAId = byeId.Value,
                    PlayerBId = null
                });
            }

            return (matches, byeId, warning);
        }

        private static Dictionary<Guid, int> BuildPlayerToTableMap(List<Match> matches)
        {
            var map = new Dictionary<Guid, int>();
            foreach (var m in matches)
            {
                if (!m.TableNumber.HasValue) continue;
                map[m.PlayerAId] = m.TableNumber.Value;
                if (m.PlayerBId.HasValue) map[m.PlayerBId.Value] = m.TableNumber.Value;
            }
            return map;
        }

        private static int? TryAssignTable(int tableCount, HashSet<int> used, int? avoidA, int? avoidB)
        {
            for (int t = 1; t <= tableCount; t++)
            {
                if (used.Contains(t)) continue;
                if (avoidA.HasValue && t == avoidA.Value) continue;
                if (avoidB.HasValue && t == avoidB.Value) continue;
                used.Add(t);
                return t;
            }
            return null;
        }

        private static int GetAnyTable(int tableCount, HashSet<int> used)
        {
            for (int t = 1; t <= tableCount; t++)
            {
                if (!used.Contains(t))
                {
                    used.Add(t);
                    return t;
                }
            }

            return 1;
        }

        private static List<Player> SortByScoreWithRandomTies(List<Player> players)
        {
            var grouped = players
                .GroupBy(p => p.Round1Score ?? 0)
                .OrderByDescending(g => g.Key)
                .ToList();

            var result = new List<Player>();
            foreach (var g in grouped)
            {
                var tieGroup = g.ToList();
                Shuffle(tieGroup);
                result.AddRange(tieGroup);
            }
            return result;
        }

        private static void Shuffle<T>(List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}