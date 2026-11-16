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

            AvoidSpecialVsSpecial(pairs);
            EnsureEnoughTables(state, pairs.Count);

            // Tables 1..(number of matches), randomly assigned
            var tables = Enumerable.Range(1, pairs.Count).ToList();
            Shuffle(tables);

            var matches = new List<Match>();

            for (int i = 0; i < pairs.Count; i++)
            {
                matches.Add(new Match
                {
                    RoundIndex = 1,
                    TableNumber = tables[i],
                    PlayerAId = pairs[i].A.Id,
                    PlayerBId = pairs[i].B.Id
                });
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
        /// Round 2 pairing (Swiss style):
        /// - Excludes withdrawn players (IsWithdrawnForRound2=true)
        /// - Applies BYE if odd (voluntary if provided), Special cannot be BYE
        /// - Ranks by Round1Score, then Round1Units, then draw (RandomTieBreaker, same as standings)
        /// - Pairs by ranking: 1-3, 2-4, 5-7, 6-8, ... (see PairByRanking for the rules)
        /// - Tables 1..(number of matches); optionally nobody plays on their Round 1 table (see AssignRound2Tables)
        /// Returns: matches + bye id + warning string (empty if none)
        /// </summary>
        public static (List<Match> matches, Guid? byePlayerId, string warning) GenerateRound2(
            TournamentState state,
            Guid? voluntaryByePlayerId,
            bool preferAxisVsAllies = false,
            bool avoidRound1Tables = true
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

            if (active.Count % 2 == 1)
            {
                // should not happen after BYE removal
                throw new InvalidOperationException("Unexpected leftover player after pairing (Round 2).");
            }

            var ranked = active
                .OrderByDescending(p => p.Round1Score ?? 0)
                .ThenByDescending(p => p.Round1Units ?? 0)
                .ThenBy(p => p.RandomTieBreaker)
                .ToList();

            var r1Opponents = state.Round1Matches
                .Where(m => m.PlayerBId.HasValue)
                .Select(m => OpponentKey(m.PlayerAId, m.PlayerBId!.Value))
                .ToHashSet();

            var pairs = PairByRanking(ranked, r1Opponents, preferAxisVsAllies);
            EnsureEnoughTables(state, pairs.Count);

            var matches = new List<Match>();
            string warning = "";

            int rematches = pairs.Count(p => r1Opponents.Contains(OpponentKey(p.A.Id, p.B.Id)));
            if (rematches > 0)
                warning += $"{rematches} Round 2 match(es) repeat a Round 1 pairing (no other legal pairing was possible).\n";

            var tables = AssignRound2Tables(pairs, r1TableMap, avoidRound1Tables);

            if (avoidRound1Tables)
            {
                var repeats = new List<string>();
                for (int i = 0; i < pairs.Count; i++)
                {
                    foreach (var p in new[] { pairs[i].A, pairs[i].B })
                        if (r1TableMap.TryGetValue(p.Id, out var t) && t == tables[i])
                            repeats.Add($"{p.FullName} (Table {t})");
                }

                if (repeats.Count > 0)
                    warning += "Same table as Round 1 could not be avoided for: " + string.Join(", ", repeats) + "\n";
            }

            for (int i = 0; i < pairs.Count; i++)
            {
                matches.Add(new Match
                {
                    RoundIndex = 2,
                    TableNumber = tables[i],
                    PlayerAId = pairs[i].A.Id,
                    PlayerBId = pairs[i].B.Id
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

            return (matches, byeId, warning.Trim());
        }

        // Penalties, from the most important rule to the least important
        private const long SpecialVsSpecialPenalty = 1_000_000_000_000;
        private const long RematchPenalty = 1_000_000_000;
        private const long FirstVsSecondPenalty = 1_000_000;
        private const long SameSidePenalty = 3; // ~ allows a different opponent within the same group of 4
        private const long SearchNodeLimit = 3_000_000; // safety cap for very large events: best pairing found so far is used

        /// <summary>
        /// Round 2 pairing by ranking (ranked[0] = 1st).
        /// Ideal pattern: 1-3, 2-4, 5-7, 6-8, ... (if the count is 4k+2, the last two play each other).
        /// Rules, most important first:
        ///   1. Special vs Special only if unavoidable
        ///   2. No rematch from Round 1 unless unavoidable
        ///   3. 1st never plays 2nd unless unavoidable
        ///   4. Opponents as close as possible to the ideal pattern
        ///   5. (optional) prefer Axis vs Allies
        /// Finds the pairing with the lowest total penalty: a quick swap-based solution first,
        /// then an exact search that only explores pairings that can beat it.
        /// Returns matches in ranking order, higher-ranked player as A.
        /// </summary>
        internal static List<(Player A, Player B)> PairByRanking(
            List<Player> ranked,
            HashSet<(Guid, Guid)> r1Opponents,
            bool preferAxisVsAllies)
        {
            int n = ranked.Count;
            var cost = new long[n, n];
            var minCost = new long[n]; // cheapest possible opponent per player (search bound)

            for (int i = 0; i < n; i++)
            {
                minCost[i] = long.MaxValue;
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    var (a, b) = (ranked[i], ranked[j]);
                    long c = Math.Abs(j - IdealOpponent(i, n)) + Math.Abs(i - IdealOpponent(j, n));
                    if (a.IsSpecial && b.IsSpecial) c += SpecialVsSpecialPenalty;
                    if (r1Opponents.Contains(OpponentKey(a.Id, b.Id))) c += RematchPenalty;
                    if (Math.Min(i, j) == 0 && Math.Max(i, j) == 1) c += FirstVsSecondPenalty;
                    if (preferAxisVsAllies && a.Side == b.Side) c += SameSidePenalty;
                    cost[i, j] = c;
                    minCost[i] = Math.Min(minCost[i], c);
                }
            }

            // 1) Quick solution: ideal pattern, then swap opponents between two matches while it helps
            var partner = new int[n];
            for (int i = 0; i < n; i++) partner[i] = IdealOpponent(i, n);

            void Link(int x, int y) { partner[x] = y; partner[y] = x; }

            bool improved = true;
            while (improved)
            {
                improved = false;
                for (int a = 0; a < n; a++)
                {
                    for (int c = a + 1; c < n; c++)
                    {
                        int b = partner[a], d = partner[c];
                        if (b == c) continue; // same match

                        long now = cost[a, b] + cost[c, d];
                        if (cost[a, c] + cost[b, d] < now) { Link(a, c); Link(b, d); improved = true; }
                        else if (cost[a, d] + cost[b, c] < now) { Link(a, d); Link(b, c); improved = true; }
                    }
                }
            }

            // 2) Exact search: pair the highest-ranked free player first, cheapest opponents first,
            //    skipping branches that cannot beat the best total found so far
            long best = 0;
            for (int i = 0; i < n; i++)
                if (partner[i] > i) best += cost[i, partner[i]];

            var bestPartner = (int[])partner.Clone();
            var current = new int[n];
            var used = new bool[n];
            long nodes = 0;

            void Search(long total, long remainingMin)
            {
                if (++nodes > SearchNodeLimit) return;

                int i = Array.IndexOf(used, false);
                if (i < 0)
                {
                    if (total < best) { best = total; Array.Copy(current, bestPartner, n); }
                    return;
                }

                used[i] = true;
                var opponents = Enumerable.Range(i + 1, n - i - 1).Where(j => !used[j]).OrderBy(j => cost[i, j]).ToList();
                foreach (int j in opponents)
                {
                    long newTotal = total + cost[i, j];
                    long newRemainingMin = remainingMin - minCost[i] - minCost[j];
                    // every remaining match costs at least the average of its two players' cheapest options
                    if (newTotal + newRemainingMin / 2 >= best) continue;

                    used[j] = true;
                    current[i] = j;
                    current[j] = i;
                    Search(newTotal, newRemainingMin);
                    used[j] = false;
                }
                used[i] = false;
            }

            Search(0, minCost.Sum());

            var result = new List<(Player A, Player B)>();
            for (int i = 0; i < n; i++)
                if (bestPartner[i] > i) result.Add((ranked[i], ranked[bestPartner[i]]));
            return result;
        }

        // 0-based ranks: 0-2, 1-3, 4-6, 5-7, ...; with 4k+2 players the last two play each other
        private static int IdealOpponent(int i, int n)
        {
            int groupStart = i - i % 4;
            if (groupStart + 4 > n) return i % 2 == 0 ? i + 1 : i - 1;
            return i % 4 < 2 ? i + 2 : i - 2;
        }

        private static (Guid, Guid) OpponentKey(Guid a, Guid b) => a.CompareTo(b) < 0 ? (a, b) : (b, a);

        /// <summary>
        /// Special players must not play each other unless there are not enough
        /// non-Special players to pair every Special with one.
        /// Each Special vs Special pair is fixed by swapping with a non-Special vs non-Special pair
        /// (the only swap that removes an S-S pair). When no N-N pair is left, the remaining
        /// S-S pairs are unavoidable.
        /// Swap choice: keep Axis vs Allies where possible, then the nearest pair in the list, ties random.
        /// </summary>
        internal static void AvoidSpecialVsSpecial(List<(Player A, Player B)> pairs)
        {
            while (true)
            {
                int ss = pairs.FindIndex(p => p.A.IsSpecial && p.B.IsSpecial);
                if (ss < 0) return;

                var (s1, s2) = pairs[ss];
                var candidates = new List<(int j, (Player A, Player B) p1, (Player A, Player B) p2)>();
                for (int j = 0; j < pairs.Count; j++)
                {
                    var (x, y) = pairs[j];
                    if (x.IsSpecial || y.IsSpecial) continue;
                    candidates.Add((j, (s1, x), (s2, y)));
                    candidates.Add((j, (s1, y), (s2, x)));
                }

                if (candidates.Count == 0) return; // no N-N pair left: remaining S-S pairs are unavoidable

                var best = candidates
                    .OrderByDescending(c => CrossSide(c.p1) + CrossSide(c.p2))
                    .ThenBy(c => Math.Abs(c.j - ss))
                    .ThenBy(_ => Rng.Next())
                    .First();

                pairs[ss] = best.p1;
                pairs[best.j] = best.p2;
            }
        }

        private static int CrossSide((Player A, Player B) p) => p.A.Side != p.B.Side ? 1 : 0;

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

        private static void EnsureEnoughTables(TournamentState state, int matchCount)
        {
            if (matchCount > state.TableCount)
                throw new InvalidOperationException(
                    $"Not enough tables: {matchCount} matches but only {state.TableCount} tables declared. Increase Tables in the tournament setup.");
        }

        /// <summary>
        /// Tables 1..(number of matches), random among the best options.
        /// avoidRound1Tables: tries every assignment and keeps the one where the fewest players
        /// sit at the same table as in Round 1 (zero whenever possible).
        /// Returns the table for each match (same order as pairs).
        /// </summary>
        private static int[] AssignRound2Tables(List<(Player A, Player B)> pairs, Dictionary<Guid, int> r1TableMap, bool avoidRound1Tables)
        {
            int k = pairs.Count;
            var tables = Enumerable.Range(1, k).ToList();
            Shuffle(tables);

            var best = tables.ToArray();
            if (!avoidRound1Tables) return best;

            int Repeats(int match, int table) =>
                (r1TableMap.TryGetValue(pairs[match].A.Id, out var ta) && ta == table ? 1 : 0) +
                (r1TableMap.TryGetValue(pairs[match].B.Id, out var tb) && tb == table ? 1 : 0);

            int bestRepeats = Enumerable.Range(0, k).Sum(i => Repeats(i, best[i]));
            var current = new int[k];
            var used = new bool[k];
            long nodes = 0;

            void Search(int match, int repeats)
            {
                if (repeats >= bestRepeats || ++nodes > SearchNodeLimit) return;
                if (match == k)
                {
                    bestRepeats = repeats;
                    Array.Copy(current, best, k);
                    return;
                }

                for (int x = 0; x < k; x++)
                {
                    if (used[x]) continue;
                    used[x] = true;
                    current[match] = tables[x];
                    Search(match + 1, repeats + Repeats(match, tables[x]));
                    used[x] = false;
                }
            }

            Search(0, 0);
            return best;
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