using Mono.Cecil;

namespace ImpWiz.Filters
{
    public partial class TypeFilterStrategy
    {
        private class FilterStrategyAll : ITypeFilterStrategy
        {
            /// <inheritdoc />
            public bool Filter(TypeDefinition type)
            {
                return true;
            }
        }
    }
}