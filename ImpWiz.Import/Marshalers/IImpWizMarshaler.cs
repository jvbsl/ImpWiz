using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using ImpWiz.Import;

namespace ImpWiz.Import.Marshalers
{
    public static class MarshalInitialization<TNative, TManaged>
        where TNative : unmanaged
    {
        public static void ObjectInitialized(TManaged managed){}
        public static void ObjectInitialized(TNative native){}
    }
    public interface IImpWizMarshaler<TSettings, TNative, TManaged> : IImpWizMarshaler
        where TNative : unmanaged
    {
        void MarshalManaged(TSettings info, TManaged managed);
        void MarshalNative(TSettings info, TNative native);
    }

    public interface IImpWizMarshaler
    {
        
    }
}