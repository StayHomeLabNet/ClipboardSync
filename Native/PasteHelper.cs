using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

internal static class PasteHelper
{
    private const int INPUT_KEYBOARD = 1;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const uint KEYEVENTF_SCANCODE = 0x0008;

    private const ushort VK_CONTROL = 0x11;
    private const ushort VK_V = 0x56;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [DllImport("user32.dll")]
    private static extern uint MapVirtualKey(uint uCode, uint uMapType);

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT { public int type; public InputUnion U; }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT { public ushort wVk; public ushort wScan; public uint dwFlags; public uint time; public IntPtr dwExtraInfo; }

    private static INPUT ScanDown(ushort vk)
    {
        var scan = (ushort)MapVirtualKey(vk, 0);
        return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = KEYEVENTF_SCANCODE, time = 0, dwExtraInfo = IntPtr.Zero } } };
    }

    private static INPUT ScanUp(ushort vk)
    {
        var scan = (ushort)MapVirtualKey(vk, 0);
        return new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = 0, wScan = scan, dwFlags = KEYEVENTF_SCANCODE | KEYEVENTF_KEYUP, time = 0, dwExtraInfo = IntPtr.Zero } } };
    }

    public static bool CtrlV_FallbackSendKeys()
    {
        try
        {
            var inputs = new INPUT[] { ScanDown(VK_CONTROL), ScanDown(VK_V), ScanUp(VK_V), ScanUp(VK_CONTROL) };
            var sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
            if (sent == inputs.Length) return true;
        }
        catch { }

        try
        {
            SendKeys.SendWait("^v");
            return true;
        }
        catch
        {
            return false;
        }
    }
}