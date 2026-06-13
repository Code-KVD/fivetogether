using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Manages ViGEmBus virtual Xbox 360 controllers.
/// Creates up to 4 virtual controllers that Cricket 19 sees as real Xbox gamepads.
/// </summary>
public class ViGEmManager : IDisposable
{
    private ViGEmClient? _client;
    private readonly IXbox360Controller?[] _virtualControllers = new IXbox360Controller?[4];
    private bool _disposed;

    /// <summary>
    /// Checks if the ViGEmBus driver is installed by attempting to create a client.
    /// </summary>
    public static bool IsDriverInstalled()
    {
        try
        {
            using var client = new ViGEmClient();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes the ViGEm client connection.
    /// </summary>
    public bool Connect()
    {
        try
        {
            _client = new ViGEmClient();
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ViGEm connect error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates and connects a virtual Xbox 360 controller at the specified slot index (0-3).
    /// </summary>
    public bool CreateSlot(int slotIndex)
    {
        if (_client == null || slotIndex < 0 || slotIndex > 3)
            return false;

        try
        {
            // Disconnect existing controller at this slot if any
            DestroySlot(slotIndex);

            var controller = _client.CreateXbox360Controller();
            controller.Connect();

            _virtualControllers[slotIndex] = controller;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ViGEm create slot {slotIndex} error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Creates all 4 virtual controller slots.
    /// </summary>
    public bool CreateAllSlots()
    {
        bool allOk = true;
        for (int i = 0; i < 4; i++)
        {
            if (!CreateSlot(i))
                allOk = false;
        }
        return allOk;
    }

    /// <summary>
    /// Disconnects and removes a virtual controller at the specified slot index.
    /// </summary>
    public void DestroySlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > 3)
            return;

        try
        {
            _virtualControllers[slotIndex]?.Disconnect();
        }
        catch { }

        _virtualControllers[slotIndex] = null;
    }

    /// <summary>
    /// Destroys all virtual controller slots.
    /// </summary>
    public void DestroyAllSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            DestroySlot(i);
        }
    }

    /// <summary>
    /// Sends a full input report to a virtual controller slot.
    /// This is called at high frequency by the InputForwarder to pass through real input.
    /// </summary>
    public void SubmitInput(int slotIndex, ControllerInput input)
    {
        if (slotIndex < 0 || slotIndex > 3)
            return;

        var controller = _virtualControllers[slotIndex];
        if (controller == null)
            return;

        try
        {
            // Set buttons
            controller.SetButtonState(Xbox360Button.A, input.A);
            controller.SetButtonState(Xbox360Button.B, input.B);
            controller.SetButtonState(Xbox360Button.X, input.X);
            controller.SetButtonState(Xbox360Button.Y, input.Y);

            controller.SetButtonState(Xbox360Button.Up, input.DPadUp);
            controller.SetButtonState(Xbox360Button.Down, input.DPadDown);
            controller.SetButtonState(Xbox360Button.Left, input.DPadLeft);
            controller.SetButtonState(Xbox360Button.Right, input.DPadRight);

            controller.SetButtonState(Xbox360Button.Start, input.Start);
            controller.SetButtonState(Xbox360Button.Back, input.Back);
            controller.SetButtonState(Xbox360Button.Guide, input.Guide);

            controller.SetButtonState(Xbox360Button.LeftThumb, input.LeftThumb);
            controller.SetButtonState(Xbox360Button.RightThumb, input.RightThumb);
            controller.SetButtonState(Xbox360Button.LeftShoulder, input.LeftShoulder);
            controller.SetButtonState(Xbox360Button.RightShoulder, input.RightShoulder);

            // Set axes
            controller.SetAxisValue(Xbox360Axis.LeftThumbX, input.LeftThumbX);
            controller.SetAxisValue(Xbox360Axis.LeftThumbY, input.LeftThumbY);
            controller.SetAxisValue(Xbox360Axis.RightThumbX, input.RightThumbX);
            controller.SetAxisValue(Xbox360Axis.RightThumbY, input.RightThumbY);

            // Set triggers
            controller.SetSliderValue(Xbox360Slider.LeftTrigger, input.LeftTrigger);
            controller.SetSliderValue(Xbox360Slider.RightTrigger, input.RightTrigger);

            controller.SubmitReport();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"ViGEm submit slot {slotIndex} error: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns whether a specific slot has an active virtual controller.
    /// </summary>
    public bool IsSlotActive(int slotIndex)
    {
        return slotIndex >= 0 && slotIndex <= 3 && _virtualControllers[slotIndex] != null;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            DestroyAllSlots();
            _client?.Dispose();
            _client = null;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~ViGEmManager()
    {
        Dispose();
    }
}
