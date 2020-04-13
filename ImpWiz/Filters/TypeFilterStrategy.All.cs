using Mono.Cecil;
using Mono.Collections.Generic;

namespace ImpWiz.Filters
{
    public partial class TypeFilterStrategy
    {
        private class FilterStrategyAll : ITypeFilterStrategy
        {
            /// <inheritdoc />
            public bool Filter(Collection<CustomAttribute> customAttributes)
            {
                return true;
            }
        }
    }
}