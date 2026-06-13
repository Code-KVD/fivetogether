using FiveTogether.Models;

namespace FiveTogether.UI;

/// <summary>
/// Simple dialog to select which unassigned controller to swap into a slot.
/// </summary>
public class ControllerSelectDialog : Form
{
    private ListBox lstControllers = null!;
    private Button btnOk = null!;
    private Button btnCancel = null!;

    public PhysicalController? SelectedController { get; private set; }

    public ControllerSelectDialog(List<PhysicalController> unassignedControllers, int slotIndex)
    {
        // Form settings
        Text = $"Select Controller for Slot {slotIndex + 1}";
        Size = new Size(380, 260);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(30, 30, 35);
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        // Instruction label
        var lblInstruction = new Label
        {
            Text = "Choose a controller to assign to this slot:",
            Location = new Point(15, 15),
            AutoSize = true,
            ForeColor = Color.LightGray,
        };
        Controls.Add(lblInstruction);

        // Controller list
        lstControllers = new ListBox
        {
            Location = new Point(15, 40),
            Size = new Size(335, 120),
            BackColor = Color.FromArgb(40, 40, 48),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10f),
        };

        foreach (var controller in unassignedControllers)
        {
            lstControllers.Items.Add(controller);
        }

        if (lstControllers.Items.Count > 0)
            lstControllers.SelectedIndex = 0;

        Controls.Add(lstControllers);

        // OK button
        btnOk = new Button
        {
            Text = "Assign",
            Location = new Point(155, 175),
            Size = new Size(90, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(40, 100, 40),
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            DialogResult = DialogResult.OK,
            Cursor = Cursors.Hand,
        };
        btnOk.FlatAppearance.BorderColor = Color.FromArgb(60, 140, 60);
        btnOk.Click += (_, _) =>
        {
            SelectedController = lstControllers.SelectedItem as PhysicalController;
        };
        Controls.Add(btnOk);

        // Cancel button
        btnCancel = new Button
        {
            Text = "Cancel",
            Location = new Point(260, 175),
            Size = new Size(90, 35),
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(60, 60, 60),
            ForeColor = Color.White,
            DialogResult = DialogResult.Cancel,
            Cursor = Cursors.Hand,
        };
        btnCancel.FlatAppearance.BorderColor = Color.FromArgb(90, 90, 90);
        Controls.Add(btnCancel);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }
}
