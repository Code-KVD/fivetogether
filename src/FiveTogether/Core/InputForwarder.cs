using HidSharp;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Manages input forwarding from a physical HID controller to a ViGEmBus virtual slot.
/// Runs a dedicated background thread per slot, reading HID reports and forwarding to ViGEm.
/// </summary>
public class InputForwarder : IDisposable
{
    private readonly ViGEmManager _vigemManager;
    private readonly int _slotIndex;
    private HidInputParser? _parser;
    private Thread? _forwardThread;
    private volatile bool _running;
    private bool _disposed;

    /// <summary>
    /// Fires when the forwarding thread encounters an error or the device disconnects.
    /// </summary>
    public event Action<int, string>? OnError;

    /// <summary>
    /// Fires when input activity is detected on this slot (for UI indicators).
    /// </summary>
    public event Action<int, ControllerInput>? OnInputReceived;

    public int SlotIndex => _slotIndex;
    public bool IsRunning => _running;

    public InputForwarder(ViGEmManager vigemManager, int slotIndex)
    {
        _vigemManager = vigemManager;
        _slotIndex = slotIndex;
    }

    /// <summary>
    /// Starts forwarding input from the specified HID device to the virtual slot.
    /// Opens the HID device, creates a parser, and launches the forwarding thread.
    /// </summary>
    public bool Start(HidDevice device)
    {
        if (_running)
            Stop();

        try
        {
            _parser = new HidInputParser(device);
            if (!_parser.Open())
            {
                OnError?.Invoke(_slotIndex, $"Cannot open device: {device.GetProductName()}");
                return false;
            }

            _running = true;
            _forwardThread = new Thread(ForwardLoop)
            {
                Name = $"FiveTogether_Slot{_slotIndex}_Forward",
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal,
            };
            _forwardThread.Start();

            return true;
        }
        catch (Exception ex)
        {
            OnError?.Invoke(_slotIndex, $"Start error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Stops the forwarding thread and releases the HID device.
    /// </summary>
    public void Stop()
    {
        _running = false;

        // Wait for thread to finish (max 500ms)
        if (_forwardThread?.IsAlive == true)
        {
            _forwardThread.Join(500);
        }
        _forwardThread = null;

        // Release the HID parser/stream
        _parser?.Dispose();
        _parser = null;

        // Send a neutral (all-zero) report to the virtual slot so no buttons are stuck
        _vigemManager.SubmitInput(_slotIndex, default);
    }

    /// <summary>
    /// Main forwarding loop — runs on a dedicated background thread.
    /// Reads HID reports and forwards them to the ViGEm virtual controller.
    /// </summary>
    private void ForwardLoop()
    {
        try
        {
            while (_running)
            {
                if (_parser == null || !_parser.IsRunning)
                {
                    OnError?.Invoke(_slotIndex, "Device disconnected");
                    break;
                }

                if (_parser.TryReadInput(out var input))
                {
                    // Forward to the virtual controller
                    _vigemManager.SubmitInput(_slotIndex, input);

                    // Notify UI (throttled — only if there's activity)
                    if (input.HasActivity())
                    {
                        OnInputReceived?.Invoke(_slotIndex, input);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            if (_running)
            {
                OnError?.Invoke(_slotIndex, $"Forwarding error: {ex.Message}");
            }
        }
        finally
        {
            _running = false;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            Stop();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~InputForwarder()
    {
        Dispose();
    }
}
