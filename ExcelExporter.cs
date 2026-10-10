using System;
using System.Linq;
using System.Collections.Generic;
using ClosedXML.Excel;

namespace TournamentPairings
{
    public static class ExcelExporter
    {
        public static void ExportTournament(TournamentState state, string filePath)
        {
            using var wb = new XLWorkbook();

            AddPlayersAndStandingsSheet(wb, state);
            AddRoundSheet(wb, state, roundIndex: 1);
            AddRoundSheet(wb, state, roundIndex: 2);

            wb.SaveAs(filePath);
        }

        private static void AddPlayersAndStandingsSheet(XLWorkbook wb, TournamentState state)
        {
            var ws = wb.Worksheets.Add("Standings");

            int r = 1;

            ws.Cell(r, 1).Value = "Tournament";
            ws.Cell(r, 2).Value = state.TournamentName;
            r++;

            ws.Cell(r, 1).Value = "Tables";
            ws.Cell(r, 2).Value = state.TableCount;
            r += 2;

            ws.Cell(r, 1).Value = "#";
            ws.Cell(r, 2).Value = "Full Name";
            ws.Cell(r, 3).Value = "Side";
            ws.Cell(r, 4).Value = "Special";
            ws.Cell(r, 5).Value = "Withdrawn (R2)";
            ws.Cell(r, 6).Value = "R1 Score";
            ws.Cell(r, 7).Value = "R1 Units";
            ws.Cell(r, 8).Value = "R2 Score";
            ws.Cell(r, 9).Value = "R2 Units";
            ws.Cell(r, 10).Value = "Total Score";
            ws.Cell(r, 11).Value = "Total Units";
            ws.Cell(r, 12).Value = "Notes";
            ws.Range(r, 1, r, 12).Style.Font.Bold = true;
            r++;

            int rank = 1;
            foreach (var p in state.GetStandings())
            {
                string notes = BuildPlayerNotes(state, p);
                string displayName = string.IsNullOrWhiteSpace(notes)
                    ? p.FullName
                    : $"{p.FullName} ({notes})";

                ws.Cell(r, 1).Value = rank++;
                ws.Cell(r, 2).Value = displayName;
                ws.Cell(r, 3).Value = p.Side.ToString();
                ws.Cell(r, 4).Value = p.IsSpecial ? "Yes" : "";
                ws.Cell(r, 5).Value = p.IsWithdrawnForRound2 ? "Yes" : "";
                ws.Cell(r, 6).Value = p.Round1Score;
                ws.Cell(r, 7).Value = p.Round1Units;
                ws.Cell(r, 8).Value = p.Round2Score;
                ws.Cell(r, 9).Value = p.Round2Units;
                ws.Cell(r, 10).Value = p.TotalScoreSoFar;
                ws.Cell(r, 11).Value = p.TotalUnits;
                ws.Cell(r, 12).Value = notes;
                r++;
            }

            ws.Columns().AdjustToContents();
        }

        private static void AddRoundSheet(XLWorkbook wb, TournamentState state, int roundIndex)
        {
            var ws = wb.Worksheets.Add($"Round {roundIndex}");
            var matches = (roundIndex == 1) ? state.Round1Matches : state.Round2Matches;

            int r = 1;

            ws.Cell(r, 1).Value = $"Round {roundIndex} Matches";
            r += 2;

            ws.Cell(r, 1).Value = "Table";
            ws.Cell(r, 2).Value = "Player A";
            ws.Cell(r, 3).Value = "Side A";
            ws.Cell(r, 4).Value = "Score A";
            ws.Cell(r, 5).Value = "Units A";
            ws.Cell(r, 6).Value = "Player B / BYE";
            ws.Cell(r, 7).Value = "Side B";
            ws.Cell(r, 8).Value = "Score B";
            ws.Cell(r, 9).Value = "Units B";
            ws.Cell(r, 10).Value = "Outcome";
            ws.Range(r, 1, r, 10).Style.Font.Bold = true;
            r++;

            foreach (var m in matches.OrderBy(x => x.TableNumber ?? int.MaxValue))
            {
                var pa = state.Players.First(x => x.Id == m.PlayerAId);
                var pb = m.PlayerBId.HasValue ? state.Players.First(x => x.Id == m.PlayerBId.Value) : null;

                ws.Cell(r, 1).Value = m.TableNumber?.ToString() ?? "BYE";
                ws.Cell(r, 2).Value = BuildRoundPlayerDisplayName(state, pa);
                ws.Cell(r, 3).Value = pa.Side.ToString();
                ws.Cell(r, 4).Value = m.ScoreA;
                ws.Cell(r, 5).Value = m.UnitsA;

                ws.Cell(r, 6).Value = pb != null ? BuildRoundPlayerDisplayName(state, pb) : "BYE";
                ws.Cell(r, 7).Value = pb?.Side.ToString() ?? "";
                ws.Cell(r, 8).Value = m.ScoreB;
                ws.Cell(r, 9).Value = m.UnitsB;

                string outcome = "";
                if (m.IsBye) outcome = "BYE";
                else if (m.IsDraw) outcome = "Draw";
                else if (m.WinnerA) outcome = "Winner A";
                else if (m.WinnerB) outcome = "Winner B";

                ws.Cell(r, 10).Value = outcome;
                r++;
            }

            ws.Columns().AdjustToContents();
        }

        private static string BuildRoundPlayerDisplayName(TournamentState state, Player player)
        {
            string notes = BuildPlayerNotes(state, player);
            return string.IsNullOrWhiteSpace(notes)
                ? player.FullName
                : $"{player.FullName} ({notes})";
        }

        private static string BuildPlayerNotes(TournamentState state, Player player)
        {
            var notes = new List<string>();

            Guid? byeR1 = state.Round1Matches.FirstOrDefault(m => m.IsBye)?.PlayerAId;
            Guid? byeR2 = state.Round2Matches.FirstOrDefault(m => m.IsBye)?.PlayerAId;

            if (player.IsLateEntry)
                notes.Add("Late Entry");

            if (player.IsWithdrawnForRound2)
                notes.Add("Withdrawn (R2)");

            if (byeR1 == player.Id)
                notes.Add("BYE (R1)");

            if (byeR2 == player.Id)
                notes.Add("BYE (R2)");

            return string.Join(", ", notes);
        }
    }
}