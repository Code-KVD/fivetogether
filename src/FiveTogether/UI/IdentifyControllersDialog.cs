using FiveTogether.Core;
using FiveTogether.Models;

namespace FiveTogether.UI;

/// <summary>
/// Guides each player through pressing A to claim their controller.
/// Opens HID streams for all detected controllers and listens in the background.
/// The first controller to send an A press each round gets assigned to the current player.
/// Minimum 2 players required to enable Start Session.
/// </summary>
public class IdentifyControllersDialog : Form
{
    // Pre-filled player names matching the group
    private static readonly string[] DefaultNames = { "Srikant", "KVD", "Ashpak", "Ekansh", "Debu" };

    private readonly List<PhysicalController> _controllers;
    private readonly ControllerIdentifier _identifier;
    private readonly List<(PhysicalController Controller, string PlayerName)> _identified = new();

    private Label lblPrompt = null!;
    private Label lblSubprompt = null!;
    private Panel pnlList = null!;
    private Button btnSkip = null!;
    private Button btnDone = null!;

    private int _currentPlayer = 0;
    private CancellationTokenSource? _listenCts;

    /// <summary>
    /// Ordered list of (controller, playerName) pairs built during identification.
    /// Slots are assigned in this order: index 0 → Slot 1, index 1 → Slot 2, etc.
    /// The 5th entry (if present) is the bench player.
    /// </summary>
    public List<(PhysicalController Controller, string PlayerName)> IdentifiedControllers => _identified;

    public IdentifyControllersDialog(List<PhysicalController> controllers)
    {
        _controllers = controllers;
        _identifier = new ControllerIdentifier();
        BuildUI();
        Load += OnLoad;
        FormClosing += OnFormClosing;
    }

    // ─── UI Construction ─────────────────────────────────────────────────

    private void BuildUI()
    {
        Text = "Identify Controllers";
        Size = new Size(500, 500);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(30, 30, 35);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        Controls.Add(new Label
        {
            Text = "🎮  Controller Identification",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 200, 255),
            Location = new Point(20, 18),
            AutoSize = true,
        });

        lblPrompt = new Label
        {
            Font = new Font("Segoe UI", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(20, 65),
            Size = new Size(450, 28),
        };
        Controls.Add(lblPrompt);

        lblSubprompt = new Label
        {
            Font = new Font("Segoe UI", 9f),
            ForeColor = Color.Silver,
            Location = new Point(20, 97),
            Size = new Size(450, 20),
        };
        Controls.Add(lblSubprompt);

        Controls.Add(new Label
        {
            Text = "IDENTIFIED PLAYERS",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = Color.Gray,
            Location = new Point(20, 135),
            AutoSize = true,
        });

        pnlList = new Panel
        {
            Location = new Point(20, 157),
            Size = new Size(450, 220),
            BackColor = Color.FromArgb(40, 40, 48),
        };
        Controls.Add(pnlList);

        for (int i = 0; i < 5; i++)
        {
            pnlList.Controls.Add(new Label
            {
                Name = $"row{i}",
                Text = $"Player {i + 1}  ({DefaultNames[i]}):  —",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(80, 80, 90),
                Location = new Point(14, 14 + i * 40),
                Size = new Size(420, 24),
            });
        }

        btnSkip = new Button
        {
            Text = "Skip Player",
            Location = new Point(20, 398),
            Size = new Size(115, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(65, 55, 25),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        btnSkip.FlatAppearance.BorderColor = Color.FromArgb(110, 90, 35);
        btnSkip.Click += BtnSkip_Click;
        Controls.Add(btnSkip);

        btnDone = new Button
        {
            Text = "▶  Start Session",
            Location = new Point(270, 398),
            Size = new Size(190, 36),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 100, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            Enabled = false,
            DialogResult = DialogResult.OK,
        };
        btnDone.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
        Controls.Add(btnDone);

        AcceptButton = btnDone;
        CancelButton = null;

        // Manual cancel via form X button — stop listening and return Cancel
        FormClosing += (_, e) =>
        {
            if (DialogResult != DialogResult.OK)
            {
                _identified.Clear();
                DialogResult = DialogResult.Cancel;
            }
        };
    }

    // ─── Lifecycle ───────────────────────────────────────────────────────

    private void OnLoad(object? sender, EventArgs e)
    {
        var opened = _identifier.StartListening(_controllers);
        if (opened == 0)
        {
            lblPrompt.Text = "No controllers could be opened.";
            lblSubprompt.Text = "Try refreshing the controller list and restarting.";
            btnSkip.Enabled = false;
            return;
        }
        AdvanceToNextPlayer();
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        CancelListening();
        _identifier.Dispose();
    }

    // ─── Player Advance ──────────────────────────────────────────────────

    private void AdvanceToNextPlayer()
    {
        // No more player slots or no unidentified controllers left
        var used = _identified.Select(x => x.Controller.DevicePath).ToHashSet();
        var remaining = _controllers.Count(c => !used.Contains(c.DevicePath));

        if (_currentPlayer >= 5 || remaining == 0)
        {
            FinishIdentification();
            return;
        }

        SetPrompt();
        BeginListening();
    }

    private void SetPrompt()
    {
        var name = _currentPlayer < DefaultNames.Length ? DefaultNames[_currentPlayer] : $"Player {_currentPlayer + 1}";
        lblPrompt.Text = $"Player {_currentPlayer + 1} — Press  A  on your controller";
        lblSubprompt.Text = $"Hi {name}! Press the  A  button (bottom face button) now";
        btnDone.Enabled = _identified.Count >= 2;
    }

    private void BeginListening()
    {
        CancelListening();
        _listenCts = new CancellationTokenSource();
        var token = _listenCts.Token;
        var already = _identified.Select(x => x.Controller.DevicePath).ToHashSet();

        _ = Task.Run(() =>
        {
            var ctrl = _identifier.WaitForAPress(already, token);
            if (ctrl != null && !token.IsCancellationRequested)
                Invoke(() => OnControllerIdentified(ctrl));
        }, token);
    }

    // ─── Identification Events ───────────────────────────────────────────

    private void OnControllerIdentified(PhysicalController controller)
    {
        var name = _currentPlayer < DefaultNames.Length ? DefaultNames[_currentPlayer] : $"Player {_currentPlayer + 1}";
        controller.Label = name;
        _identified.Add((controller, name));

        // Update row
        if (pnlList.Controls.Find($"row{_currentPlayer}", false).FirstOrDefault() is Label row)
        {
            row.Text = $"✅  Player {_currentPlayer + 1}  ({name}):  {controller.Name}";
            row.ForeColor = Color.LimeGreen;
        }

        _currentPlayer++;
        AdvanceToNextPlayer();
    }

    private void FinishIdentification()
    {
        CancelListening();
        lblPrompt.Text = _identified.Count > 0 ? "All players identified!" : "No players identified.";
        lblSubprompt.Text = _identified.Count >= 2
            ? $"{_identified.Count} player(s) ready — click Start Session."
            : "Need at least 2 players to start.";
        btnSkip.Enabled = false;
        btnDone.Enabled = _identified.Count >= 2;
    }

    // ─── Button Handlers ─────────────────────────────────────────────────

    private void BtnSkip_Click(object? sender, EventArgs e)
    {
        CancelListening();

        if (pnlList.Controls.Find($"row{_currentPlayer}", false).FirstOrDefault() is Label row)
        {
            row.Text = $"⏭  Player {_currentPlayer + 1}:  Skipped";
            row.ForeColor = Color.DimGray;
        }

        _currentPlayer++;
        AdvanceToNextPlayer();
    }

    private void CancelListening()
    {
        _listenCts?.Cancel();
        _listenCts?.Dispose();
        _listenCts = null;
    }
}
