using System;
using System.IO;
using System.Windows.Forms;

namespace TournamentPairings
{
    public partial class StartForm : Form
    {
        private readonly AppSettings _settings;

        private ListBox _recentList = null!;
        private Button _btnNew = null!;
        private Button _btnOpen = null!;
        private Button _btnOpenRecent = null!;
        private Label _lblCredits = null!;

        public StartForm()
        {
            InitializeComponent();

            Text = "Tournament Pairings - Start";
            Width = 650;
            Height = 420;

            _settings = AppSettings.Load();

            BuildUi();
            LoadRecent();
            UiScaler.Attach(this, PositionCredits);

            // keep credits at bottom-right
            Resize += (_, __) => PositionCredits();
            Shown += (_, __) => PositionCredits();
        }

        private void BuildUi()
        {
            var lbl = new Label
            {
                Text = "Start",
                AutoSize = true,
                Left = 20,
                Top = 15,
                Font = new System.Drawing.Font(Font.FontFamily, 14, System.Drawing.FontStyle.Bold)
            };
            Controls.Add(lbl);

            _btnNew = new Button { Text = "Create New Tournament", Left = 20, Top = 60, Width = 220, Height = 35 };
            _btnOpen = new Button { Text = "Open Tournament File", Left = 20, Top = 105, Width = 220, Height = 35 };
            Controls.Add(_btnNew);
            Controls.Add(_btnOpen);

            var btnHelp = new Button { Text = "Help", Left = 20, Top = 150, Width = 220, Height = 35 };
            btnHelp.Click += (_, __) => HelpWindow.Show(this);
            Controls.Add(btnHelp);

            _btnNew.Click += (_, __) =>
            {
                using var setup = new SetupForm();
                if (setup.ShowDialog(this) == DialogResult.OK && setup.CreatedState != null)
                {
                    OpenTournamentInHub(setup.CreatedState, addToRecent: true);
                }
            };

            _btnOpen.Click += (_, __) =>
            {
                using var ofd = new OpenFileDialog
                {
                    Filter = "Tournament Files (*.tournament)|*.tournament|All Files (*.*)|*.*",
                    Title = "Open Tournament"
                };

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var state = Storage.LoadTournament(ofd.FileName);
                        OpenTournamentInHub(state, addToRecent: true);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(this, $"Failed to open file.\n\n{ex.Message}", "Error",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            };

            var lblRecent = new Label { Text = "Recent", AutoSize = true, Left = 270, Top = 60 };
            Controls.Add(lblRecent);

            _recentList = new ListBox
            {
                Left = 270, Top = 85, Width = 340, Height = 220,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            Controls.Add(_recentList);

            _btnOpenRecent = new Button
            {
                Text = "Open Selected", Left = 270, Top = 315, Width = 140, Height = 35,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(_btnOpenRecent);

            _btnOpenRecent.Click += (_, __) =>
            {
                if (_recentList.SelectedItem is not string path) return;

                if (!File.Exists(path))
                {
                    MessageBox.Show(this, "File not found. It will be removed from Recent.", "Missing file",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _settings.RecentFiles.Remove(path);
                    _settings.Save();
                    LoadRecent();
                    return;
                }

                try
                {
                    var state = Storage.LoadTournament(path);
                    OpenTournamentInHub(state, addToRecent: true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"Failed to open file.\n\n{ex.Message}", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            _recentList.DoubleClick += (_, __) => _btnOpenRecent.PerformClick();

            // ✅ Credits bottom-right
            _lblCredits = new Label
            {
                Text = "Made by Thanasis Kaltsoudas",
                AutoSize = true
            };
            Controls.Add(_lblCredits);
            PositionCredits();

            var tooltip = new ToolTip();
            tooltip.SetToolTip(_btnNew, "Start a new tournament setup.");
            tooltip.SetToolTip(_btnOpen, "Load an existing tournament file.");
            tooltip.SetToolTip(_btnOpenRecent, "Load an existing tournament file.");
        }

        private void PositionCredits()
        {
            if (_lblCredits == null) return;

            const int margin = 10;
            _lblCredits.Left = Math.Max(margin, ClientSize.Width - _lblCredits.Width - margin);
            _lblCredits.Top = Math.Max(margin, ClientSize.Height - _lblCredits.Height - margin);
            _lblCredits.BringToFront();
        }

        private void LoadRecent()
        {
            _recentList.Items.Clear();
            foreach (var f in _settings.RecentFiles)
                _recentList.Items.Add(f);
        }

        private void OpenTournamentInHub(TournamentState state, bool addToRecent)
        {
            if (addToRecent && !string.IsNullOrWhiteSpace(state.FilePath))
                _settings.AddRecent(state.FilePath);

            Hide();
            using var hub = new HubForm(state);
            hub.ShowDialog(this);
            Show();

            LoadRecent();
            PositionCredits();
        }
    }
}