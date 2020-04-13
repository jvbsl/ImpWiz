using Mono.Cecil;
using Mono.Collections.Generic;

namespace ImpWiz.Filters
{
    /// <summary>
    /// A filter strategy for types.
    /// </summary>
    public interface ITypeFilterStrategy
    {
        /// <summary>
        /// Whether to include the given <paramref name="type"/> or not.
        /// </summary>
        /// <param name="customAttributes">The custom attributes to check.</param>
        /// <returns><c>true</c> if the attributes suggest it should be included; otherwise <c>false</c></returns>
        bool Filter(Collection<CustomAttribute> customAttributes);
    }
}