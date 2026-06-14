using HidSharp;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Central manager that orchestrates the entire session lifecycle:
/// - Controller detection
/// - HidHide setup (hide real controllers, whitelist app)
/// - ViGEmBus setup (create virtual slots)
/// - Input forwarding (physical → virtual)
/// - Slot swapping
/// - Cleanup on session end
/// </summary>
public class SlotManager : IDisposable
{
    private readonly ControllerDetector _detector;
    private readonly HidHideManager _hidHide;
    private readonly ViGEmManager _vigem;
    private readonly InputForwarder[] _forwarders = new InputForwarder[4];
    private readonly VirtualSlot[] _slots = new VirtualSlot[4];

    private List<PhysicalController> _detectedControllers = new();
    private bool _sessionActive;
    private bool _disposed;

    /// <summary>
    /// Fires when a slot forwarding error occurs (device disconnected, etc.).
    /// Parameters: slot index, error message.
    /// </summary>
    public event Action<int, string>? OnSlotError;

    /// <summary>
    /// Fires when input activity is detected on a slot (for UI status indicators).
    /// Parameters: slot index, input state.
    /// </summary>
    public event Action<int, ControllerInput>? OnSlotActivity;

    /// <summary>
    /// Fires when the session state changes (started/stopped).
    /// </summary>
    public event Action<bool>? OnSessionStateChanged;

    public bool IsSessionActive => _sessionActive;
    public IReadOnlyList<PhysicalController> DetectedControllers => _detectedControllers.AsReadOnly();
    public IReadOnlyList<VirtualSlot> Slots => Array.AsReadOnly(_slots);

    public SlotManager()
    {
        _detector = new ControllerDetector();
        _hidHide = new HidHideManager();
        _vigem = new ViGEmManager();

        // Initialize slot models
        for (int i = 0; i < 4; i++)
        {
            _slots[i] = new VirtualSlot { Index = i };
        }
    }

    /// <summary>
    /// Checks if required drivers are installed.
    /// Returns a tuple of (hidHideInstalled, vigemInstalled).
    /// </summary>
    public static (bool HidHide, bool ViGEm) CheckDrivers()
    {
        return (HidHideManager.IsDriverInstalled(), ViGEmManager.IsDriverInstalled());
    }

    /// <summary>
    /// Scans for all connected game controllers and returns the list.
    /// Can be called before or during a session.
    /// </summary>
    public List<PhysicalController> RefreshControllers()
    {
        _detectedControllers = _detector.DetectControllers();
        return _detectedControllers;
    }

    /// <summary>
    /// Starts a session: hides all real controllers, creates virtual slots,
    /// and begins input forwarding. When <paramref name="orderedControllers"/> is provided
    /// (from the identify dialog) those controllers fill slots 0-3 in order and the 5th
    /// is left unassigned (bench). Falls back to detection order if null.
    /// </summary>
    public (bool Success, string Message) StartSession(List<PhysicalController>? orderedControllers = null)
    {
        if (_sessionActive)
            return (false, "Session already active.");

        if (_detectedControllers.Count == 0)
            return (false, "No controllers detected. Please refresh first.");

        // Step 1: Connect to HidHide
        if (!_hidHide.Connect())
            return (false, "Failed to connect to HidHide driver. Is it installed?");

        // Step 2: Connect to ViGEmBus
        if (!_vigem.Connect())
        {
            _hidHide.Dispose();
            return (false, "Failed to connect to ViGEmBus driver. Is it installed?");
        }

        // Step 3: Whitelist our app in HidHide
        if (!_hidHide.WhitelistCurrentApp())
        {
            CleanupSession();
            return (false, "Failed to whitelist app in HidHide.");
        }

        // Step 4: Hide all detected game controllers via HidHide
        foreach (var controller in _detectedControllers)
        {
            if (!string.IsNullOrEmpty(controller.InstancePath))
            {
                _hidHide.HideDevice(controller.InstancePath);
            }
        }

        // Step 5: Activate HidHide filtering
        _hidHide.SetActive(true);

        // Step 6: Create 4 virtual Xbox 360 controllers via ViGEmBus
        if (!_vigem.CreateAllSlots())
        {
            CleanupSession();
            return (false, "Failed to create virtual Xbox controllers via ViGEmBus.");
        }

        // Step 7: Assign and start forwarding — use provided order (from identify dialog)
        // or fall back to detection order. Only the first 4 get active slots.
        var source = orderedControllers ?? _detectedControllers;
        var controllersToAssign = source.Take(4).ToList();
        for (int i = 0; i < controllersToAssign.Count; i++)
        {
            var result = AssignControllerToSlot(controllersToAssign[i], i);
            if (!result.Success)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Warning: Failed to assign {controllersToAssign[i].Name} to slot {i}: {result.Message}");
            }
        }

        // Mark remaining controllers as unassigned
        for (int i = 4; i < _detectedControllers.Count; i++)
        {
            _detectedControllers[i].IsAssigned = false;
            _detectedControllers[i].AssignedSlotIndex = -1;
        }

        _sessionActive = true;
        OnSessionStateChanged?.Invoke(true);

        var assignedCount = controllersToAssign.Count;
        var totalCount = _detectedControllers.Count;
        return (true, $"Session started! {assignedCount} controllers active, {totalCount - assignedCount} unassigned.");
    }

    /// <summary>
    /// Stops the session: stops all forwarding, destroys virtual slots, unhides controllers.
    /// </summary>
    public void StopSession()
    {
        if (!_sessionActive)
            return;

        CleanupSession();
        _sessionActive = false;
        OnSessionStateChanged?.Invoke(false);
    }

    /// <summary>
    /// Swaps the controller assigned to a slot with a different controller.
    /// The previously assigned controller becomes unassigned.
    /// This is the core feature — no Parsec disconnect needed.
    /// </summary>
    public (bool Success, string Message) SwapSlot(int slotIndex, PhysicalController newController)
    {
        if (!_sessionActive)
            return (false, "No active session.");

        if (slotIndex < 0 || slotIndex > 3)
            return (false, "Invalid slot index.");

        if (newController.IsAssigned)
            return (false, $"{newController.Name} is already assigned to Slot {newController.AssignedSlotIndex + 1}.");

        var slot = _slots[slotIndex];
        var oldController = slot.AssignedController;

        // Stop forwarding on this slot
        _forwarders[slotIndex]?.Stop();

        // Unassign the old controller
        if (oldController != null)
        {
            oldController.IsAssigned = false;
            oldController.AssignedSlotIndex = -1;
        }

        // Assign the new controller
        var result = AssignControllerToSlot(newController, slotIndex);
        if (!result.Success)
        {
            // If the new assignment fails, try to restore the old one
            if (oldController != null)
            {
                AssignControllerToSlot(oldController, slotIndex);
            }
            return (false, $"Swap failed: {result.Message}");
        }

        var oldName = oldController?.Name ?? "Empty";
        return (true, $"Slot {slotIndex + 1}: {oldName} → {newController.Name}");
    }

    /// <summary>
    /// Assigns a physical controller to a virtual slot and starts input forwarding.
    /// </summary>
    private (bool Success, string Message) AssignControllerToSlot(PhysicalController controller, int slotIndex)
    {
        try
        {
            // Find the HID device matching this controller's path
            var hidDevice = FindHidDevice(controller.DevicePath);
            if (hidDevice == null)
            {
                return (false, $"HID device not found for {controller.Name}.");
            }

            // Create and start the input forwarder
            var forwarder = new InputForwarder(_vigem, slotIndex);
            forwarder.OnError += (slot, msg) => OnSlotError?.Invoke(slot, msg);
            forwarder.OnInputReceived += (slot, input) => OnSlotActivity?.Invoke(slot, input);

            if (!forwarder.Start(hidDevice))
            {
                forwarder.Dispose();
                return (false, $"Cannot start forwarding for {controller.Name}.");
            }

            // Dispose old forwarder if any
            _forwarders[slotIndex]?.Dispose();
            _forwarders[slotIndex] = forwarder;

            // Update models
            controller.IsAssigned = true;
            controller.AssignedSlotIndex = slotIndex;
            _slots[slotIndex].AssignedController = controller;
            _slots[slotIndex].IsActive = true;
            _slots[slotIndex].IsForwarding = true;

            return (true, "OK");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Finds a HidSharp HidDevice by its device path.
    /// </summary>
    private static HidDevice? FindHidDevice(string devicePath)
    {
        try
        {
            return DeviceList.Local.GetHidDevices()
                .FirstOrDefault(d => d.DevicePath.Equals(devicePath, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Internal cleanup — stops all forwarding, destroys virtual slots, restores HidHide state.
    /// </summary>
    private void CleanupSession()
    {
        // Stop all input forwarders
        for (int i = 0; i < 4; i++)
        {
            _forwarders[i]?.Dispose();
            _forwarders[i] = null!;

            _slots[i].AssignedController = null;
            _slots[i].IsActive = false;
            _slots[i].IsForwarding = false;
        }

        // Reset controller assignment state
        foreach (var controller in _detectedControllers)
        {
            controller.IsAssigned = false;
            controller.AssignedSlotIndex = -1;
        }

        // Destroy virtual controllers
        _vigem.DestroyAllSlots();

        // Restore all hidden devices and disable HidHide filtering
        _hidHide.RestoreAll();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopSession();
            _vigem.Dispose();
            _hidHide.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~SlotManager()
    {
        Dispose();
    }
}
