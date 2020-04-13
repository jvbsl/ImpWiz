using System;
using System.Runtime.InteropServices;
using ImpWiz.Import;
using ImpWiz.Import.LibLoader;

namespace ImpWiz.Example
{
    public class CustomLibraryLoader : ICustomLibraryLoader
    {
        public IntPtr LoadLibrary(string name)
        {
            if (name == "<redirect>")
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return LibLoader.Instance.LoadLibrary("libNativeTestLibrary.so");
                return LibLoader.Instance.LoadLibrary("NativeTestLibrary.dll");
            }

            return LibLoader.Instance.LoadLibrary(name);
        }

        public IntPtr GetProcAddress(IntPtr handle, string name)
        {
            return LibLoader.Instance.GetProcAddress(handle, name);
        }

        public bool FreeLibrary(IntPtr handle)
        {
            return LibLoader.Instance.FreeLibrary(handle);
        }

        public string GetError()
        {
            return LibLoader.Instance.GetError();
        }
        
        private static readonly CustomLibraryLoader Instance = new CustomLibraryLoader();
        
        public static ICustomLibraryLoader GetInstance(string libLoaderCookie)
        {
            return Instance;
        }
    }
}