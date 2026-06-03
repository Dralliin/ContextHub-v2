#pragma warning disable CS0101
using System;
using System.IO;
using System.Drawing;
using System.Runtime.InteropServices;

namespace ContextHubDev
{
    public static class SystemStuff
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        public const uint SHGFI_ICON = 0x100;
        public const uint SHGFI_LARGEICON = 0x0;
        public const uint SHGFI_USEFILEATTRIBUTES = 0x10;

        [DllImport("shell32.dll")]
        public static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

        [DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        public static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        public static Icon GetSystemIcon(string path)
        {
            SHFILEINFO shinfo = new SHFILEINFO();
            uint flags = SHGFI_ICON | SHGFI_LARGEICON;
            if (!File.Exists(path) && !Directory.Exists(path)) flags |= SHGFI_USEFILEATTRIBUTES;

            SHGetFileInfo(path, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);
            if (shinfo.hIcon != IntPtr.Zero)
            {
                Icon icon = (Icon)Icon.FromHandle(shinfo.hIcon).Clone();
                DestroyIcon(shinfo.hIcon);
                return icon;
            }
            return null;
        }
    }
}