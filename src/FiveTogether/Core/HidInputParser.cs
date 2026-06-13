using HidSharp;
using HidSharp.Reports;
using HidSharp.Reports.Input;
using FiveTogether.Models;

namespace FiveTogether.Core;

/// <summary>
/// Parses raw HID input reports from Xbox-layout game controllers.
/// 
/// Uses HidSharp's HidDeviceInputReceiver and DeviceItemInputParser
/// for efficient async report reading and descriptor-based parsing.
/// Handles different controller manufacturers (Xbox, CosmicByte, Readgear, Parsec virtual)
/// without hardcoding byte offsets.
/// </summary>
public class HidInputParser : IDisposable
{
    private readonly HidDevice _device;
    private HidStream? _stream;
    private HidDeviceInputReceiver? _inputReceiver;
    private DeviceItemInputParser? _inputParser;
    private byte[]? _reportBuffer;
    private bool _disposed;

    // Cached usage IDs for Xbox-style controls (HID Usage Tables)
    // Generic Desktop Page (0x01)
    private const uint UsageX = 0x00010030;          // Left stick X
    private const uint UsageY = 0x00010031;          // Left stick Y
    private const uint UsageZ = 0x00010032;          // Left trigger (or right stick X)
    private const uint UsageRx = 0x00010033;         // Right stick X
    private const uint UsageRy = 0x00010034;         // Right stick Y
    private const uint UsageRz = 0x00010035;         // Right trigger (or left trigger)
    private const uint UsageHatSwitch = 0x00010039;  // D-Pad

    // Button Page (0x09) — buttons 1-15
    private const uint UsageButton1 = 0x00090001;
    private const uint UsageButton2 = 0x00090002;
    private const uint UsageButton3 = 0x00090003;
    private const uint UsageButton4 = 0x00090004;
    private const uint UsageButton5 = 0x00090005;
    private const uint UsageButton6 = 0x00090006;
    private const uint UsageButton7 = 0x00090007;
    private const uint UsageButton8 = 0x00090008;
    private const uint UsageButton9 = 0x00090009;
    private const uint UsageButton10 = 0x0009000A;
    private const uint UsageButton11 = 0x0009000B;

    public HidInputParser(HidDevice device)
    {
        _device = device;
    }

    /// <summary>
    /// Opens the HID device and starts the async input receiver.
    /// </summary>
    public bool Open()
    {
        try
        {
            if (!_device.TryOpen(out var stream))
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Cannot open HID device: {_device.DevicePath}");
                return false;
            }

            _stream = stream;
            _reportBuffer = new byte[_device.GetMaxInputReportLength()];

            var reportDescriptor = _device.GetReportDescriptor();
            var deviceItem = reportDescriptor.DeviceItems.FirstOrDefault();
            if (deviceItem == null)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"No device items in report descriptor: {_device.DevicePath}");
                return false;
            }

            _inputParser = deviceItem.CreateDeviceItemInputParser();
            _inputReceiver = reportDescriptor.CreateHidDeviceInputReceiver();
            _inputReceiver.Start(_stream);

            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"HidInputParser open error for {_device.DevicePath}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Whether the input receiver is still running (device is connected and readable).
    /// </summary>
    public bool IsRunning => _inputReceiver?.IsRunning ?? false;

    /// <summary>
    /// Reads the latest input report and parses it into a ControllerInput struct.
    /// Returns true if a new report was successfully read and parsed.
    /// Non-blocking: returns false immediately if no new data is available.
    /// </summary>
    public bool TryReadInput(out ControllerInput input)
    {
        input = default;

        if (_inputReceiver == null || _inputParser == null || _reportBuffer == null)
            return false;

        try
        {
            // Wait briefly for new data (4ms = ~250Hz polling)
            if (!_inputReceiver.WaitHandle.WaitOne(4))
                return false;

            // Read and parse all available reports, keeping only the latest
            bool parsed = false;
            while (_inputReceiver.TryRead(_reportBuffer, 0, out var report))
            {
                if (_inputParser.TryParseReport(_reportBuffer, 0, report))
                {
                    parsed = true;
                }
            }

            if (parsed)
            {
                input = ExtractInput();
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(
                $"HidInputParser read error: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Extracts parsed control values from the input parser into our ControllerInput struct.
    /// Maps HID usages to Xbox 360 controls based on standard usage definitions.
    /// </summary>
    private ControllerInput ExtractInput()
    {
        var input = new ControllerInput();

        if (_inputParser == null)
            return input;

        // Walk all data items in the parsed report
        for (int i = 0; i < _inputParser.ValueCount; i++)
        {
            var dataValue = _inputParser.GetValue(i);

            foreach (var usage in dataValue.Usages)
            {
                var value = dataValue.GetLogicalValue();

                switch (usage)
                {
                    // Analog sticks
                    case UsageX:
                        input.LeftThumbX = ScaleAxisToShort(value, dataValue);
                        break;
                    case UsageY:
                        // Y axis is inverted in HID (up = lower value)
                        input.LeftThumbY = (short)-ScaleAxisToShort(value, dataValue);
                        break;
                    case UsageRx:
                        input.RightThumbX = ScaleAxisToShort(value, dataValue);
                        break;
                    case UsageRy:
                        input.RightThumbY = (short)-ScaleAxisToShort(value, dataValue);
                        break;

                    // Triggers — mapped as Z/Rz axes
                    case UsageZ:
                        input.LeftTrigger = ScaleAxisToByte(value, dataValue);
                        break;
                    case UsageRz:
                        input.RightTrigger = ScaleAxisToByte(value, dataValue);
                        break;

                    // D-Pad (Hat Switch)
                    case UsageHatSwitch:
                        ParseHatSwitch(value, ref input);
                        break;

                    // Buttons — Xbox layout mapping
                    // Standard: A=1, B=2, X=3, Y=4, LB=5, RB=6, Back=7, Start=8, L3=9, R3=10, Guide=11
                    case UsageButton1: input.A = value != 0; break;
                    case UsageButton2: input.B = value != 0; break;
                    case UsageButton3: input.X = value != 0; break;
                    case UsageButton4: input.Y = value != 0; break;
                    case UsageButton5: input.LeftShoulder = value != 0; break;
                    case UsageButton6: input.RightShoulder = value != 0; break;
                    case UsageButton7: input.Back = value != 0; break;
                    case UsageButton8: input.Start = value != 0; break;
                    case UsageButton9: input.LeftThumb = value != 0; break;
                    case UsageButton10: input.RightThumb = value != 0; break;
                    case UsageButton11: input.Guide = value != 0; break;
                }
            }
        }

        return input;
    }

    /// <summary>
    /// Scales a HID axis value to Xbox 360's short range (-32768 to 32767).
    /// </summary>
    private static short ScaleAxisToShort(int value, DataValue dataValue)
    {
        var logicalMin = dataValue.DataItem.LogicalMinimum;
        var logicalMax = dataValue.DataItem.LogicalMaximum;

        if (logicalMax == logicalMin)
            return 0;

        // Normalize to 0.0..1.0, then scale to -32768..32767
        double normalized = (double)(value - logicalMin) / (logicalMax - logicalMin);
        return (short)(normalized * 65535 - 32768);
    }

    /// <summary>
    /// Scales a HID axis value to byte range (0-255) for triggers.
    /// </summary>
    private static byte ScaleAxisToByte(int value, DataValue dataValue)
    {
        var logicalMin = dataValue.DataItem.LogicalMinimum;
        var logicalMax = dataValue.DataItem.LogicalMaximum;

        if (logicalMax == logicalMin)
            return 0;

        double normalized = (double)(value - logicalMin) / (logicalMax - logicalMin);
        return (byte)(normalized * 255);
    }

    /// <summary>
    /// Parses the Hat Switch (D-Pad) value into individual directional booleans.
    /// Standard 8-direction hat: 0=N, 1=NE, 2=E, 3=SE, 4=S, 5=SW, 6=W, 7=NW, 8+=neutral
    /// </summary>
    private static void ParseHatSwitch(int value, ref ControllerInput input)
    {
        input.DPadUp = value == 0 || value == 1 || value == 7;
        input.DPadRight = value == 1 || value == 2 || value == 3;
        input.DPadDown = value == 3 || value == 4 || value == 5;
        input.DPadLeft = value == 5 || value == 6 || value == 7;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _stream?.Dispose();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    ~HidInputParser()
    {
        Dispose();
    }
}
