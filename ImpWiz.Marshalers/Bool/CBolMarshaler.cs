namespace ImpWiz.Marshalers
{
    public class CBolMarshaler : BoolMarshaler
    {
        public override ElementType GetNativeBoolElementType()
        {
            return ElementType.ELEMENT_TYPE_I1;
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