using System.Drawing;
using System.Windows.Forms;

namespace TournamentPairings
{
    /// <summary>
    /// "Help" window with the user guide (opened from the Help buttons).
    /// </summary>
    public static class HelpWindow
    {
        private const string GuideText =
@"TOURNAMENT PAIRINGS - USER GUIDE

1. CREATE A TOURNAMENT
- Start > Create New Tournament.
- Enter the tournament name and the number of tables.
  Tables are used from 1 up to the number of matches (e.g. 12 players = 6 matches = tables 1-6).
  There must be at least as many tables as matches.
- Add the players: Full Name, Side (Axis / Allies) and Special.
- Create (or Create & Save As...) opens the Tournament Hub.

2. ROUND 1 DRAW
- Round 1 Draw > Create Round 1 Draw.
- Pairings are random, Axis vs Allies where possible.
- Tables are assigned at random.
- Late Entry: players who join from Round 2. They are not drawn in Round 1.
- Volunteer BYE: a player who volunteers to sit out when the number of players is odd.

3. ENTER RESULTS
- For every match enter Score and Units (units destroyed) for both players,
  and tick exactly one of Win A / Win B / Draw.
- Save (Draft) keeps the results editable.
- Lock Round X Results finalizes the round. Locked results cannot be changed.
- A player with a BYE gets points but no units.

4. ROUND 2 DRAW (Swiss style)
- Activate Late Entry players and withdraw players if needed.
- Players are ranked by Round 1 Score, then Units, then a random draw.
- Ideal pairing by ranking: 1st-3rd, 2nd-4th, 5th-7th, 6th-8th, ...
  (with 10, 14 ... players the last two play each other).
- The pairing changes only when needed for these rules (most important first):
    1. Special players do not play each other, unless unavoidable.
    2. Nobody plays the same opponent as in Round 1, unless unavoidable.
    3. The 1st never plays the 2nd, unless unavoidable.
- Prefer Axis vs Allies (optional): pairs Axis with Allies when it changes
  the ranking order only slightly.
- Avoid Round 1 tables (on by default): nobody plays on the same table as in Round 1,
  when possible. A message lists any player for whom it could not be avoided.

5. BYE RULES
- A BYE is needed when the number of players is odd.
- Special players never get a BYE.
- The player who had the BYE in Round 1 does not get it again in Round 2, when possible.

6. STANDINGS AND FINAL RESULTS
- Total Score (Round 1 + Round 2), then Total Units, then a random draw.
- Withdrawn players are listed at the bottom.
- Final > Export Excel saves the standings and both rounds to an .xlsx file.

7. SAVING
- Save / Save As writes a .tournament file you can open later (Open Tournament File or Recent).
- Autosave snapshots are kept automatically in Documents\TournamentPairings\Autosaves
  (the 30 most recent).

8. SCREEN SIZE AND ZOOM
- Resize the window by dragging its corner: text, tables and buttons grow and shrink with it.
- Ctrl + Plus / Ctrl + Minus: zoom in / out.  Ctrl + 0: reset zoom.  Ctrl + mouse wheel also zooms.
- The zoom level is remembered.";

        public static void Show(IWin32Window owner)
        {
            using var form = new Form
            {
                Text = "Help - User Guide",
                Width = 760,
                Height = 640,
                StartPosition = FormStartPosition.CenterParent,
                ShowInTaskbar = false
            };

            form.Controls.Add(new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Dock = DockStyle.Fill,
                BackColor = SystemColors.Window,
                Font = new Font("Segoe UI", 10),
                Text = GuideText.Replace("\r\n", "\n").Replace("\n", "\r\n"),
                SelectionStart = 0
            });

            form.ShowDialog(owner);
        }
    }
}
