using FiveTogether.Core;
using FiveTogether.Models;

namespace FiveTogether.UI;

/// <summary>
/// Main application form — controller slot management dashboard.
/// Shows detected controllers, active slots, and provides swap functionality.
/// </summary>
public partial class MainForm : Form
{
    private readonly SlotManager _slotManager;
    private System.Windows.Forms.Timer? _activityTimer;

    // Track last activity time per slot for UI indicators
    private readonly DateTime[] _lastActivity = new DateTime[4];

    public MainForm()
    {
        InitializeComponent();
        _slotManager = new SlotManager();

        // Wire up events
        _slotManager.OnSlotError += SlotManager_OnSlotError;
        _slotManager.OnSlotActivity += SlotManager_OnSlotActivity;
        _slotManager.OnSessionStateChanged += SlotManager_OnSessionStateChanged;

        // Check drivers on load
        Load += MainForm_Load;
        FormClosing += MainForm_FormClosing;
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        CheckDriverStatus();
        RefreshControllerList();

        // Timer to update activity indicators every 500ms
        _activityTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _activityTimer.Tick += (_, _) => UpdateActivityIndicators();
        _activityTimer.Start();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_slotManager.IsSessionActive)
        {
            var result = MessageBox.Show(
                "A session is active. Stopping the session will restore all controllers to normal.\n\nClose anyway?",
                "FiveTogether",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }
        }

        _activityTimer?.Stop();
        _slotManager.Dispose();
    }

    // ─── Driver Status ────────────────────────────────────────────────────

    private void CheckDriverStatus()
    {
        var (hidHideOk, vigemOk) = SlotManager.CheckDrivers();

        lblHidHideStatus.Text = hidHideOk ? "✅ Installed" : "❌ Not Installed";
        lblHidHideStatus.ForeColor = hidHideOk ? Color.Green : Color.Red;

        lblViGEmStatus.Text = vigemOk ? "✅ Installed" : "❌ Not Installed";
        lblViGEmStatus.ForeColor = vigemOk ? Color.Green : Color.Red;

        btnStartSession.Enabled = hidHideOk && vigemOk;

        if (!hidHideOk || !vigemOk)
        {
            lblSessionStatus.Text = "⚠️ Install missing drivers before starting a session.";
            lblSessionStatus.ForeColor = Color.OrangeRed;
        }
    }

    // ─── Controller Detection ────────────────────────────────────────────

    private void btnRefresh_Click(object sender, EventArgs e)
    {
        RefreshControllerList();
    }

    private void RefreshControllerList()
    {
        var controllers = _slotManager.RefreshControllers();

        lstControllers.Items.Clear();
        foreach (var controller in controllers)
        {
            lstControllers.Items.Add(new ListViewItem(new[]
            {
                controller.Name,
                $"VID:{controller.VendorId:X4} PID:{controller.ProductId:X4}",
                controller.IsAssigned ? $"Slot {controller.AssignedSlotIndex + 1}" : "Unassigned",
            })
            {
                Tag = controller,
                ForeColor = controller.IsAssigned ? Color.White : Color.LightGray,
                BackColor = controller.IsAssigned ? Color.FromArgb(40, 80, 40) : Color.FromArgb(50, 50, 50),
            });
        }

        lblControllerCount.Text = $"{controllers.Count} controller(s) detected";
    }

    // ─── Session Control ─────────────────────────────────────────────────

    private void btnStartSession_Click(object sender, EventArgs e)
    {
        if (_slotManager.IsSessionActive)
        {
            _slotManager.StopSession();
        }
        else
        {
            // Refresh controllers before starting
            RefreshControllerList();

            if (_slotManager.DetectedControllers.Count == 0)
            {
                MessageBox.Show("No controllers detected. Connect your controllers and click Refresh.",
                    "FiveTogether", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var (success, message) = _slotManager.StartSession();

            if (success)
            {
                lblSessionStatus.Text = $"🟢 {message}";
                lblSessionStatus.ForeColor = Color.LimeGreen;
            }
            else
            {
                MessageBox.Show($"Failed to start session:\n{message}",
                    "FiveTogether", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            RefreshControllerList();
            UpdateSlotPanel();
        }
    }

    // ─── Slot Swap ───────────────────────────────────────────────────────

    private void btnSwap_Click(object sender, EventArgs e)
    {
        if (sender is not Button btn || btn.Tag is not int slotIndex)
            return;

        // Get unassigned controllers
        var unassigned = _slotManager.DetectedControllers
            .Where(c => !c.IsAssigned)
            .ToList();

        if (unassigned.Count == 0)
        {
            MessageBox.Show("No unassigned controllers available to swap in.",
                "FiveTogether", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Show selection dialog
        using var dialog = new ControllerSelectDialog(unassigned, slotIndex);
        if (dialog.ShowDialog(this) == DialogResult.OK && dialog.SelectedController != null)
        {
            var (success, message) = _slotManager.SwapSlot(slotIndex, dialog.SelectedController);

            if (success)
            {
                lblSessionStatus.Text = $"🔄 {message}";
                lblSessionStatus.ForeColor = Color.Cyan;
            }
            else
            {
                MessageBox.Show($"Swap failed:\n{message}",
                    "FiveTogether", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshControllerList();
            UpdateSlotPanel();
        }
    }

    // ─── Slot Panel Updates ──────────────────────────────────────────────

    private void UpdateSlotPanel()
    {
        var slots = _slotManager.Slots;

        for (int i = 0; i < 4; i++)
        {
            var slot = slots[i];
            var lblName = slotPanel.Controls.Find($"lblSlot{i}Name", true).FirstOrDefault() as Label;
            var lblStatus = slotPanel.Controls.Find($"lblSlot{i}Status", true).FirstOrDefault() as Label;
            var btnSwap = slotPanel.Controls.Find($"btnSlot{i}Swap", true).FirstOrDefault() as Button;

            if (lblName != null)
            {
                lblName.Text = slot.AssignedController?.Name ?? "(Empty)";
            }

            if (lblStatus != null)
            {
                if (slot.IsForwarding)
                {
                    lblStatus.Text = "● Active";
                    lblStatus.ForeColor = Color.LimeGreen;
                }
                else if (slot.IsActive)
                {
                    lblStatus.Text = "○ Idle";
                    lblStatus.ForeColor = Color.Yellow;
                }
                else
                {
                    lblStatus.Text = "○ Inactive";
                    lblStatus.ForeColor = Color.Gray;
                }
            }

            if (btnSwap != null)
            {
                btnSwap.Enabled = _slotManager.IsSessionActive;
            }
        }
    }

    private void UpdateActivityIndicators()
    {
        if (!_slotManager.IsSessionActive)
            return;

        var now = DateTime.UtcNow;
        for (int i = 0; i < 4; i++)
        {
            var lblStatus = slotPanel.Controls.Find($"lblSlot{i}Status", true).FirstOrDefault() as Label;
            if (lblStatus == null) continue;

            var timeSinceActivity = now - _lastActivity[i];
            if (timeSinceActivity.TotalMilliseconds < 300)
            {
                lblStatus.Text = "● Input";
                lblStatus.ForeColor = Color.Cyan;
            }
            else if (_slotManager.Slots[i].IsForwarding)
            {
                lblStatus.Text = "● Active";
                lblStatus.ForeColor = Color.LimeGreen;
            }
        }
    }

    // ─── Event Handlers ──────────────────────────────────────────────────

    private void SlotManager_OnSlotError(int slotIndex, string message)
    {
        if (InvokeRequired)
        {
            Invoke(() => SlotManager_OnSlotError(slotIndex, message));
            return;
        }

        lblSessionStatus.Text = $"⚠️ Slot {slotIndex + 1}: {message}";
        lblSessionStatus.ForeColor = Color.OrangeRed;
        UpdateSlotPanel();
    }

    private void SlotManager_OnSlotActivity(int slotIndex, ControllerInput input)
    {
        _lastActivity[slotIndex] = DateTime.UtcNow;
    }

    private void SlotManager_OnSessionStateChanged(bool active)
    {
        if (InvokeRequired)
        {
            Invoke(() => SlotManager_OnSessionStateChanged(active));
            return;
        }

        btnStartSession.Text = active ? "⏹ Stop Session" : "▶ Start Session";
        btnStartSession.BackColor = active ? Color.FromArgb(120, 40, 40) : Color.FromArgb(40, 100, 40);

        if (!active)
        {
            lblSessionStatus.Text = "⏸ Session stopped. Controllers restored.";
            lblSessionStatus.ForeColor = Color.Gray;
            RefreshControllerList();
            UpdateSlotPanel();
        }
    }
}
