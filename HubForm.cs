using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;

namespace TournamentPairings
{
    public partial class HubForm : Form
    {
        private readonly TournamentState _state;

        private Label _lblTitle = null!;
        private Label _lblPageTitle = null!;
        private Label _lblRoundStatus = null!;

        private Panel _panelStandings = null!;
        private Panel _panelR1Draw = null!;
        private Panel _panelR1Results = null!;
        private Panel _panelR2Draw = null!;
        private Panel _panelR2Results = null!;
        private Panel _panelFinal = null!;

        private DataGridView _standings = null!;
        private Button _btnSave = null!;
        private Button _btnSaveAs = null!;
        private Button _btnAutosave = null!;
        private Button _btnBackToSetup = null!;
        private Button _btnGoR1Draw = null!;
        private Button _btnGoR1Results = null!;
        private Button _btnGoR2Draw = null!;
        private Button _btnGoR2Results = null!;
        private Button _btnGoFinal = null!;
        private Button _btnClose = null!;

        private Label _lblR1Info = null!;
        private CheckBox _chkR1LateEntry = null!;
        private CheckedListBox _clbR1LateEntry = null!;
        private Button _btnApplyLateEntryR1 = null!;

        private CheckBox _chkR1VolBye = null!;
        private CheckedListBox _clbR1VolBye = null!;
        private Button _btnR1DoDraw = null!;
        private Button _btnR1Back = null!;
        private DataGridView _gridR1 = null!;

        private Label _lblR1ResInfo = null!;
        private Button _btnR1SaveDraft = null!;
        private Button _btnR1Lock = null!;
        private Button _btnR1ResBack = null!;
        private DataGridView _gridR1Res = null!;

        private Label _lblR2Info = null!;

        private CheckBox _chkActivateLateEntryR2 = null!;
        private CheckedListBox _clbActivateLateEntryR2 = null!;
        private Button _btnActivateLateEntryR2 = null!;
        private Label _lblLateEntryList = null!;

        private CheckBox _chkWithdrawR2 = null!;
        private CheckedListBox _clbWithdrawR2 = null!;
        private Button _btnApplyWithdrawR2 = null!;
        private Label _lblWithdrawnList = null!;

        private CheckBox _chkR2VolBye = null!;
        private CheckedListBox _clbR2VolBye = null!;
        private Button _btnR2DoDraw = null!;
        private Button _btnR2Back = null!;
        private DataGridView _gridR2 = null!;

        private Label _lblR2ResInfo = null!;
        private Button _btnR2SaveDraft = null!;
        private Button _btnR2Lock = null!;
        private Button _btnR2ResBack = null!;
        private DataGridView _gridR2Res = null!;

        private Label _lblFinal = null!;
        private Label _lblW1 = null!;
        private Label _lblW2 = null!;
        private Label _lblW3 = null!;
        private Button _btnFinalBack = null!;
        private Button _btnFinalExport = null!;

        private sealed class OutcomeSuppress { public bool Value; }

        private const int HeaderTop = 10;
        private const int PageTitleTop = 40;
        private const int PanelTop = 70;

        public HubForm(TournamentState state)
        {
            InitializeComponent();
            _state = state;

            Text = "Tournament Hub";
            Width = 1200;
            Height = 760;
            MinimumSize = new System.Drawing.Size(1200, 760);

            BuildUi();

            EnableGridRowNumbers();
            PositionRoundStatus();
            Resize += (_, __) => PositionRoundStatus();

            ShowStandings();
            RefreshStandings();
            RefreshButtons();
        }

        private void BuildUi()
        {
            _lblTitle = new Label
            {
                Left = 15,
                Top = HeaderTop,
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 12, System.Drawing.FontStyle.Bold)
            };
            Controls.Add(_lblTitle);

            _lblPageTitle = new Label
            {
                Left = 15,
                Top = PageTitleTop,
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 11, System.Drawing.FontStyle.Bold)
            };
            Controls.Add(_lblPageTitle);

            _lblRoundStatus = new Label
            {
                AutoSize = true,
                Font = new System.Drawing.Font(Font.FontFamily, 10, System.Drawing.FontStyle.Italic),
                ForeColor = SystemColors.GrayText,
                Top = PageTitleTop + 2,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = false
            };
            Controls.Add(_lblRoundStatus);

            _panelStandings = MakePanel();
            _panelR1Draw = MakePanel();
            _panelR1Results = MakePanel();
            _panelR2Draw = MakePanel();
            _panelR2Results = MakePanel();
            _panelFinal = MakePanel();

            Controls.AddRange(new Control[]
            {
                _panelStandings, _panelR1Draw, _panelR1Results, _panelR2Draw, _panelR2Results, _panelFinal
            });

            BuildStandingsPage();
            BuildR1DrawPage();
            BuildR1ResultsPage();
            BuildR2DrawPage();
            BuildR2ResultsPage();
            BuildFinalPage();
        }

        private Panel MakePanel()
        {
            return new Panel
            {
                Left = 0,
                Top = PanelTop,
                Width = ClientSize.Width,
                Height = ClientSize.Height - PanelTop,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Visible = false
            };
        }

        private void HideAll()
        {
            _panelStandings.Visible = false;
            _panelR1Draw.Visible = false;
            _panelR1Results.Visible = false;
            _panelR2Draw.Visible = false;
            _panelR2Results.Visible = false;
            _panelFinal.Visible = false;
        }

        private void SetPageTitle(string title) => _lblPageTitle.Text = title;

        private void PositionRoundStatus()
        {
            if (_lblRoundStatus == null) return;
            const int margin = 15;
            _lblRoundStatus.Left = Math.Max(margin, ClientSize.Width - _lblRoundStatus.Width - margin);
        }

        private void ShowRoundStatus(int roundIndex)
        {
            bool locked = roundIndex == 1 ? _state.Round1Completed : _state.Round2Completed;
            _lblRoundStatus.Text = $"Round status: {(locked ? "Locked" : "Draft")}";
            _lblRoundStatus.Visible = true;
            PositionRoundStatus();
        }

        private void HideRoundStatus()
        {
            if (_lblRoundStatus == null) return;
            _lblRoundStatus.Visible = false;
        }

        private static int CountTablesUsed(List<Match> matches) => matches.Count(m => !m.IsBye);

        private static string TableLabel(Match m)
            => m.TableNumber.HasValue ? $"Table {m.TableNumber.Value}" : "BYE";

        // =======================
        // Standings
        // =======================
        private void BuildStandingsPage()
        {
            _standings = new DataGridView
            {
                Left = 15,
                Top = 10,
                Width = _panelStandings.Width - 30,
                Height = _panelStandings.Height - 120,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = true
            };

            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Rank", HeaderText = "Rank", Width = 60 });
            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Player", Width = 260 });
            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Side", HeaderText = "Side", Width = 80 });
            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Score", HeaderText = "Score", Width = 80 });
            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Units", HeaderText = "Units", Width = 80 });
            _standings.Columns.Add(new DataGridViewTextBoxColumn { Name = "Notes", HeaderText = "Notes", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });

            _panelStandings.Controls.Add(_standings);

            _btnSave = new Button { Text = "Save", Left = 15, Top = _panelStandings.Height - 90, Width = 100, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnSaveAs = new Button { Text = "Save As", Left = 125, Top = _panelStandings.Height - 90, Width = 100, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnAutosave = new Button { Text = "Autosave Snapshot", Left = 235, Top = _panelStandings.Height - 90, Width = 150, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnBackToSetup = new Button { Text = "Back to Setup", Left = 395, Top = _panelStandings.Height - 90, Width = 130, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            _btnGoR1Draw = new Button { Text = "Round 1 Draw", Left = 535, Top = _panelStandings.Height - 90, Width = 120, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnGoR1Results = new Button { Text = "Round 1 Results", Left = 665, Top = _panelStandings.Height - 90, Width = 120, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnGoR2Draw = new Button { Text = "Round 2 Draw", Left = 795, Top = _panelStandings.Height - 90, Width = 120, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnGoR2Results = new Button { Text = "Round 2 Results", Left = 925, Top = _panelStandings.Height - 90, Width = 120, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            _btnGoFinal = new Button { Text = "Final", Left = 1055, Top = _panelStandings.Height - 90, Width = 80, Height = 35, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            _btnClose = new Button { Text = "Close", Left = 1055, Top = _panelStandings.Height - 45, Width = 80, Height = 32, Anchor = AnchorStyles.Bottom | AnchorStyles.Left };

            _panelStandings.Controls.AddRange(new Control[]
            {
                _btnSave, _btnSaveAs, _btnAutosave, _btnBackToSetup,
                _btnGoR1Draw, _btnGoR1Results, _btnGoR2Draw, _btnGoR2Results, _btnGoFinal, _btnClose
            });

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnSave, "Save the tournament (overwrite or create a new file).");
            tooltip.SetToolTip(_btnSaveAs, "Save the tournament (overwrite or create a new file).");
            tooltip.SetToolTip(_btnAutosave, "Automatic backup created during progress.");
            tooltip.SetToolTip(_btnBackToSetup, "Return to the previous or setup screen.");
            tooltip.SetToolTip(_btnGoR1Draw, "Generate pairings for the selected round.");
            tooltip.SetToolTip(_btnGoR1Results, "Open results entry for the selected round.");
            tooltip.SetToolTip(_btnGoR2Draw, "Generate pairings for the selected round.");
            tooltip.SetToolTip(_btnGoR2Results, "Open results entry for the selected round.");
            tooltip.SetToolTip(_btnGoFinal, "Open final standings screen.");
            tooltip.SetToolTip(_btnClose, "Close the current window or application.");

            _btnSave.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(_state.FilePath))
                {
                    _btnSaveAs.PerformClick();
                    return;
                }

                try { Storage.SaveTournament(_state, _state.FilePath); MessageBox.Show(this, "Saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                catch (Exception ex) { MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            _btnSaveAs.Click += (_, __) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Tournament Files (*.tournament)|*.tournament|All Files (*.*)|*.*",
                    FileName = string.IsNullOrWhiteSpace(_state.TournamentName) ? "Tournament.tournament" : $"{_state.TournamentName}.tournament"
                };

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try { Storage.SaveTournament(_state, sfd.FileName); MessageBox.Show(this, "Saved.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                    catch (Exception ex) { MessageBox.Show(this, $"Save failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
                }
            };

            _btnAutosave.Click += (_, __) =>
            {
                try { Storage.AutosaveSnapshot(_state); MessageBox.Show(this, "Autosave snapshot created.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                catch (Exception ex) { MessageBox.Show(this, $"Autosave failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            };

            _btnBackToSetup.Click += (_, __) =>
            {
                using var setup = new SetupForm(_state);
                if (setup.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshStandings();
                    RefreshButtons();
                    MessageBox.Show(this, "Setup updated.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _btnGoR1Draw.Click += (_, __) => { PrepareR1Draw(); ShowR1Draw(); };
            _btnGoR1Results.Click += (_, __) => { PrepareR1Results(); ShowR1Results(); };
            _btnGoR2Draw.Click += (_, __) => { PrepareR2Draw(); ShowR2Draw(); };
            _btnGoR2Results.Click += (_, __) => { PrepareR2Results(); ShowR2Results(); };
            _btnGoFinal.Click += (_, __) => { PrepareFinal(); ShowFinal(); };
            _btnClose.Click += (_, __) => Close();
        }

        private void RefreshButtons()
        {
            _btnGoR1Draw.Enabled = _state.Players.Count >= 1;
            _btnGoR1Results.Enabled = _state.Round1Drawn;
            _btnGoR2Draw.Enabled = _state.Round1Completed;
            _btnGoR2Results.Enabled = _state.Round2Drawn;
            _btnGoFinal.Enabled = _state.Round2Completed;
        }

        private void RefreshStandings()
        {
            _lblTitle.Text = $"{_state.TournamentName} (Tables: {_state.TableCount})";

            _standings.Rows.Clear();

            var byeR1Ids = _state.Round1Matches.Where(m => m.IsBye).Select(m => m.PlayerAId).ToHashSet();
            var byeR2Ids = _state.Round2Matches.Where(m => m.IsBye).Select(m => m.PlayerAId).ToHashSet();

            int rank = 1;
            foreach (var p in _state.GetStandings())
            {
                string notes = "";

                if (p.IsLateEntry)
                    notes = "Late Entry";

                if (p.IsWithdrawnForRound2)
                    notes = string.IsNullOrWhiteSpace(notes) ? "Withdrawn (R2)" : $"{notes}, Withdrawn (R2)";

                if (byeR1Ids.Contains(p.Id))
                    notes = string.IsNullOrWhiteSpace(notes) ? "BYE (R1)" : $"{notes}, BYE (R1)";

                if (byeR2Ids.Contains(p.Id))
                    notes = string.IsNullOrWhiteSpace(notes) ? "BYE (R2)" : $"{notes}, BYE (R2)";

                _standings.Rows.Add(rank, p.FullName, p.Side.ToString(), p.TotalScoreSoFar, p.TotalUnits, notes);
                rank++;
            }
        }

        // =======================
        // R1 Draw
        // =======================
        private void BuildR1DrawPage()
        {
            _lblR1Info = new Label { Left = 15, Top = 5, AutoSize = true };
            _panelR1Draw.Controls.Add(_lblR1Info);

            _chkR1LateEntry = new CheckBox { Text = "Late Entry", Left = 15, Top = 30, Width = 120 };
            _clbR1LateEntry = new CheckedListBox
            {
                Left = 15,
                Top = 55,
                Width = 260,
                Height = 100,
                CheckOnClick = true,
                Enabled = false
            };
            _btnApplyLateEntryR1 = new Button { Text = "Save Late Entry", Left = 15, Top = 160, Width = 140, Height = 30, Enabled = false };

            _chkR1VolBye = new CheckBox { Text = "Volunteer BYE", Left = 300, Top = 30, Width = 140 };
            _clbR1VolBye = new CheckedListBox
            {
                Left = 300,
                Top = 55,
                Width = 260,
                Height = 100,
                CheckOnClick = true,
                Enabled = false
            };

            _btnR1DoDraw = new Button { Text = "Create Round 1 Draw", Left = 585, Top = 55, Width = 190, Height = 32 };
            _btnR1Back = new Button { Text = "Back", Left = 1015, Top = 26, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            _panelR1Draw.Controls.AddRange(new Control[]
            {
                _chkR1LateEntry, _clbR1LateEntry, _btnApplyLateEntryR1,
                _chkR1VolBye, _clbR1VolBye, _btnR1DoDraw, _btnR1Back
            });

            _gridR1 = new DataGridView
            {
                Left = 15,
                Top = 205,
                Width = 1140,
                Height = 440,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                RowHeadersVisible = true
            };
            _panelR1Draw.Controls.Add(_gridR1);

            _gridR1.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table", HeaderText = "Table", Width = 120 });
            _gridR1.Columns.Add(new DataGridViewTextBoxColumn { Name = "A", HeaderText = "Player A", Width = 320 });
            _gridR1.Columns.Add(new DataGridViewTextBoxColumn { Name = "B", HeaderText = "Player B / BYE", Width = 320 });
            _gridR1.Columns.Add(new DataGridViewTextBoxColumn { Name = "SideA", HeaderText = "Side A", Width = 140 });
            _gridR1.Columns.Add(new DataGridViewTextBoxColumn { Name = "SideB", HeaderText = "Side B", Width = 140 });

            _chkR1LateEntry.CheckedChanged += (_, __) =>
            {
                _clbR1LateEntry.Enabled = _chkR1LateEntry.Checked;
                _btnApplyLateEntryR1.Enabled = _chkR1LateEntry.Checked;
                if (_chkR1LateEntry.Checked) FillR1LateEntryList();
            };

            _btnApplyLateEntryR1.Click += (_, __) =>
            {
                SaveR1LateEntrySelection();
            };

            _chkR1VolBye.CheckedChanged += (_, __) =>
            {
                _clbR1VolBye.Enabled = _chkR1VolBye.Checked;
                if (_chkR1VolBye.Checked) FillR1VolByeList();
            };

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnApplyLateEntryR1, "Save late entry selections.");
            tooltip.SetToolTip(_btnR1DoDraw, "Generate pairings for the selected round.");
            tooltip.SetToolTip(_btnR1Back, "Return to the previous or setup screen.");

            _btnR1Back.Click += (_, __) => { ShowStandings(); RefreshStandings(); RefreshButtons(); };

            _btnR1DoDraw.Click += (_, __) =>
            {
                try
                {
                    var selectedVolByeIds = _chkR1VolBye.Checked
                        ? GetCheckedPlayers(_clbR1VolBye).Select(p => p.Id).ToList()
                        : new List<Guid>();

                    SaveR1LateEntrySelection();

                    var matches = GenerateRound1WithSelections(selectedVolByeIds);

                    _state.Round1Matches = matches;
                    _state.Round1Drawn = true;
                    _state.IsDirty = true;

                    Storage.AutosaveSnapshot(_state);

                    PrepareR1Results();
                    ShowR1Results();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private void FillR1LateEntryList()
        {
            _clbR1LateEntry.Items.Clear();

            foreach (var p in _state.Players.OrderBy(x => x.FullName))
            {
                int idx = _clbR1LateEntry.Items.Add(p);
                _clbR1LateEntry.SetItemChecked(idx, p.IsLateEntry);
            }

            _clbR1LateEntry.DisplayMember = nameof(Player.FullName);
        }

        private void SaveR1LateEntrySelection()
        {
            var checkedIds = GetCheckedPlayers(_clbR1LateEntry).Select(x => x.Id).ToHashSet();

            foreach (var p in _state.Players)
                p.IsLateEntry = checkedIds.Contains(p.Id);

            _state.IsDirty = true;
            FillR1VolByeList();
            PrepareR1Draw();
        }

        private void FillR1VolByeList()
        {
            _clbR1VolBye.Items.Clear();

            foreach (var p in _state.Players
                .Where(x => !x.IsLateEntry && !x.IsSpecial)
                .OrderBy(x => x.FullName))
            {
                _clbR1VolBye.Items.Add(p, false);
            }

            _clbR1VolBye.DisplayMember = nameof(Player.FullName);
        }

        private void PrepareR1Draw()
        {
            int activePlayers = _state.Players.Count(x => !x.IsLateEntry);
            int lateEntries = _state.Players.Count(x => x.IsLateEntry);
            int tablesUsed = CountTablesUsed(_state.Round1Matches);
            int byeCount = _state.Round1Matches.Count(x => x.IsBye);

            _lblR1Info.Text = $"Players: {_state.Players.Count} | Active: {activePlayers} | Late Entry: {lateEntries} | Tables used: {tablesUsed} | BYE count: {byeCount}";

            if (_chkR1LateEntry.Checked)
                FillR1LateEntryList();

            if (_chkR1VolBye.Checked)
                FillR1VolByeList();

            _gridR1.Rows.Clear();

            foreach (var m in _state.Round1Matches.OrderBy(x => x.TableNumber ?? int.MaxValue))
            {
                var pa = _state.Players.First(x => x.Id == m.PlayerAId);
                string pbName = m.IsBye ? "BYE" : _state.Players.First(x => x.Id == m.PlayerBId!.Value).FullName;
                string sideB = m.IsBye ? "-" : _state.Players.First(x => x.Id == m.PlayerBId!.Value).Side.ToString();

                _gridR1.Rows.Add(TableLabel(m), pa.FullName, pbName, pa.Side.ToString(), sideB);
            }
        }

        private List<Match> GenerateRound1WithSelections(List<Guid> selectedVolByeIds)
        {
            var selectedByePlayers = _state.Players.Where(p => selectedVolByeIds.Contains(p.Id)).ToList();

            if (selectedByePlayers.Any(p => p.IsLateEntry))
                throw new InvalidOperationException("A Late Entry player cannot also be selected as Volunteer BYE.");

            if (selectedByePlayers.Any(p => p.IsSpecial))
                throw new InvalidOperationException("Special players cannot be BYE.");

            var activePlayers = _state.Players
                .Where(p => !p.IsLateEntry && !selectedVolByeIds.Contains(p.Id))
                .ToList();

            var matches = new List<Match>();

            if (activePlayers.Count >= 1)
            {
                var tempState = CreateTempState(activePlayers);
                var result = PairingEngine.GenerateRound1(tempState, null);
                matches.AddRange(result.matches);
            }

            foreach (var p in selectedByePlayers.OrderBy(x => x.FullName))
            {
                matches.Add(new Match
                {
                    RoundIndex = 1,
                    TableNumber = null,
                    PlayerAId = p.Id,
                    PlayerBId = null
                });
            }

            return matches;
        }

        // =======================
        // R1 Results
        // =======================
        private void BuildR1ResultsPage()
        {
            _lblR1ResInfo = new Label { Left = 15, Top = 5, AutoSize = true };
            _panelR1Results.Controls.Add(_lblR1ResInfo);

            _btnR1SaveDraft = new Button { Text = "Save (Draft)", Left = 15, Top = 30, Width = 140, Height = 32 };
            _btnR1Lock = new Button { Text = "Lock Round 1 Results", Left = 165, Top = 30, Width = 190, Height = 32 };
            _btnR1ResBack = new Button { Text = "Back", Left = 1015, Top = 30, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _panelR1Results.Controls.AddRange(new Control[] { _btnR1SaveDraft, _btnR1Lock, _btnR1ResBack });

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnR1SaveDraft, "Temporarily save results (editable later).");
            tooltip.SetToolTip(_btnR1Lock, "Finalize results (no further changes allowed).");
            tooltip.SetToolTip(_btnR1ResBack, "Return to the previous or setup screen.");

            _btnR1ResBack.Click += (_, __) => { ShowStandings(); RefreshStandings(); RefreshButtons(); };
            _btnR1SaveDraft.Click += (_, __) => SaveResultsDraft(_gridR1Res, roundIndex: 1);
            _btnR1Lock.Click += (_, __) => LockResults(_gridR1Res, roundIndex: 1);

            _gridR1Res = BuildResultsGrid(_panelR1Results);
        }

        private void PrepareR1Results()
        {
            if (!_state.Round1Drawn || _state.Round1Matches.Count == 0)
            {
                MessageBox.Show(this, "Draw Round 1 first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStandings();
                return;
            }

            int tablesUsed = CountTablesUsed(_state.Round1Matches);
            int byeCount = _state.Round1Matches.Count(m => m.IsBye);

            _lblR1ResInfo.Text = $"Tables used: {tablesUsed} | BYE count: {byeCount}";
            PopulateResultsGrid(_gridR1Res, _state.Round1Matches, roundLocked: _state.Round1Completed);
            SetResultsEditingEnabled(_gridR1Res, _btnR1SaveDraft, _btnR1Lock, enabled: !_state.Round1Completed);
            ShowRoundStatus(1);
        }

        // =======================
        // R2 Draw
        // =======================
        private void BuildR2DrawPage()
        {
            _lblR2Info = new Label { Left = 15, Top = 5, AutoSize = true };
            _panelR2Draw.Controls.Add(_lblR2Info);

            _chkActivateLateEntryR2 = new CheckBox { Text = "Activate Late Entry players", Left = 15, Top = 30, Width = 220 };
            _clbActivateLateEntryR2 = new CheckedListBox
            {
                Left = 15,
                Top = 55,
                Width = 260,
                Height = 85,
                CheckOnClick = true,
                Enabled = false
            };
            _btnActivateLateEntryR2 = new Button { Text = "Activate Selected", Left = 15, Top = 145, Width = 140, Height = 30, Enabled = false };
            _lblLateEntryList = new Label { Left = 15, Top = 178, Width = 560, Height = 18, AutoSize = false };

            _chkWithdrawR2 = new CheckBox { Text = "Withdraw players for Round 2", Left = 300, Top = 30, Width = 220 };
            _clbWithdrawR2 = new CheckedListBox
            {
                Left = 300,
                Top = 55,
                Width = 260,
                Height = 85,
                CheckOnClick = true,
                Enabled = false
            };
            _btnApplyWithdrawR2 = new Button { Text = "Save Withdraw Selection", Left = 300, Top = 145, Width = 170, Height = 30, Enabled = false };
            _lblWithdrawnList = new Label { Left = 300, Top = 178, Width = 560, Height = 18, AutoSize = false };

            _chkR2VolBye = new CheckBox { Text = "Volunteer BYE", Left = 585, Top = 30, Width = 140 };
            _clbR2VolBye = new CheckedListBox
            {
                Left = 585,
                Top = 55,
                Width = 260,
                Height = 85,
                CheckOnClick = true,
                Enabled = false
            };

            _btnR2DoDraw = new Button { Text = "Create Round 2 Draw", Left = 585, Top = 145, Width = 190, Height = 32 };
            _btnR2Back = new Button { Text = "Back", Left = 1015, Top = 26, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            _panelR2Draw.Controls.AddRange(new Control[]
            {
                _chkActivateLateEntryR2, _clbActivateLateEntryR2, _btnActivateLateEntryR2, _lblLateEntryList,
                _chkWithdrawR2, _clbWithdrawR2, _btnApplyWithdrawR2, _lblWithdrawnList,
                _chkR2VolBye, _clbR2VolBye, _btnR2DoDraw, _btnR2Back
            });

            _gridR2 = new DataGridView
            {
                Left = 15,
                Top = 215,
                Width = 1140,
                Height = 430,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                ReadOnly = true,
                RowHeadersVisible = true
            };
            _panelR2Draw.Controls.Add(_gridR2);

            _gridR2.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table", HeaderText = "Table", Width = 120 });
            _gridR2.Columns.Add(new DataGridViewTextBoxColumn { Name = "A", HeaderText = "Player A", Width = 320 });
            _gridR2.Columns.Add(new DataGridViewTextBoxColumn { Name = "B", HeaderText = "Player B / BYE", Width = 320 });
            _gridR2.Columns.Add(new DataGridViewTextBoxColumn { Name = "SideA", HeaderText = "Side A", Width = 140 });
            _gridR2.Columns.Add(new DataGridViewTextBoxColumn { Name = "SideB", HeaderText = "Side B", Width = 140 });

            _chkActivateLateEntryR2.CheckedChanged += (_, __) =>
            {
                _clbActivateLateEntryR2.Enabled = _chkActivateLateEntryR2.Checked;
                _btnActivateLateEntryR2.Enabled = _chkActivateLateEntryR2.Checked;
                if (_chkActivateLateEntryR2.Checked) FillActivateLateEntryR2List();
            };

            _btnActivateLateEntryR2.Click += (_, __) =>
            {
                var selectedIds = GetCheckedPlayers(_clbActivateLateEntryR2).Select(x => x.Id).ToHashSet();

                foreach (var p in _state.Players.Where(x => x.IsLateEntry))
                {
                    if (selectedIds.Contains(p.Id))
                        p.IsLateEntry = false;
                }

                _state.IsDirty = true;
                Storage.AutosaveSnapshot(_state);
                PrepareR2Draw();
            };

            _chkWithdrawR2.CheckedChanged += (_, __) =>
            {
                _clbWithdrawR2.Enabled = _chkWithdrawR2.Checked;
                _btnApplyWithdrawR2.Enabled = _chkWithdrawR2.Checked;
                if (_chkWithdrawR2.Checked) FillWithdrawR2List();
            };

            _btnApplyWithdrawR2.Click += (_, __) =>
            {
                SaveWithdrawSelectionR2();
            };

            _chkR2VolBye.CheckedChanged += (_, __) =>
            {
                _clbR2VolBye.Enabled = _chkR2VolBye.Checked;
                if (_chkR2VolBye.Checked) FillR2VolByeList();
            };

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnActivateLateEntryR2, "Manage late entry players and activate them for the next round.");
            tooltip.SetToolTip(_btnApplyWithdrawR2, "Mark players as withdrawn and save changes.");
            tooltip.SetToolTip(_btnR2DoDraw, "Generate pairings for the selected round.");
            tooltip.SetToolTip(_btnR2Back, "Return to the previous or setup screen.");

            _btnR2Back.Click += (_, __) => { ShowStandings(); RefreshStandings(); RefreshButtons(); };

            _btnR2DoDraw.Click += (_, __) =>
            {
                try
                {
                    if (!_state.Round1Completed)
                    {
                        MessageBox.Show(this, "Lock Round 1 results first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    var selectedVolByeIds = _chkR2VolBye.Checked
                        ? GetCheckedPlayers(_clbR2VolBye).Select(p => p.Id).ToList()
                        : new List<Guid>();

                    SaveWithdrawSelectionR2();

                    var result = GenerateRound2WithSelections(selectedVolByeIds);

                    _state.Round2Matches = result.matches;
                    _state.Round2Drawn = true;
                    _state.IsDirty = true;
                    Storage.AutosaveSnapshot(_state);

                    if (!string.IsNullOrWhiteSpace(result.warning))
                        MessageBox.Show(this, result.warning, "Pairing note", MessageBoxButtons.OK, MessageBoxIcon.Information);

                    PrepareR2Results();
                    ShowR2Results();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
        }

        private void FillActivateLateEntryR2List()
        {
            _clbActivateLateEntryR2.Items.Clear();

            foreach (var p in _state.Players.Where(x => x.IsLateEntry).OrderBy(x => x.FullName))
                _clbActivateLateEntryR2.Items.Add(p, false);

            _clbActivateLateEntryR2.DisplayMember = nameof(Player.FullName);
        }

        private void FillWithdrawR2List()
        {
            _clbWithdrawR2.Items.Clear();

            foreach (var p in _state.Players.Where(x => !x.IsLateEntry).OrderBy(x => x.FullName))
            {
                int idx = _clbWithdrawR2.Items.Add(p);
                _clbWithdrawR2.SetItemChecked(idx, p.IsWithdrawnForRound2);
            }

            _clbWithdrawR2.DisplayMember = nameof(Player.FullName);
        }

        private void SaveWithdrawSelectionR2()
        {
            var checkedIds = GetCheckedPlayers(_clbWithdrawR2).Select(x => x.Id).ToHashSet();

            foreach (var p in _state.Players.Where(x => !x.IsLateEntry))
                p.IsWithdrawnForRound2 = checkedIds.Contains(p.Id);

            _state.IsDirty = true;
            Storage.AutosaveSnapshot(_state);
            FillR2VolByeList();
            PrepareR2Draw();
        }

        private void FillR2VolByeList()
        {
            _clbR2VolBye.Items.Clear();

            foreach (var p in _state.Players
                .Where(x => !x.IsLateEntry && !x.IsWithdrawnForRound2 && !x.IsSpecial)
                .OrderBy(x => x.FullName))
            {
                _clbR2VolBye.Items.Add(p, false);
            }

            _clbR2VolBye.DisplayMember = nameof(Player.FullName);
        }

        private void PrepareR2Draw()
        {
            int active = _state.Players.Count(x => !x.IsWithdrawnForRound2 && !x.IsLateEntry);
            int withdrawnCount = _state.Players.Count(x => x.IsWithdrawnForRound2);
            int lateEntryPending = _state.Players.Count(x => x.IsLateEntry);
            int tablesUsed = CountTablesUsed(_state.Round2Matches);
            int byeCount = _state.Round2Matches.Count(x => x.IsBye);

            _lblR2Info.Text = $"Active players: {active} | Withdrawn: {withdrawnCount} | Pending Late Entry: {lateEntryPending} | Tables used: {tablesUsed} | BYE count: {byeCount}";

            var lateEntries = _state.Players.Where(x => x.IsLateEntry).Select(x => x.FullName).ToList();
            _lblLateEntryList.Text = lateEntries.Count == 0 ? "Late Entry pending: (none)" : "Late Entry pending: " + string.Join(", ", lateEntries);

            var withdrawn = _state.Players.Where(x => x.IsWithdrawnForRound2).Select(x => x.FullName).ToList();
            _lblWithdrawnList.Text = withdrawn.Count == 0 ? "Withdrawn: (none)" : "Withdrawn: " + string.Join(", ", withdrawn);

            if (_chkActivateLateEntryR2.Checked)
                FillActivateLateEntryR2List();

            if (_chkWithdrawR2.Checked)
                FillWithdrawR2List();

            if (_chkR2VolBye.Checked)
                FillR2VolByeList();

            _gridR2.Rows.Clear();

            foreach (var m in _state.Round2Matches.OrderBy(x => x.TableNumber ?? int.MaxValue))
            {
                var pa = _state.Players.First(x => x.Id == m.PlayerAId);
                string pbName = m.IsBye ? "BYE" : _state.Players.First(x => x.Id == m.PlayerBId!.Value).FullName;
                string sideB = m.IsBye ? "-" : _state.Players.First(x => x.Id == m.PlayerBId!.Value).Side.ToString();

                _gridR2.Rows.Add(TableLabel(m), pa.FullName, pbName, pa.Side.ToString(), sideB);
            }
        }

        private (List<Match> matches, string warning) GenerateRound2WithSelections(List<Guid> selectedVolByeIds)
        {
            var selectedByePlayers = _state.Players.Where(p => selectedVolByeIds.Contains(p.Id)).ToList();

            if (selectedByePlayers.Any(p => p.IsLateEntry))
                throw new InvalidOperationException("A Late Entry player cannot also be selected as Volunteer BYE.");

            if (selectedByePlayers.Any(p => p.IsWithdrawnForRound2))
                throw new InvalidOperationException("A withdrawn player cannot also be selected as Volunteer BYE.");

            if (selectedByePlayers.Any(p => p.IsSpecial))
                throw new InvalidOperationException("Special players cannot be BYE.");

            var activePlayers = _state.Players
                .Where(p => !p.IsLateEntry && !p.IsWithdrawnForRound2 && !selectedVolByeIds.Contains(p.Id))
                .ToList();

            var matches = new List<Match>();
            string warning = "";

            if (activePlayers.Count >= 2)
            {
                var tempState = CreateTempState(activePlayers);
                tempState.Round1Matches = _state.Round1Matches;
                tempState.Round1Completed = _state.Round1Completed;

                var result = PairingEngine.GenerateRound2(tempState, null);
                matches.AddRange(result.matches);
                warning = result.warning;
            }
            else if (activePlayers.Count == 1)
            {
                matches.Add(new Match
                {
                    RoundIndex = 2,
                    TableNumber = null,
                    PlayerAId = activePlayers[0].Id,
                    PlayerBId = null
                });
            }

            foreach (var p in selectedByePlayers.OrderBy(x => x.FullName))
            {
                matches.Add(new Match
                {
                    RoundIndex = 2,
                    TableNumber = null,
                    PlayerAId = p.Id,
                    PlayerBId = null
                });
            }

            return (matches, warning);
        }

        private TournamentState CreateTempState(List<Player> players)
        {
            return new TournamentState
            {
                TournamentName = _state.TournamentName,
                TableCount = _state.TableCount,
                Players = players.ToList(),
                Round1Matches = _state.Round1Matches.ToList(),
                Round2Matches = _state.Round2Matches.ToList(),
                FilePath = _state.FilePath,
                LastSavedUtc = _state.LastSavedUtc,
                Round1Drawn = _state.Round1Drawn,
                Round1Completed = _state.Round1Completed,
                Round2Drawn = _state.Round2Drawn,
                Round2Completed = _state.Round2Completed,
                IsDirty = _state.IsDirty
            };
        }

        private List<Player> GetCheckedPlayers(CheckedListBox list)
        {
            return list.CheckedItems.Cast<Player>().ToList();
        }

        // =======================
        // R2 Results
        // =======================
        private void BuildR2ResultsPage()
        {
            _lblR2ResInfo = new Label { Left = 15, Top = 5, AutoSize = true };
            _panelR2Results.Controls.Add(_lblR2ResInfo);

            _btnR2SaveDraft = new Button { Text = "Save (Draft)", Left = 15, Top = 30, Width = 140, Height = 32 };
            _btnR2Lock = new Button { Text = "Lock Round 2 Results", Left = 165, Top = 30, Width = 190, Height = 32 };
            _btnR2ResBack = new Button { Text = "Back", Left = 1015, Top = 30, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _panelR2Results.Controls.AddRange(new Control[] { _btnR2SaveDraft, _btnR2Lock, _btnR2ResBack });

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnR2SaveDraft, "Temporarily save results (editable later).");
            tooltip.SetToolTip(_btnR2Lock, "Finalize results (no further changes allowed).");
            tooltip.SetToolTip(_btnR2ResBack, "Return to the previous or setup screen.");

            _btnR2ResBack.Click += (_, __) => { ShowStandings(); RefreshStandings(); RefreshButtons(); };
            _btnR2SaveDraft.Click += (_, __) => SaveResultsDraft(_gridR2Res, roundIndex: 2);
            _btnR2Lock.Click += (_, __) => LockResults(_gridR2Res, roundIndex: 2);

            _gridR2Res = BuildResultsGrid(_panelR2Results);
        }

        private void PrepareR2Results()
        {
            if (!_state.Round2Drawn || _state.Round2Matches.Count == 0)
            {
                MessageBox.Show(this, "Draw Round 2 first.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                ShowStandings();
                return;
            }

            int tablesUsed = CountTablesUsed(_state.Round2Matches);
            int byeCount = _state.Round2Matches.Count(m => m.IsBye);

            _lblR2ResInfo.Text = $"Tables used: {tablesUsed} | BYE count: {byeCount}";
            PopulateResultsGrid(_gridR2Res, _state.Round2Matches, roundLocked: _state.Round2Completed);
            SetResultsEditingEnabled(_gridR2Res, _btnR2SaveDraft, _btnR2Lock, enabled: !_state.Round2Completed);
            ShowRoundStatus(2);
        }

        // =======================
        // Results Grid + Rules
        // =======================
        private DataGridView BuildResultsGrid(Panel host)
        {
            var grid = new DataGridView
            {
                Left = 15,
                Top = 70,
                Width = 1140,
                Height = 575,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                RowHeadersVisible = true
            };
            host.Controls.Add(grid);

            grid.Tag = new OutcomeSuppress();

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Table", HeaderText = "Table", ReadOnly = true, Width = 90 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlayerA", HeaderText = "Player A", ReadOnly = true, Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScoreA", HeaderText = "Score", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitsA", HeaderText = "Units", Width = 70 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "WinA", HeaderText = "Win A", Width = 60 });

            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PlayerB", HeaderText = "Player B / BYE", ReadOnly = true, Width = 200 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ScoreB", HeaderText = "Score", Width = 70 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitsB", HeaderText = "Units", Width = 70 });
            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "WinB", HeaderText = "Win B", Width = 60 });

            grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Draw", HeaderText = "Draw", Width = 60 });

            grid.CurrentCellDirtyStateChanged += (_, __) =>
            {
                if (grid.IsCurrentCellDirty)
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            grid.CellContentClick += (_, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
                var col = grid.Columns[e.ColumnIndex].Name;
                if (col == "WinA" || col == "WinB" || col == "Draw")
                {
                    grid.CommitEdit(DataGridViewDataErrorContexts.Commit);

                    if (grid.Tag is OutcomeSuppress sup && !sup.Value)
                        ApplyOutcomeRulesAndDisable(grid, e.RowIndex, col, sup);
                }
            };

            grid.CellValueChanged += (_, e) =>
            {
                if (e.RowIndex < 0) return;
                if (grid.Tag is not OutcomeSuppress sup) return;
                if (sup.Value) return;

                var colName = grid.Columns[e.ColumnIndex].Name;
                if (colName == "WinA" || colName == "WinB" || colName == "Draw")
                {
                    ApplyOutcomeRulesAndDisable(grid, e.RowIndex, colName, sup);
                }
            };

            grid.DataError += (_, e) =>
            {
                e.ThrowException = false;
                e.Cancel = true;
            };

            return grid;
        }

        private void DisableCell(DataGridView grid, int rowIndex, string colName, bool disabled)
        {
            var cell = grid.Rows[rowIndex].Cells[colName];
            cell.ReadOnly = disabled;
            cell.Style.BackColor = disabled ? System.Drawing.Color.LightGray : System.Drawing.Color.White;
        }

        private void ApplyOutcomeRulesAndDisable(DataGridView grid, int rowIndex, string changedColumn, OutcomeSuppress suppress)
        {
            var row = grid.Rows[rowIndex];
            if (row.Tag is not Match match) return;
            if (match.IsBye)
            {
                suppress.Value = true;
                row.Cells["WinA"].Value = true;
                row.Cells["WinB"].Value = false;
                row.Cells["Draw"].Value = false;
                suppress.Value = false;

                DisableCell(grid, rowIndex, "WinA", true);
                DisableCell(grid, rowIndex, "WinB", true);
                DisableCell(grid, rowIndex, "Draw", true);
                return;
            }

            bool winA = row.Cells["WinA"].Value is bool a && a;
            bool winB = row.Cells["WinB"].Value is bool b && b;
            bool draw = row.Cells["Draw"].Value is bool d && d;

            suppress.Value = true;

            if (changedColumn == "WinA" && winA)
            {
                row.Cells["WinB"].Value = false;
                row.Cells["Draw"].Value = false;
            }
            else if (changedColumn == "WinB" && winB)
            {
                row.Cells["WinA"].Value = false;
                row.Cells["Draw"].Value = false;
            }
            else if (changedColumn == "Draw" && draw)
            {
                row.Cells["WinA"].Value = false;
                row.Cells["WinB"].Value = false;
            }

            suppress.Value = false;

            winA = row.Cells["WinA"].Value is bool wa && wa;
            winB = row.Cells["WinB"].Value is bool wb && wb;
            draw = row.Cells["Draw"].Value is bool dr && dr;

            DisableCell(grid, rowIndex, "WinA", false);
            DisableCell(grid, rowIndex, "WinB", false);
            DisableCell(grid, rowIndex, "Draw", false);

            if (winA)
            {
                DisableCell(grid, rowIndex, "WinB", true);
                DisableCell(grid, rowIndex, "Draw", true);
            }
            else if (winB)
            {
                DisableCell(grid, rowIndex, "WinA", true);
                DisableCell(grid, rowIndex, "Draw", true);
            }
            else if (draw)
            {
                DisableCell(grid, rowIndex, "WinA", true);
                DisableCell(grid, rowIndex, "WinB", true);
            }
        }

        private void PopulateResultsGrid(DataGridView grid, List<Match> matches, bool roundLocked)
        {
            grid.Rows.Clear();

            foreach (var m in matches.OrderBy(x => x.TableNumber ?? int.MaxValue))
            {
                var pa = _state.Players.First(x => x.Id == m.PlayerAId);
                bool isBye = m.IsBye;
                string pbName = isBye ? "BYE" : _state.Players.First(x => x.Id == m.PlayerBId!.Value).FullName;

                int rowIndex = grid.Rows.Add(
                    TableLabel(m),
                    pa.FullName,
                    m.ScoreA?.ToString() ?? "",
                    isBye ? "" : (m.UnitsA?.ToString() ?? ""),
                    m.WinnerA,
                    pbName,
                    isBye ? "" : (m.ScoreB?.ToString() ?? ""),
                    isBye ? "" : (m.UnitsB?.ToString() ?? ""),
                    m.WinnerB,
                    m.IsDraw
                );

                grid.Rows[rowIndex].Tag = m;

                if (isBye)
                {
                    grid.Rows[rowIndex].Cells["WinA"].Value = true;
                    grid.Rows[rowIndex].Cells["WinB"].Value = false;
                    grid.Rows[rowIndex].Cells["Draw"].Value = false;

                    DisableCell(grid, rowIndex, "UnitsA", true);
                    DisableCell(grid, rowIndex, "ScoreB", true);
                    DisableCell(grid, rowIndex, "UnitsB", true);
                    DisableCell(grid, rowIndex, "WinA", true);
                    DisableCell(grid, rowIndex, "WinB", true);
                    DisableCell(grid, rowIndex, "Draw", true);
                }
                else
                {
                    ApplyOutcomeRulesAndDisable(grid, rowIndex, "", (OutcomeSuppress)grid.Tag!);
                }
            }

            grid.ReadOnly = roundLocked;
        }

        private void SetResultsEditingEnabled(DataGridView grid, Button btnDraft, Button btnLock, bool enabled)
        {
            btnDraft.Enabled = enabled;
            btnLock.Enabled = enabled;
            grid.ReadOnly = !enabled;
        }

        private static string FriendlyField(string colName)
        {
            return colName switch
            {
                "ScoreA" => "Player A Score",
                "UnitsA" => "Player A Units",
                "ScoreB" => "Player B Score",
                "UnitsB" => "Player B Units",
                _ => colName
            };
        }

        private static string ContextPrefix(Match m)
        {
            return m.IsBye ? "BYE:" : $"{TableLabel(m)}:";
        }

        private void SaveResultsDraft(DataGridView grid, int roundIndex)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is not Match match) continue;
                bool isBye = match.IsBye;

                if (!TryGetOptionalIntCell(row, "ScoreA", 1, 9, out int? scoreA, out string errSA))
                {
                    MessageBox.Show(this, $"{ContextPrefix(match)} {errSA}", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? unitsA = null;
                if (!isBye)
                {
                    if (!TryGetOptionalIntCell(row, "UnitsA", 0, 99, out unitsA, out string errUA))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errUA}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                int? scoreB = null;
                int? unitsB = null;

                bool winA = row.Cells["WinA"].Value is bool wa && wa;
                bool winB = row.Cells["WinB"].Value is bool wb && wb;
                bool draw = row.Cells["Draw"].Value is bool dr && dr;

                if (!isBye)
                {
                    if (!TryGetOptionalIntCell(row, "ScoreB", 1, 9, out scoreB, out string errSB))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errSB}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!TryGetOptionalIntCell(row, "UnitsB", 0, 99, out unitsB, out string errUB))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errUB}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                match.ScoreA = scoreA;
                match.UnitsA = isBye ? null : unitsA;
                match.ScoreB = isBye ? null : scoreB;
                match.UnitsB = isBye ? null : unitsB;

                match.WinnerA = isBye ? true : winA;
                match.WinnerB = isBye ? false : winB;
                match.IsDraw = isBye ? false : draw;
            }

            _state.IsDirty = true;
            Storage.AutosaveSnapshot(_state);

            PushMatchesToPlayers(roundIndex);
            RefreshStandings();

            MessageBox.Show(this, $"Draft saved (Round {roundIndex}).", "Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void LockResults(DataGridView grid, int roundIndex)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.Tag is not Match match) continue;
                bool isBye = match.IsBye;

                if (!TryGetRequiredIntCell(row, "ScoreA", 1, 9, out int scoreA, out string errSA))
                {
                    MessageBox.Show(this, $"{ContextPrefix(match)} {errSA}", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                int? unitsA = null;
                if (!isBye)
                {
                    if (!TryGetRequiredIntCell(row, "UnitsA", 0, 99, out int ua, out string errUA))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errUA}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    unitsA = ua;
                }

                bool winA = row.Cells["WinA"].Value is bool wa && wa;
                bool winB = row.Cells["WinB"].Value is bool wb && wb;
                bool draw = row.Cells["Draw"].Value is bool dr && dr;

                if (!isBye)
                {
                    int outcomeCount = (winA ? 1 : 0) + (winB ? 1 : 0) + (draw ? 1 : 0);
                    if (outcomeCount != 1)
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} Διάλεξε ΑΚΡΙΒΩΣ ένα: Win A / Win B / Draw.", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    if (!TryGetRequiredIntCell(row, "ScoreB", 1, 9, out int sb, out string errSB))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errSB}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    if (!TryGetRequiredIntCell(row, "UnitsB", 0, 99, out int ub, out string errUB))
                    {
                        MessageBox.Show(this, $"{ContextPrefix(match)} {errUB}", "Validation",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    match.ScoreB = sb;
                    match.UnitsB = ub;
                    match.WinnerA = winA;
                    match.WinnerB = winB;
                    match.IsDraw = draw;
                }
                else
                {
                    match.ScoreB = null;
                    match.UnitsB = null;
                    match.WinnerA = true;
                    match.WinnerB = false;
                    match.IsDraw = false;
                }

                match.ScoreA = scoreA;
                match.UnitsA = isBye ? null : unitsA;
            }

            PushMatchesToPlayers(roundIndex);

            if (roundIndex == 1) _state.Round1Completed = true;
            if (roundIndex == 2) _state.Round2Completed = true;

            _state.IsDirty = true;
            Storage.AutosaveSnapshot(_state);

            grid.ReadOnly = true;
            RefreshStandings();
            RefreshButtons();

            MessageBox.Show(this, $"Round {roundIndex} results locked.", "Locked",
                MessageBoxButtons.OK, MessageBoxIcon.Information);

            ShowStandings();
            RefreshStandings();
            RefreshButtons();
        }

        private void PushMatchesToPlayers(int roundIndex)
        {
            if (roundIndex == 1)
            {
                foreach (var p in _state.Players)
                {
                    p.Round1Score = null;
                    p.Round1Units = null;
                }

                foreach (var m in _state.Round1Matches)
                {
                    var pa = _state.Players.First(x => x.Id == m.PlayerAId);
                    pa.Round1Score = m.ScoreA;
                    pa.Round1Units = m.UnitsA;

                    if (m.PlayerBId.HasValue)
                    {
                        var pb = _state.Players.First(x => x.Id == m.PlayerBId.Value);
                        pb.Round1Score = m.ScoreB;
                        pb.Round1Units = m.UnitsB;
                    }
                }
            }
            else
            {
                foreach (var p in _state.Players)
                {
                    p.Round2Score = null;
                    p.Round2Units = null;
                }

                foreach (var m in _state.Round2Matches)
                {
                    var pa = _state.Players.First(x => x.Id == m.PlayerAId);
                    pa.Round2Score = m.ScoreA;
                    pa.Round2Units = m.UnitsA;

                    if (m.PlayerBId.HasValue)
                    {
                        var pb = _state.Players.First(x => x.Id == m.PlayerBId.Value);
                        pb.Round2Score = m.ScoreB;
                        pb.Round2Units = m.UnitsB;
                    }
                }
            }
        }

        private bool TryGetOptionalIntCell(DataGridViewRow row, string colName, int min, int max, out int? value, out string error)
        {
            error = "";
            value = null;

            var txt = (row.Cells[colName].Value?.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(txt))
                return true;

            if (!int.TryParse(txt, out int v))
            {
                error = $"{FriendlyField(colName)} πρέπει να είναι αριθμός.";
                return false;
            }

            if (v < min || v > max)
            {
                error = $"{FriendlyField(colName)} πρέπει να είναι {min}–{max}.";
                return false;
            }

            value = v;
            return true;
        }

        private bool TryGetRequiredIntCell(DataGridViewRow row, string colName, int min, int max, out int value, out string error)
        {
            error = "";
            value = 0;

            var txt = (row.Cells[colName].Value?.ToString() ?? "").Trim();
            if (string.IsNullOrWhiteSpace(txt))
            {
                error = $"{FriendlyField(colName)} είναι υποχρεωτικό.";
                return false;
            }

            if (!int.TryParse(txt, out value))
            {
                error = $"{FriendlyField(colName)} πρέπει να είναι αριθμός.";
                return false;
            }

            if (value < min || value > max)
            {
                error = $"{FriendlyField(colName)} πρέπει να είναι {min}–{max}.";
                return false;
            }

            return true;
        }

        // =======================
        // Final
        // =======================
        private void BuildFinalPage()
        {
            _lblFinal = new Label { Left = 15, Top = 15, AutoSize = true, Text = "Final Standings" };
            _lblW1 = new Label { Left = 15, Top = 50, AutoSize = true };
            _lblW2 = new Label { Left = 15, Top = 75, AutoSize = true };
            _lblW3 = new Label { Left = 15, Top = 100, AutoSize = true };

            _btnFinalBack = new Button { Text = "Back", Left = 1015, Top = 20, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _btnFinalExport = new Button { Text = "Export Excel", Left = 860, Top = 20, Width = 140, Height = 32, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnFinalBack, "Return to the previous or setup screen.");
            tooltip.SetToolTip(_btnFinalExport, "Export final standings to Excel.");

            _btnFinalBack.Click += (_, __) => { ShowStandings(); RefreshStandings(); RefreshButtons(); };
            _btnFinalExport.Click += (_, __) =>
            {
                using var sfd = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    FileName = string.IsNullOrWhiteSpace(_state.TournamentName) ? "Tournament.xlsx" : $"{_state.TournamentName}.xlsx"
                };
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    ExcelExporter.ExportTournament(_state, sfd.FileName);
                    MessageBox.Show(this, "Exported.", "OK", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            _panelFinal.Controls.AddRange(new Control[] { _lblFinal, _lblW1, _lblW2, _lblW3, _btnFinalExport, _btnFinalBack });
        }

        private void PrepareFinal()
        {
            var standings = _state.GetStandings().ToList();
            _lblW1.Text = standings.Count > 0 ? $"1) {standings[0].FullName} - {standings[0].TotalScoreSoFar} / {standings[0].TotalUnits}" : "";
            _lblW2.Text = standings.Count > 1 ? $"2) {standings[1].FullName} - {standings[1].TotalScoreSoFar} / {standings[1].TotalUnits}" : "";
            _lblW3.Text = standings.Count > 2 ? $"3) {standings[2].FullName} - {standings[2].TotalScoreSoFar} / {standings[2].TotalUnits}" : "";
        }

        // =======================
        // Navigation
        // =======================
        private void ShowStandings()
        {
            HideAll();
            _panelStandings.Visible = true;
            SetPageTitle("Standings");
            HideRoundStatus();
        }

        private void ShowR1Draw()
        {
            HideAll();
            _panelR1Draw.Visible = true;
            SetPageTitle("Round 1 Draw");
            HideRoundStatus();
        }

        private void ShowR1Results()
        {
            HideAll();
            _panelR1Results.Visible = true;
            SetPageTitle("Round 1 Results");
            ShowRoundStatus(1);
        }

        private void ShowR2Draw()
        {
            HideAll();
            _panelR2Draw.Visible = true;
            SetPageTitle("Round 2 Draw");
            HideRoundStatus();
        }

        private void ShowR2Results()
        {
            HideAll();
            _panelR2Results.Visible = true;
            SetPageTitle("Round 2 Results");
            ShowRoundStatus(2);
        }

        private void ShowFinal()
        {
            HideAll();
            _panelFinal.Visible = true;
            SetPageTitle("Final");
            HideRoundStatus();
        }

        // =======================
        // Row numbering
        // =======================
        private void EnableGridRowNumbers()
        {
            foreach (var grid in GetAllControls(this).OfType<DataGridView>())
            {
                grid.RowPostPaint -= Grid_RowPostPaint;
                grid.RowPostPaint += Grid_RowPostPaint;

                if (grid.RowHeadersWidth < 50) grid.RowHeadersWidth = 50;
                grid.RowHeadersVisible = true;
            }
        }

        private static IEnumerable<Control> GetAllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                foreach (var child in GetAllControls(c))
                    yield return child;

                yield return c;
            }

            yield return root;
        }

        private void Grid_RowPostPaint(object? sender, DataGridViewRowPostPaintEventArgs e)
        {
            if (sender is not DataGridView grid) return;

            var rowNumber = (e.RowIndex + 1).ToString();

            using var centerFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(
                e.RowBounds.Left,
                e.RowBounds.Top,
                grid.RowHeadersWidth,
                e.RowBounds.Height);

            e.Graphics.DrawString(
                rowNumber,
                grid.Font,
                SystemBrushes.ControlText,
                headerBounds,
                centerFormat);
        }
    }
}