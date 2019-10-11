using System.Linq;
using ImpWiz.Import;
using Mono.Cecil;

namespace ImpWiz.Filters
{
    public static partial class TypeFilterStrategy
    {
        private class FilterStrategyExclude : ITypeFilterStrategy
        {
            /// <inheritdoc />
            public bool Filter(TypeDefinition type)
            {
                var attr = type.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(ImportFilterAttribute));
                if (attr == null)
                    return true;

                var attrValue = attr.CreateInstance<ImportFilterAttribute>();

                return (attrValue.Include);
            }
        }
    }
}