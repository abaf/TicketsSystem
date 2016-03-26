using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TicketSystem.Utils
{
    public class WindowsConsoleHelper
    {
        [DllImport("user32.dll")]
        static extern bool LockWindowUpdate(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        #region WINDOWPLACEMENT

        [StructLayout(LayoutKind.Sequential)]
        struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        enum ShowWindowCommands
        {
            Hide = 0,
            Normal = 1,
            ShowMinimized = 2,
            Maximize = 3,
            ShowMaximized = 3,
            ShowNoActivate = 4,
            Show = 5,
            Minimize = 6,
            ShowMinNoActive = 7,
            ShowNA = 8,
            Restore = 9,
            ShowDefault = 10,
            ForceMinimize = 11
        }

        /// <summary>
        /// Contains information about the placement of a window on the screen.
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        struct WINDOWPLACEMENT
        {
            public int Length;
            public int Flags;
            public ShowWindowCommands ShowCmd;
            public POINT MinPosition;
            public POINT MaxPosition;
            public RECT NormalPosition;
        }

        #endregion

        static ShowWindowCommands WindowState = ShowWindowCommands.Normal;
        static IntPtr ConsoleHandle = IntPtr.Zero; 

        [MethodImpl(MethodImplOptions.NoInlining)]
        static void GetConsoleWindowState()
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.Length = Marshal.SizeOf(placement);
            GetWindowPlacement(ConsoleHandle, ref placement);
            WindowState = placement.ShowCmd;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Write(string line)
        {
            if (ConsoleHandle == IntPtr.Zero)
                ConsoleHandle = Process.GetCurrentProcess().MainWindowHandle;

            if (line.Length < 4 * 1024)
                Console.Write(line);

            else
            {
                WindowsConsoleHelper.GetConsoleWindowState();

                if (WindowState != ShowWindowCommands.ShowMinimized)
                    LockWindowUpdate(ConsoleHandle);

                Console.Write(line);

                if (WindowState != ShowWindowCommands.ShowMinimized)
                    LockWindowUpdate(IntPtr.Zero);
            }
        }
    }
}
