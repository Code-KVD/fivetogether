using Nefarius.Drivers.HidHide;

namespace FiveTogether.Core;

/// <summary>
/// Manages the HidHide driver to hide physical/Parsec game controllers from other applications
/// and whitelist our app so we can still read from them.
/// 
/// Uses the Nefarius.Drivers.HidHide NuGet package for clean API access.
/// Requires HidHide driver to be installed: https://github.com/nefarius/HidHide
/// </summary>
public class HidHideManager : IDisposable
{
    private HidHideControlService? _service;
    private readonly List<string> _hiddenDevices = new();
    private string? _appPath;
    private bool _disposed;

    /// <summary>
    /// Checks if the HidHide driver is installed and accessible.
    /// </summary>
    public static bool IsDriverInstalled()
    {
        try
        {
            var service = new HidHideControlService();
            return service.IsInstalled;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Initializes the HidHide control service.
    /// </summary>
    public bool Connect()
    {
        try
        {
            _service = new HidHideControlService();

            if (!_service.IsInstalled)
            {
                System.Diagnostics.Debug.WriteLine("HidHide driver is not installed.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HidHide connect error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a device instance path to the HidHide blocked list (hides it from non-whitelisted apps).
    /// </summary>
    public bool HideDevice(string instancePath)
    {
        if (_service == null)
            return false;

        try
        {
            _service.AddBlockedInstanceId(instancePath);

            if (!_hiddenDevices.Contains(instancePath))
                _hiddenDevices.Add(instancePath);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HidHide hide error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes a device from the HidHide blocked list (makes it visible again).
    /// </summary>
    public bool UnhideDevice(string instancePath)
    {
        if (_service == null)
            return false;

        try
        {
            _service.RemoveBlockedInstanceId(instancePath);
            _hiddenDevices.Remove(instancePath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HidHide unhide error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds our application to the HidHide whitelist so we can still read hidden devices.
    /// </summary>
    public bool WhitelistCurrentApp()
    {
        if (_service == null)
            return false;

        try
        {
            _appPath = Environment.ProcessPath ??
                System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;

            if (string.IsNullOrEmpty(_appPath))
                return false;

            _service.AddApplicationPath(_appPath);
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HidHide whitelist error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Enables or disables the HidHide filtering globally.
    /// When active, blocked devices are hidden from non-whitelisted apps.
    /// </summary>
    public bool SetActive(bool active)
    {
        if (_service == null)
            return false;

        try
        {
            _service.IsActive = active;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"HidHide set active error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Unhides all devices that were hidden during this session and disables filtering.
    /// Called on cleanup/exit to restore the system to its original state.
    /// </summary>
    public void RestoreAll()
    {
        if (_service == null) return;

        // Unhide all devices we hid
        foreach (var devicePath in _hiddenDevices.ToList())
        {
            try { _service.RemoveBlockedInstanceId(devicePath); }
            catch { /* Best effort cleanup */ }
        }
        _hiddenDevices.Clear();

        // Remove our app from the whitelist
        if (!string.IsNullOrEmpty(_appPath))
        {
            try { _service.RemoveApplicationPath(_appPath); }
            catch { /* Best effort cleanup */ }
        }

        // Disable filtering
        try { _service.IsActive = false; }
        catch { /* Best effort cleanup */ }
    }

    /// <summary>
    /// Gets the list of devices currently hidden by this session.
    /// </summary>
    public IReadOnlyList<string> GetHiddenDevices() => _hiddenDevices.AsReadOnly();

    public void Dispose()
    {
        if (!_disposed)
        {
            RestoreAll();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~HidHideManager()
    {
        Dispose();
    }
}
