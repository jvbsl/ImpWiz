using System;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ImpWiz.Marshalers
{
    public abstract class CustomMarshaler<TMarshaler, TSettings, TNative, TManaged> : IImpWizMarshaler<TSettings, TNative, TManaged>
        where TSettings : struct
        where TNative : unmanaged
        where TMarshaler : IImpWizMarshaler<TSettings, TNative, TManaged>
    {
        private static readonly Func<string, TMarshaler> GetInstance;

        static CustomMarshaler()
        {
            var type = typeof(TMarshaler);
            var mI = type.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static, null, new[] {typeof(string)},
                null);
            
            if (mI == null)
                throw new TypeLoadException($"static GetInstance(string) method missing from custom marshaler '{type.Name}'");

            GetInstance = (Func<string, TMarshaler>)mI.CreateDelegate(typeof(Func<string, TMarshaler>));
        }

        public abstract void MarshalManaged(TSettings info, TManaged managed);

        public abstract void MarshalNative(TSettings info, TNative native);
    }
}