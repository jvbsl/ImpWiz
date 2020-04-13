using System;

namespace ImpWiz.Marshalers
{
    public abstract class BoolMarshaler : Marshaler
    {
        public abstract ElementType GetNativeBoolElementType();
        public abstract int GetNativeTrueValue();
        public abstract int GetNativeFalseValue();
        
        protected override LocalDesc GetNativeType()
        {
            return new LocalDesc(GetNativeBoolElementType());
        }

        protected override LocalDesc GetManagedType()
        {
            return new LocalDesc(ElementType.ELEMENT_TYPE_BOOLEAN);
        }
        
        
    }
}