using System;
using System.Runtime.InteropServices;

namespace ImpWiz.Import.Marshalers
{
    public class ImpWizCustomMarshaler<TCustomMarshaler> : IImpWizMarshaler<TCustomMarshaler, IntPtr, object>
        where TCustomMarshaler : ICustomMarshaler
    {

        public void MarshalManaged(TCustomMarshaler info, object managed)
        {
            var native = info.MarshalManagedToNative(managed);
            MarshalInitialization<IntPtr, object>.ObjectInitialized(native);
            info.CleanUpNativeData(native);
        }

        public void MarshalNative(TCustomMarshaler info, IntPtr native)
        {
            var managed = info.MarshalNativeToManaged(native);
            MarshalInitialization<IntPtr, object>.ObjectInitialized(managed);
            info.CleanUpManagedData(managed);
        }
    }
}