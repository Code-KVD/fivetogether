using HidSharp;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Detects all HID game controllers (gamepads/joysticks) connected to the system.
/// Uses HidSharp to enumerate HID devices and filters by usage page/usage for game controllers.
/// </summary>
public class ControllerDetector
{
    // HID Usage Page: Generic Desktop Controls
    private const int UsagePageGenericDesktop = 0x0001;

    // HID Usages for game controllers
    private const int UsageGamePad = 0x0005;
    private const int UsageJoystick = 0x0004;

    /// <summary>
    /// Enumerates all connected HID game controllers.
    /// Returns both real (USB/Bluetooth) and virtual (Parsec) controllers.
    /// </summary>
    public List<PhysicalController> DetectControllers()
    {
        var controllers = new List<PhysicalController>();

        try
        {
            var devices = DeviceList.Local.GetHidDevices();

            foreach (var device in devices)
            {
                try
                {
                    // Filter for game controllers by HID usage
                    if (!IsGameController(device))
                        continue;

                    var controller = new PhysicalController
                    {
                        DevicePath = device.DevicePath,
                        VendorId = device.VendorID,
                        ProductId = device.ProductID,
                        Name = GetFriendlyName(device),
                        InstancePath = ExtractInstancePath(device.DevicePath),
                    };

                    controllers.Add(controller);
                }
                catch (Exception ex)
                {
                    // Skip devices we can't query — might be locked by another driver
                    System.Diagnostics.Debug.WriteLine(
                        $"Skipping device {device.DevicePath}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"Controller detection failed: {ex.Message}");
        }

        // Deduplicate: one physical device can expose multiple HID collections
        // (e.g. Col01/Col02 on Xbox controllers). Keep the first entry per InstancePath.
        return controllers
            .GroupBy(c => string.IsNullOrEmpty(c.InstancePath) ? c.DevicePath : c.InstancePath,
                     StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();
    }

    /// <summary>
    /// Checks if a HID device is a game controller (gamepad or joystick).
    /// </summary>
    private static bool IsGameController(HidDevice device)
    {
        try
        {
            var reportDescriptor = device.GetReportDescriptor();

            foreach (var deviceItem in reportDescriptor.DeviceItems)
            {
                foreach (var usage in deviceItem.Usages.GetAllValues())
                {
                    uint usagePage = (usage >> 16) & 0xFFFF;
                    uint usageId = usage & 0xFFFF;

                    if (usagePage == UsagePageGenericDesktop &&
                        (usageId == UsageGamePad || usageId == UsageJoystick))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Can't read report descriptor — not a game controller we can use
        }

        return false;
    }

    /// <summary>
    /// Gets a human-readable name for the device.
    /// Tries product name first, falls back to manufacturer + VID/PID.
    /// </summary>
    private static string GetFriendlyName(HidDevice device)
    {
        try
        {
            var name = device.GetProductName();
            if (!string.IsNullOrWhiteSpace(name))
                return name.Trim();
        }
        catch { }

        try
        {
            var manufacturer = device.GetManufacturer();
            if (!string.IsNullOrWhiteSpace(manufacturer))
                return $"{manufacturer.Trim()} Controller";
        }
        catch { }

        return $"Controller (VID:{device.VendorID:X4} PID:{device.ProductID:X4})";
    }

    /// <summary>
    /// Extracts the device instance path from the full HID device path.
    /// The instance path is needed by HidHide to hide specific devices.
    /// 
    /// Input:  \\?\HID#VID_045E&PID_028E#7&12345678&0&0000#{4d1e55b2-f16f-11cf-88cb-001111000030}
    /// Output: HID\VID_045E&PID_028E\7&12345678&0&0000
    /// </summary>
    private static string ExtractInstancePath(string devicePath)
    {
        if (string.IsNullOrEmpty(devicePath))
            return string.Empty;

        // Remove \\?\ prefix
        var path = devicePath;
        if (path.StartsWith(@"\\?\") || path.StartsWith(@"\\.\"))
            path = path[4..];

        // Remove GUID suffix (everything after the last #{ )
        var guidIndex = path.IndexOf("#{", StringComparison.Ordinal);
        if (guidIndex >= 0)
            path = path[..guidIndex];

        // Replace # with \ to get standard instance path format
        path = path.Replace('#', '\\');

        return path;
    }
}
