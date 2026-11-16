using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace TournamentPairings
{
    public partial class SetupForm : Form
    {
        // When SetupForm closes with OK, the resulting state (new or updated) is returned here
        public TournamentState? CreatedState { get; private set; }

        private readonly TournamentState? _editingState;
        private readonly bool _isEditMode;

        private TextBox _txtName = null!;
        private NumericUpDown _numTables = null!;
        private DataGridView _grid = null!;
        private Button _btnCreateOrApply = null!;
        private Button _btnBack = null!;
        private Button _btnSaveAs = null!;

        // Create mode
        public SetupForm()
        {
            InitializeComponent();
            _isEditMode = false;
            _editingState = null;

            Text = "Create New Tournament";
            Width = 920;
            Height = 600;

            BuildUi();
            UiScaler.Attach(this);
        }

        // Edit mode (going back before the Round 1 Draw)
        public SetupForm(TournamentState existingState)
        {
            InitializeComponent();
            _isEditMode = true;
            _editingState = existingState;

            Text = "Edit Tournament Setup";
            Width = 920;
            Height = 600;

            BuildUi();
            LoadFromExistingState();
            UiScaler.Attach(this);
        }

        private void BuildUi()
        {
            var lblName = new Label { Text = "Tournament Name:", Left = 15, Top = 15, AutoSize = true };
            _txtName = new TextBox { Left = 150, Top = 12, Width = 400 };

            var lblTables = new Label { Text = "Tables:", Left = 580, Top = 15, AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _numTables = new NumericUpDown { Left = 640, Top = 12, Width = 80, Minimum = 1, Maximum = 200, Value = 8, Anchor = AnchorStyles.Top | AnchorStyles.Right };

            Controls.Add(lblName);
            Controls.Add(_txtName);
            Controls.Add(lblTables);
            Controls.Add(_numTables);

            _grid = new DataGridView
            {
                Left = 15,
                Top = 50,
                Width = 870,
                Height = 430,
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_grid);

            // IMPORTANT: handle DataError so ComboBox doesn't show the default exception dialog
            _grid.DataError += Grid_DataError;

            // Columns
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "FullName",
                HeaderText = "Full Name"
            });

            var colSide = new DataGridViewComboBoxColumn
            {
                Name = "Side",
                HeaderText = "Side",
                DataSource = Enum.GetValues(typeof(Side)),
                ValueType = typeof(Side),
                FlatStyle = FlatStyle.Flat
            };
            _grid.Columns.Add(colSide);

            _grid.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "Special",
                HeaderText = "Special"
            });

            // Default values for new rows to avoid "value is not valid"
            _grid.DefaultValuesNeeded += (_, e) =>
            {
                e.Row.Cells["Side"].Value = Side.Axis;
                e.Row.Cells["Special"].Value = false;
            };

            // Also protect existing rows when user enters Side cell that is still null
            _grid.CellBeginEdit += (_, e) =>
            {
                if (_grid.Columns[e.ColumnIndex].Name == "Side")
                {
                    var cell = _grid.Rows[e.RowIndex].Cells["Side"];
                    if (cell.Value == null)
                        cell.Value = Side.Axis;
                }
            };

            _btnCreateOrApply = new Button
            {
                Text = _isEditMode ? "Apply Changes" : "Create",
                Left = 15,
                Top = 495,
                Width = 140,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            _btnSaveAs = new Button
            {
                Text = _isEditMode ? "Apply & Save As..." : "Create & Save As...",
                Left = 165,
                Top = 495,
                Width = 170,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            _btnBack = new Button
            {
                Text = "Back",
                Left = 765,
                Top = 495,
                Width = 120,
                Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };

            Controls.Add(_btnCreateOrApply);
            Controls.Add(_btnSaveAs);
            Controls.Add(_btnBack);

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnCreateOrApply, _isEditMode ? "Apply changes to the current tournament (optionally save as new file)." : "Create a new tournament (with optional save location).");
            tooltip.SetToolTip(_btnSaveAs, _isEditMode ? "Apply changes to the current tournament (optionally save as new file)." : "Create a new tournament (with optional save location).");
            tooltip.SetToolTip(_btnBack, "Return to the previous or setup screen.");

            _btnBack.Click += (_, __) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            _btnCreateOrApply.Click += (_, __) =>
            {
                // In edit mode, no changes are allowed once the Round 1 Draw has been made
                if (_isEditMode && _editingState != null && _editingState.Round1Drawn)
                {
                    MessageBox.Show(this,
                        "You can't edit the setup after Round 1 Draw.\n\nCreate a new tournament or restart before drawing Round 1.",
                        "Not allowed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var state = TryBuildState();
                if (state == null) return;

                // Create mode: just return the new state
                if (!_isEditMode)
                {
                    Storage.AutosaveSnapshot(state);
                    CreatedState = state;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                // Edit mode: update the existing state (so the Hub keeps working with the same object)
                ApplyToExistingState(state);
                Storage.AutosaveSnapshot(_editingState!);

                CreatedState = _editingState;
                DialogResult = DialogResult.OK;
                Close();
            };

            _btnSaveAs.Click += (_, __) =>
            {
                // In edit mode, no changes are allowed once the Round 1 Draw has been made
                if (_isEditMode && _editingState != null && _editingState.Round1Drawn)
                {
                    MessageBox.Show(this,
                        "You can't edit the setup after Round 1 Draw.\n\nCreate a new tournament or restart before drawing Round 1.",
                        "Not allowed",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var state = TryBuildState();
                if (state == null) return;

                // In edit mode, apply to the existing state first
                if (_isEditMode)
                {
                    ApplyToExistingState(state);
                    state = _editingState!;
                }

                using var sfd = new SaveFileDialog
                {
                    Filter = "Tournament Files (*.tournament)|*.tournament",
                    FileName = $"{Storage.MakeFileSafe(state.TournamentName)}.tournament",
                    Title = "Save Tournament"
                };

                if (sfd.ShowDialog(this) != DialogResult.OK)
                    return;

                Storage.SaveTournament(state, sfd.FileName);
                CreatedState = state;

                DialogResult = DialogResult.OK;
                Close();
            };
        }

        private void LoadFromExistingState()
        {
            if (_editingState == null) return;

            _txtName.Text = _editingState.TournamentName;
            _numTables.Value = Math.Max(_numTables.Minimum, Math.Min(_numTables.Maximum, _editingState.TableCount));

            _grid.Rows.Clear();

            foreach (var p in _editingState.Players.OrderBy(p => p.FullName))
            {
                int idx = _grid.Rows.Add();
                var row = _grid.Rows[idx];
                row.Cells["FullName"].Value = p.FullName;
                row.Cells["Side"].Value = p.Side;
                row.Cells["Special"].Value = p.IsSpecial;
            }

            // If Round 1 has been drawn, lock the UI (read-only)
            if (_editingState.Round1Drawn)
            {
                _txtName.ReadOnly = true;
                _numTables.Enabled = false;
                _grid.ReadOnly = true;
                _grid.AllowUserToAddRows = false;
                _grid.AllowUserToDeleteRows = false;

                _btnCreateOrApply.Enabled = false;
                _btnSaveAs.Enabled = false;
            }
        }

        private void ApplyToExistingState(TournamentState newState)
        {
            if (_editingState == null) return;

            // Safety: if Round 1 has been drawn, change nothing
            if (_editingState.Round1Drawn)
                return;

            _editingState.TournamentName = newState.TournamentName;
            _editingState.TableCount = newState.TableCount;

            // Before the Round 1 Draw the players can be replaced completely
            // (new IDs). This is the simplest/safest way without breaking matches.
            _editingState.Players = newState.Players;

            // Reset progress (clean state in case something was clicked by mistake)
            _editingState.Round1Matches.Clear();
            _editingState.Round2Matches.Clear();
            _editingState.Round1Drawn = false;
            _editingState.Round1Completed = false;
            _editingState.Round2Drawn = false;
            _editingState.Round2Completed = false;

            // Reset withdraw flags (optional, but cleaner)
            foreach (var p in _editingState.Players)
                p.IsWithdrawnForRound2 = false;

            _editingState.IsDirty = true;
        }

        private void Grid_DataError(object? sender, DataGridViewDataErrorEventArgs e)
        {
            // Avoid showing the default "DataGridViewComboBoxCell value is not valid" dialog.
            e.ThrowException = false;
            e.Cancel = true;
        }

        private TournamentState? TryBuildState()
        {
            var name = (_txtName.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(this, "Tournament Name is required.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var players = new List<Player>();

            foreach (DataGridViewRow row in _grid.Rows)
            {
                if (row.IsNewRow) continue;

                var fullName = (row.Cells["FullName"].Value?.ToString() ?? "").Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    continue;

                var sideObj = row.Cells["Side"].Value;
                var side = sideObj is Side s ? s : Side.Axis;

                var special = row.Cells["Special"].Value is bool b && b;

                players.Add(new Player
                {
                    FullName = fullName,
                    Side = side,
                    IsSpecial = special
                });
            }

            if (players.Count < 2)
            {
                MessageBox.Show(this, "Please add at least 2 players.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            // Prevent duplicate names
            var dup = players
                .GroupBy(p => p.FullName, StringComparer.CurrentCultureIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .FirstOrDefault();

            if (dup != null)
            {
                MessageBox.Show(this, $"Duplicate player name detected: {dup}\nPlease make names unique.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            var state = new TournamentState
            {
                TournamentName = name,
                TableCount = (int)_numTables.Value,
                Players = players,
                IsDirty = true
            };

            return state;
        }
    }
}