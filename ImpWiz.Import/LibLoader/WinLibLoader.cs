using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace ImpWiz.Import.LibLoader
{
    /// <summary>
    /// A <see cref="ILibLoader"/> implementation for Windows.
    /// </summary>
    public sealed class WinLibLoader : ILibLoader
    {
        private const string LibraryName = "kernel32.dll";
        [DllImport(LibraryName, EntryPoint = "LoadLibrary")]
        private static extern IntPtr _LoadLibrary(string name);
        [DllImport(LibraryName, EntryPoint = "GetProcAddress")]
        private static extern IntPtr _GetProcAddress(IntPtr handle, string name);
        [DllImport(LibraryName, EntryPoint = "FreeLibrary")]
        private static extern bool _FreeLibrary(IntPtr handle);

        /// <inheritdoc />
        public IntPtr LoadLibrary(string name) => _LoadLibrary(name);

        /// <inheritdoc />
        public IntPtr GetProcAddress(IntPtr handle, string name) => _GetProcAddress(handle, name);

        /// <inheritdoc />
        public bool FreeLibrary(IntPtr handle) => _FreeLibrary(handle);

        /// <inheritdoc />
        public string GetError() => GetErrorMessage(Marshal.GetLastWin32Error());

        // GetErrorMessage from Win32Exception.cs
        [DllImport(LibraryName, CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = true)]
        private static extern int FormatMessage(int dwFlags, IntPtr lpSource, uint dwMessageId, int dwLanguageId, StringBuilder lpBuffer, int nSize, IntPtr[] arguments);
        
        private const int FORMAT_MESSAGE_ALLOCATE_BUFFER = 0x00000100;
        private const int FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200;
        private const int FORMAT_MESSAGE_FROM_STRING     = 0x00000400;
        private const int FORMAT_MESSAGE_FROM_HMODULE    = 0x00000800;
        private const int FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000;
        private const int FORMAT_MESSAGE_ARGUMENT_ARRAY  = 0x00002000;
        private const int FORMAT_MESSAGE_MAX_WIDTH_MASK  = 0x000000FF;
        
        private const int ERROR_INSUFFICIENT_BUFFER      = 122;
        private static bool TryGetErrorMessage(int error, StringBuilder sb, out string errorMsg)
        {
            errorMsg = "";
            int result = FormatMessage(
                FORMAT_MESSAGE_IGNORE_INSERTS |
                FORMAT_MESSAGE_FROM_SYSTEM |
                FORMAT_MESSAGE_ARGUMENT_ARRAY,
                IntPtr.Zero, (uint) error, 0, sb, sb.Capacity + 1,
                null);
            if (result != 0) {
                int i = sb.Length;
                while (i > 0) {
                    char ch = sb[i - 1];
                    if (ch > 32 && ch != '.') break;
                    i--;
                }
                errorMsg = sb.ToString(0, i);
            }
            else if (Marshal.GetLastWin32Error() == ERROR_INSUFFICIENT_BUFFER) {
                return false;
            }
            else {
                errorMsg ="Unknown error (0x" + Convert.ToString(error, 16) + ")";
            }
 
            return true;
        }
        
        private const int MaxAllowedBufferSize = 65 * 1024;
 
        private static string GetErrorMessage(int error) {
            string errorMsg;
 
            StringBuilder sb = new StringBuilder(256);
            do {
                if (TryGetErrorMessage(error, sb, out errorMsg))
                    return errorMsg;
                else {
                    // increase the capacity of the StringBuilder by 4 times.
                    sb.Capacity *= 4;
                }
            }
            while (sb.Capacity < MaxAllowedBufferSize);
 
            // If you come here then a size as large as 65K is also not sufficient and so we give the generic errorMsg.
            return "Unknown error (0x" + Convert.ToString(error, 16) + ")";
        }

        /// <inheritdoc />
        public void Prepare()
        {
            Marshal.Prelink(typeof(WinLibLoader).GetMethod(nameof(_LoadLibrary), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(WinLibLoader).GetMethod(nameof(_GetProcAddress), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(WinLibLoader).GetMethod(nameof(_FreeLibrary), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(WinLibLoader).GetMethod(nameof(FormatMessage), BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}