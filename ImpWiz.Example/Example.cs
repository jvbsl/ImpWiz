using System;
using System.Runtime.InteropServices;
using ImpWiz.Import;

namespace ImpWiz.Example
{
    public static class Example
    {
        [DllImport("<unavailable>", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableDll();
        
        [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableSymbol();

        [DllImport("<redirect>", EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        [return: MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CustomMarshaler))]
        public static extern string GetError();
    }
}