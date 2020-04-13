namespace ImpWiz.Marshalers
{
    public class VtBoolMarshaler : BoolMarshaler
    {
        public override ElementType GetNativeBoolElementType()
        {
            return ElementType.ELEMENT_TYPE_I2;
        }

        public override int GetNativeTrueValue()
        {
            return 65535;
        }

        public override int GetNativeFalseValue()
        {
            return 0;
        }
    }
}