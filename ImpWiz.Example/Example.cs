using System;
using System.Runtime.InteropServices;
using ImpWiz.Import;
using ImpWiz.Import.Marshalers;

namespace ImpWiz.Example
{
    public static class Example
    {
        [DllImport("<unavailable>", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableDll();
        
        [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl)]
        public static extern void UnavailableSymbol();
        
        [DllImport("<redirect>", EntryPoint = "test", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        public static extern void Test([MarshalAs(UnmanagedType.LPStr)]string bla);
        
        [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Test([MarshalAs(UnmanagedType.LPStr)]string bla1, [MarshalAs(UnmanagedType.LPWStr)]string bla2);
        
        [DllImport("libdl.so", CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string TestRet([MarshalAs(UnmanagedType.LPStr)]string bla1, [MarshalAs(UnmanagedType.LPWStr)]string bla2);
        
        [DllImport("<redirect>", EntryPoint = "GetLPSTR", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string GetLPSTR();

        [DllImport("<redirect>", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        public static extern int GetInt32();
        
        [DllImport("<redirect>", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        public static extern int Add(int a, int b);

        [DllImport("<redirect>", EntryPoint = "ParseInt32", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        public static extern int ParseInt32([MarshalAs(UnmanagedType.LPStr)] string str);
        
        [DllImport("<redirect>", EntryPoint = "Combine", CallingConvention = CallingConvention.Cdecl)]
        [ImportLoader(typeof(CustomLibraryLoader))]
        [return: MarshalAs(UnmanagedType.LPStr)]
        public static extern string Combine([MarshalAs(UnmanagedType.LPStr)] string a, [MarshalAs(UnmanagedType.LPStr)] string b);
    }
}