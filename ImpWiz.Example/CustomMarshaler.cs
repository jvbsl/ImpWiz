using System;
using System.Runtime.InteropServices;

namespace ImpWiz.Example
{
    public class CustomMarshaler : ICustomMarshaler
    {
        public void CleanUpManagedData(object managedObj)
        {
            throw new NotImplementedException();
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            throw new NotImplementedException();
        }

        public int GetNativeDataSize()
        {
            throw new NotImplementedException();
        }

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            throw new NotImplementedException();
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new NotImplementedException();
        }
        
        private static readonly CustomMarshaler Instance = new CustomMarshaler();

        public static CustomMarshaler GetInstance(string cookie)
        {
            return Instance;
        }
    }
}