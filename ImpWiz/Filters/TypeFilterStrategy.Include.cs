using System.Linq;
using ImpWiz.Import;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace ImpWiz.Filters
{
    public static partial class TypeFilterStrategy
    {
        private class FilterStrategyInclude : ITypeFilterStrategy
        {
            /// <inheritdoc />
            public bool Filter(Collection<CustomAttribute> customAttributes)
            {
                var attr = customAttributes.FirstOrDefault(x => x.AttributeType.FullName == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(ImportFilterAttribute));
                if (attr == null)
                    return false;

                var attrValue = attr.CreateInstance<ImportFilterAttribute>();

                return (attrValue.Include);
            }
        }
    }
}