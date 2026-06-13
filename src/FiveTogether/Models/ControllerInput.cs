namespace FiveTogether.Models;

/// <summary>
/// Parsed input state from a physical game controller.
/// Represents a single snapshot of all buttons, sticks, and triggers.
/// Matches Xbox 360 layout for direct forwarding to ViGEmBus.
/// </summary>
public struct ControllerInput
{
    // --- Buttons (bitfield) ---
    public bool DPadUp;
    public bool DPadDown;
    public bool DPadLeft;
    public bool DPadRight;
    public bool Start;
    public bool Back;
    public bool LeftThumb;   // L3
    public bool RightThumb;  // R3
    public bool LeftShoulder;  // LB
    public bool RightShoulder; // RB
    public bool Guide;  // Xbox button
    public bool A;
    public bool B;
    public bool X;
    public bool Y;

    // --- Triggers (0-255) ---
    public byte LeftTrigger;
    public byte RightTrigger;

    // --- Analog sticks (-32768 to 32767) ---
    public short LeftThumbX;
    public short LeftThumbY;
    public short RightThumbX;
    public short RightThumbY;

    /// <summary>
    /// Returns true if any button is pressed or any axis is significantly off-center.
    /// Useful for detecting controller activity.
    /// </summary>
    public readonly bool HasActivity()
    {
        const short deadzone = 4000;
        return DPadUp || DPadDown || DPadLeft || DPadRight ||
               Start || Back || LeftThumb || RightThumb ||
               LeftShoulder || RightShoulder || Guide ||
               A || B || X || Y ||
               LeftTrigger > 20 || RightTrigger > 20 ||
               Math.Abs(LeftThumbX) > deadzone || Math.Abs(LeftThumbY) > deadzone ||
               Math.Abs(RightThumbX) > deadzone || Math.Abs(RightThumbY) > deadzone;
    }
}
