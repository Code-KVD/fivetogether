namespace FiveTogether.Models;

/// <summary>
/// Represents one of the 4 virtual Xbox 360 controller slots created via ViGEmBus.
/// </summary>
public class VirtualSlot
{
    /// <summary>
    /// Slot index (0-3).
    /// </summary>
    public int Index { get; set; }

    /// <summary>
    /// Display name (e.g., "Slot 1").
    /// </summary>
    public string DisplayName => $"Slot {Index + 1}";

    /// <summary>
    /// The physical controller currently assigned to this slot, or null if empty.
    /// </summary>
    public PhysicalController? AssignedController { get; set; }

    /// <summary>
    /// Whether the virtual controller is connected and active.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Whether input forwarding is currently running for this slot.
    /// </summary>
    public bool IsForwarding { get; set; }

    public override string ToString()
    {
        if (!IsActive) return $"{DisplayName}: Inactive";
        if (AssignedController == null) return $"{DisplayName}: Empty";
        return $"{DisplayName}: {AssignedController.Name}";
    }
}
