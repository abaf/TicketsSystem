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

    public static class ConsoleHelper
    {
        [DllImport("kernel32")]
        public static extern bool SetConsoleIcon(IntPtr hIcon);

        [DllImport("kernel32")]
        private extern static bool SetConsoleFont(IntPtr hOutput, uint index);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetCurrentConsoleFontEx(
            IntPtr ConsoleOutput,
            bool MaximumWindow,
            CONSOLE_FONT_INFO_EX ConsoleCurrentFontEx
            );

        public enum StdHandle
        {
            OutputHandle = -11
        }

        [DllImport("kernel32")]
        public static extern IntPtr GetStdHandle(StdHandle index);

        public static bool SetConsoleFont(uint index)
        {
            return SetConsoleFont(GetStdHandle(StdHandle.OutputHandle), index);
        }

        [DllImport("kernel32")]
        private static extern bool GetConsoleFontInfo(IntPtr hOutput, [MarshalAs(UnmanagedType.Bool)]bool bMaximize,
            uint count, [MarshalAs(UnmanagedType.LPArray), Out] ConsoleFont[] fonts);

        [DllImport("kernel32")]
        private static extern uint GetNumberOfConsoleFonts();

        public static uint ConsoleFontsCount
        {
            get
            {
                return GetNumberOfConsoleFonts();
            }
        }

        public static ConsoleFont[] ConsoleFonts
        {
            get
            {
                ConsoleFont[] fonts = new ConsoleFont[GetNumberOfConsoleFonts()];
                if (fonts.Length > 0)
                    GetConsoleFontInfo(GetStdHandle(StdHandle.OutputHandle), false, (uint)fonts.Length, fonts);
                return fonts;
            }
        }

        public static void DisableCloseMenu()
        {
            IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;
            IntPtr hSystemMenu = GetSystemMenu(hMenu, false);
            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_BYCOMMAND);
        }


        [DllImport("user32.dll")]
        public static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        public static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);

        public const uint SC_CLOSE = 0xF060;
        public const uint MF_GRAYED = 0x00000001;
        public const uint MF_BYCOMMAND = 0x00000000;

    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ConsoleFont
    {
        public uint Index;
        public short SizeX, SizeY;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct CONSOLE_FONT_INFO_EX
    {
        public uint cbSize;
        public uint nFont;
        public short SizeX, SizeY;
        public FontFamilies FontFamily;
        public uint FontWeight;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string FaceName;
    }

    /* Font Families */
    public enum FontFamilies : uint
    {
        FF_DONTCARE = (0 << 4)  /* Don't care or don't know. */
        ,
        FF_ROMAN = (1 << 4)  /* Variable stroke width, serifed. */
                             /* Times Roman, Century Schoolbook, etc. */
            ,
        FF_SWISS = (2 << 4)  /* Variable stroke width, sans-serifed. */
                             /* Helvetica, Swiss, etc. */
            ,
        FF_MODERN = (3 << 4)  /* Constant stroke width, serifed or sans-serifed. */
                              /* Pica, Elite, Courier, etc. */
            ,
        FF_SCRIPT = (4 << 4)  /* Cursive, etc. */
            , FF_DECORATIVE = (5 << 4)  /* Old English, etc. */
    }
}
