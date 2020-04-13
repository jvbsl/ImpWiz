using System;
using System.Reflection;

namespace ImpWiz.Import.Marshalers
{
    public abstract class ImpWizMarshaler<TMarshaler, TSettings, TNative, TManaged> : IImpWizMarshaler<TSettings, TNative, TManaged>
        where TSettings : struct
        where TNative : unmanaged
        where TMarshaler : IImpWizMarshaler<TSettings, TNative, TManaged>
    {
        public abstract void MarshalManaged(TSettings info, TManaged managed);

        public abstract void MarshalNative(TSettings info, TNative native);
    }
}