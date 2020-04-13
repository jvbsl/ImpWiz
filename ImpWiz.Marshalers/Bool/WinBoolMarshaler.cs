namespace ImpWiz.Marshalers
{
    public class WinBoolMarshaler : BoolMarshaler
    {
        public override ElementType GetNativeBoolElementType()
        {
            return ElementType.ELEMENT_TYPE_I4;
        }

        public override int GetNativeTrueValue()
        {
            return 1;
        }

        public override int GetNativeFalseValue()
        {
            return 0;
        }
    }
}