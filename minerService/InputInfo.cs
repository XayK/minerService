using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace minerService
{
    public class GetLastUserInput
    {
        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }
        private static LASTINPUTINFO lastInPutNfo;
        static GetLastUserInput()
        {
            lastInPutNfo = new LASTINPUTINFO();
            lastInPutNfo.cbSize = (uint)Marshal.SizeOf(lastInPutNfo);
        }
        [DllImport("User32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        public static uint GetIdleTickCount()
        {
            return ((uint)Environment.TickCount - GetLastInputTime());
        }

        public static uint GetLastInputTime()
        {
            if (!GetLastInputInfo(ref lastInPutNfo))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            return lastInPutNfo.dwTime;
        }
    }
}