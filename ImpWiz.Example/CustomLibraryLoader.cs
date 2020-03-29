using System;
using ImpWiz.Import;
using ImpWiz.Import.LibLoader;

namespace ImpWiz.Example
{
    public class CustomLibraryLoader : ICustomLibraryLoader
    {
        public IntPtr LoadLibrary(string name)
        {
            if (name == "<redirect>")
                return LibLoader.Instance.LoadLibrary("libdl.so");
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