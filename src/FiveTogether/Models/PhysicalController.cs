namespace FiveTogether.Models;

/// <summary>
/// Represents a physical (or Parsec virtual) game controller detected on the system.
/// </summary>
public class PhysicalController
{
    /// <summary>
    /// Unique device path used to open the HID device (e.g., \\?\HID#VID_045E&PID_028E...).
    /// </summary>
    public string DevicePath { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable product name (e.g., "Xbox Controller", "Parsec Gamepad").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// USB Vendor ID.
    /// </summary>
    public int VendorId { get; set; }

    /// <summary>
    /// USB Product ID.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Windows device instance path (used by HidHide for hiding/unhiding).
    /// Example: HID\VID_045E&PID_028E\7&12345678&0&0000
    /// </summary>
    public string InstancePath { get; set; } = string.Empty;

    /// <summary>
    /// Whether this controller is currently assigned to an active virtual slot.
    /// </summary>
    public bool IsAssigned { get; set; }

    /// <summary>
    /// The virtual slot index (0-3) this controller is assigned to, or -1 if unassigned.
    /// </summary>
    public int AssignedSlotIndex { get; set; } = -1;

    /// <summary>
    /// Friendly label (can be set by user, e.g., player name).
    /// </summary>
    public string Label { get; set; } = string.Empty;

    public override string ToString()
    {
        var label = string.IsNullOrEmpty(Label) ? Name : $"{Name} ({Label})";
        return IsAssigned ? $"{label} → Slot {AssignedSlotIndex + 1}" : $"{label} [Unassigned]";
    }
}
