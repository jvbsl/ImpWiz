using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using ImpWiz.Import;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ImpWiz.Marshalers
{
    public interface IImpWizMarshaler<TSettings, TNative, TManaged> : IImpWizMarshaler
        where TNative : unmanaged
        where TSettings : struct
    {
        void MarshalManaged(TSettings info, TManaged managed);
        void MarshalNative(TSettings info, TNative native);

        static void ObjectInitialized(TManaged managed){}
        static void ObjectInitialized(TNative native){}
    }

    public interface IImpWizMarshaler
    {
        
    }
}