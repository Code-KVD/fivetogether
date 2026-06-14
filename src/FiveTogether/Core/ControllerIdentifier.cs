using HidSharp;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Listens for A-button presses across multiple controllers simultaneously.
/// Used during session setup to identify which physical device each player holds.
/// Each controller's HID stream is kept open and polled in round-robin until
/// A (Button 1) is detected on an unidentified device.
/// </summary>
public class ControllerIdentifier : IDisposable
{
    private readonly List<(PhysicalController Controller, HidInputParser Parser)> _listeners = new();
    private bool _disposed;

    /// <summary>
    /// Opens HID streams for all given controllers and starts listening.
    /// Returns the number of controllers successfully opened.
    /// </summary>
    public int StartListening(List<PhysicalController> controllers)
    {
        int opened = 0;
        foreach (var ctrl in controllers)
        {
            var device = FindHidDevice(ctrl.DevicePath);
            if (device == null) continue;

            var parser = new HidInputParser(device);
            if (parser.Open())
            {
                _listeners.Add((ctrl, parser));
                opened++;
            }
            else
            {
                parser.Dispose();
            }
        }
        return opened;
    }

    /// <summary>
    /// Blocks until A is pressed on any controller whose DevicePath is NOT in
    /// <paramref name="alreadyIdentified"/>. Returns the controller, or null if cancelled.
    /// Safe to call from a background thread.
    /// </summary>
    public PhysicalController? WaitForAPress(HashSet<string> alreadyIdentified, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            foreach (var (ctrl, parser) in _listeners)
            {
                if (alreadyIdentified.Contains(ctrl.DevicePath)) continue;
                if (!parser.IsRunning) continue;

                if (parser.TryReadInput(out var input) && input.A)
                    return ctrl;
            }
        }
        return null;
    }

    private static HidDevice? FindHidDevice(string devicePath)
    {
        try
        {
            return DeviceList.Local.GetHidDevices()
                .FirstOrDefault(d => d.DevicePath.Equals(devicePath, StringComparison.OrdinalIgnoreCase));
        }
        catch { return null; }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var (_, parser) in _listeners)
                parser.Dispose();
            _listeners.Clear();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~ControllerIdentifier() => Dispose();
}
