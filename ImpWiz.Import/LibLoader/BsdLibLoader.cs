using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ImpWiz.Import.LibLoader
{
    /// <summary>
    /// A <see cref="ILibLoader"/> implementation for BSD.
    /// </summary>
    public class BsdLibLoader : ILibLoader
    {
        private const string LibraryName = "c";
        
        [DllImport(LibraryName)]
        private static extern IntPtr dlopen(string name, int flag);
        [DllImport(LibraryName)]
        private static extern IntPtr dlsym(IntPtr handle, string name);
        [DllImport(LibraryName)]
        private static extern int dlclose(IntPtr handle);
        
        [DllImport(LibraryName)]
        private static extern IntPtr dlerror();
        
        private const int RtldNow = 0x2;
        private const int RtldLazy = 0x1;
        private const int RtldGlobal = 8;

        /// <inheritdoc />
        public IntPtr LoadLibrary(string name) => dlopen(name, RtldNow | RtldGlobal);

        /// <inheritdoc />
        public IntPtr GetProcAddress(IntPtr handle, string name) => dlsym(handle, name);

        /// <inheritdoc />
        public bool FreeLibrary(IntPtr handle) => dlclose(handle) == 0;

        /// <inheritdoc />
        public string GetError() => Marshal.PtrToStringAnsi(dlerror());

        /// <inheritdoc />
        public void Prepare()
        {
            Marshal.Prelink(typeof(BsdLibLoader).GetMethod(nameof(dlsym), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(BsdLibLoader).GetMethod(nameof(dlopen), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(BsdLibLoader).GetMethod(nameof(dlerror), BindingFlags.Static | BindingFlags.NonPublic));
            Marshal.Prelink(typeof(BsdLibLoader).GetMethod(nameof(dlclose), BindingFlags.Static | BindingFlags.NonPublic));
        }
    }
}