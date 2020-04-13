namespace ImpWiz.Marshalers
{
    public abstract class Marshaler
    {
        protected abstract LocalDesc GetNativeType();

        protected abstract LocalDesc GetManagedType();
    }
}