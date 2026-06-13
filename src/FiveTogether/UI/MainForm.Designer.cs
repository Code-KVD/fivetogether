namespace FiveTogether.UI;

/// <summary>
/// Designer-generated UI layout for MainForm.
/// Dark themed, functional layout with slot panels and controller list.
/// </summary>
partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Controls
    private Label lblTitle = null!;
    private Label lblDriverHeader = null!;
    private Label lblHidHideLabel = null!;
    private Label lblHidHideStatus = null!;
    private Label lblViGEmLabel = null!;
    private Label lblViGEmStatus = null!;
    private Label lblControllerHeader = null!;
    private Label lblControllerCount = null!;
    private ListView lstControllers = null!;
    private Button btnRefresh = null!;
    private Panel slotPanel = null!;
    private Label lblSlotHeader = null!;
    private Button btnStartSession = null!;
    private Label lblSessionStatus = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();

        // ─── Form Settings ───────────────────────────────
        Text = "FiveTogether — Controller Manager";
        Size = new Size(640, 720);
        MinimumSize = new Size(600, 650);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(30, 30, 35);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;

        // ─── Title ───────────────────────────────────────
        lblTitle = new Label
        {
            Text = "🎮 FiveTogether",
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 200, 255),
            Location = new Point(20, 15),
            AutoSize = true,
        };
        Controls.Add(lblTitle);

        // ─── Driver Status ───────────────────────────────
        lblDriverHeader = new Label
        {
            Text = "DRIVER STATUS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.Gray,
            Location = new Point(20, 60),
            AutoSize = true,
        };
        Controls.Add(lblDriverHeader);

        var driverPanel = new Panel
        {
            Location = new Point(20, 80),
            Size = new Size(580, 50),
            BackColor = Color.FromArgb(40, 40, 48),
        };
        Controls.Add(driverPanel);

        lblHidHideLabel = new Label
        {
            Text = "HidHide:",
            Location = new Point(15, 15),
            AutoSize = true,
            ForeColor = Color.LightGray,
        };
        driverPanel.Controls.Add(lblHidHideLabel);

        lblHidHideStatus = new Label
        {
            Text = "Checking...",
            Location = new Point(90, 15),
            AutoSize = true,
        };
        driverPanel.Controls.Add(lblHidHideStatus);

        lblViGEmLabel = new Label
        {
            Text = "ViGEmBus:",
            Location = new Point(280, 15),
            AutoSize = true,
            ForeColor = Color.LightGray,
        };
        driverPanel.Controls.Add(lblViGEmLabel);

        lblViGEmStatus = new Label
        {
            Text = "Checking...",
            Location = new Point(365, 15),
            AutoSize = true,
        };
        driverPanel.Controls.Add(lblViGEmStatus);

        // ─── Controller List ─────────────────────────────
        lblControllerHeader = new Label
        {
            Text = "DETECTED CONTROLLERS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.Gray,
            Location = new Point(20, 145),
            AutoSize = true,
        };
        Controls.Add(lblControllerHeader);

        lblControllerCount = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.DarkGray,
            Location = new Point(200, 145),
            AutoSize = true,
        };
        Controls.Add(lblControllerCount);

        btnRefresh = new Button
        {
            Text = "🔄 Refresh",
            Location = new Point(500, 140),
            Size = new Size(100, 28),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(50, 60, 80),
            ForeColor = Color.White,
            Cursor = Cursors.Hand,
        };
        btnRefresh.FlatAppearance.BorderColor = Color.FromArgb(80, 100, 140);
        btnRefresh.Click += btnRefresh_Click;
        Controls.Add(btnRefresh);

        lstControllers = new ListView
        {
            Location = new Point(20, 170),
            Size = new Size(580, 130),
            View = View.Details,
            FullRowSelect = true,
            BackColor = Color.FromArgb(40, 40, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            HeaderStyle = ColumnHeaderStyle.Nonclickable,
            Font = new Font("Segoe UI", 9f),
        };
        lstControllers.Columns.Add("Controller Name", 250);
        lstControllers.Columns.Add("Device ID", 170);
        lstControllers.Columns.Add("Status", 140);
        Controls.Add(lstControllers);

        // ─── Slot Panel ──────────────────────────────────
        lblSlotHeader = new Label
        {
            Text = "VIRTUAL SLOTS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = Color.Gray,
            Location = new Point(20, 315),
            AutoSize = true,
        };
        Controls.Add(lblSlotHeader);

        slotPanel = new Panel
        {
            Location = new Point(20, 335),
            Size = new Size(580, 240),
            BackColor = Color.FromArgb(35, 35, 42),
            AutoScroll = false,
        };
        Controls.Add(slotPanel);

        // Create slot rows
        for (int i = 0; i < 4; i++)
        {
            CreateSlotRow(i, slotPanel);
        }

        // ─── Session Controls ────────────────────────────
        btnStartSession = new Button
        {
            Text = "▶ Start Session",
            Location = new Point(20, 590),
            Size = new Size(580, 42),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 100, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        btnStartSession.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
        btnStartSession.Click += btnStartSession_Click;
        Controls.Add(btnStartSession);

        lblSessionStatus = new Label
        {
            Text = "⏸ Ready. Detect controllers and start a session.",
            Location = new Point(20, 645),
            Size = new Size(580, 25),
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 9f),
        };
        Controls.Add(lblSessionStatus);
    }

    /// <summary>
    /// Creates a single slot row in the slot panel.
    /// Each row has: slot label, controller name, status indicator, swap button.
    /// </summary>
    private void CreateSlotRow(int slotIndex, Panel parent)
    {
        int y = slotIndex * 58 + 5;

        var rowPanel = new Panel
        {
            Location = new Point(5, y),
            Size = new Size(568, 52),
            BackColor = Color.FromArgb(45, 45, 55),
        };
        parent.Controls.Add(rowPanel);

        // Slot label
        var lblSlotNum = new Label
        {
            Text = $"SLOT {slotIndex + 1}",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 200, 255),
            Location = new Point(12, 16),
            AutoSize = true,
        };
        rowPanel.Controls.Add(lblSlotNum);

        // Controller name
        var lblName = new Label
        {
            Name = $"lblSlot{slotIndex}Name",
            Text = "(Empty)",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.White,
            Location = new Point(80, 16),
            AutoSize = true,
        };
        rowPanel.Controls.Add(lblName);

        // Status indicator
        var lblStatus = new Label
        {
            Name = $"lblSlot{slotIndex}Status",
            Text = "○ Inactive",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.Gray,
            Location = new Point(340, 17),
            AutoSize = true,
        };
        rowPanel.Controls.Add(lblStatus);

        // Swap button
        var btnSwap = new Button
        {
            Name = $"btnSlot{slotIndex}Swap",
            Text = "Swap",
            Tag = slotIndex,
            Location = new Point(470, 10),
            Size = new Size(80, 32),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 90),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Enabled = false,
        };
        btnSwap.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 130);
        btnSwap.Click += btnSwap_Click;
        rowPanel.Controls.Add(btnSwap);
    }
}
