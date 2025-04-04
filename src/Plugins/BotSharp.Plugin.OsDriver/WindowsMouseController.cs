using SharpHook;
using SharpHook.Native;
using System.Drawing;
using System.Runtime.InteropServices;

namespace BotSharp.Plugin.OsDriver;

public class WindowsMouseController
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    // Define MONITORINFO structure
    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    // Delegate for monitor enumeration callback
    private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    // P/Invoke declarations
    [DllImport("user32.dll")]
    private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
       MonitorEnumDelegate lpfnEnum, IntPtr dwData);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // Class to hold monitor information
    public class MonitorInfo
    {
        public IntPtr MonitorHandle;
        public RECT MonitorArea;
    }

    // Method to get all connected monitors
    public static List<MonitorInfo> GetMonitors()
    {
        var monitors = new List<MonitorInfo>();

        bool Callback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                monitors.Add(new MonitorInfo
                {
                    MonitorHandle = hMonitor,
                    MonitorArea = mi.rcMonitor
                });
            }
            return true; // Continue enumeration
        }

        EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
        return monitors;
    }

    public static (int virtualX, int virtualY) GetVirtualCoordinates(int screenIndex, int x, int y)
    {
        // Get all screens
        var monitors = GetMonitors();

        // Validate the screen index
        if (screenIndex < 0 || screenIndex >= monitors.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");
        }

        var monitor = monitors[screenIndex];
        var monitorBounds = new Rectangle(monitor.MonitorArea.Left, monitor.MonitorArea.Top,
            monitor.MonitorArea.Right - monitor.MonitorArea.Left,
            monitor.MonitorArea.Bottom - monitor.MonitorArea.Top);


        // Validate coordinates
        if (x < 0 || x >= monitorBounds.Width || y < 0 || y >= monitorBounds.Height)
        {
            throw new ArgumentOutOfRangeException("Coordinates are out of bounds for the specified monitor.");
        }

        // Convert to virtual screen coordinates
        int virtualX = (int)(monitorBounds.X + x);
        int virtualY = (int)(monitorBounds.Y + y);

        return (virtualX, virtualY);
    }

    // Method to set the cursor position on a specific monitor
    public static void SetCursorPositionOnMonitor(int monitorIndex, int x, int y)
    {
        var (virtualX, virtualY) = GetVirtualCoordinates(monitorIndex, x, y);

        IEventSimulator simulator = new EventSimulator();

        // Move the cursor to the specified position
        simulator.SimulateMouseMovement(Convert.ToInt16(virtualX), Convert.ToInt16(virtualY));
    }

    // Method to perform a left-click at the current cursor position
    public static void LeftClick()
    {
        IEventSimulator simulator = new EventSimulator();
        simulator.SimulateMousePress(MouseButton.Button1);
        simulator.SimulateMouseRelease(MouseButton.Button1);

    }

    public static void RightClick()
    {
        IEventSimulator simulator = new EventSimulator();
        simulator.SimulateMousePress(MouseButton.Button2);
        simulator.SimulateMouseRelease(MouseButton.Button2);

    }


    // Method to move the cursor and perform a left-click at specified coordinates on a specific monitor
    public static void ClickAtPositionOnMonitor(int monitorIndex, int x, int y)
    {
        SetCursorPositionOnMonitor(monitorIndex, x, y);
        System.Threading.Thread.Sleep(50); // Optional delay to ensure the cursor has moved
        LeftClick();
    }


}
