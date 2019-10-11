using System;
using System.Runtime.InteropServices;

namespace ImpWiz.Example
{
    public static class Example
    {
        [DllImport("<unavailable>", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableDll();
        
        [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableSymbol();

        [DllImport("libdl.so", EntryPoint = "dlerror", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr GetError();
    }
}