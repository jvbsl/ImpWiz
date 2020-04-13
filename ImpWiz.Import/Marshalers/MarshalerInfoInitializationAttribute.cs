using System;

namespace ImpWiz.Import.Marshalers
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class MarshalerInfoInitializationAttribute : Attribute
    {
        public string InitializationGenerator { get; }
        public MarshalerInfoInitializationAttribute(string initializationGenerator)
        {
            InitializationGenerator = initializationGenerator;
        }
    }
}