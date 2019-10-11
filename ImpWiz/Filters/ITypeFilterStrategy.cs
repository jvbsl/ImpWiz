using Mono.Cecil;

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
        /// <param name="type">The type to check.</param>
        /// <returns><c>true</c> if the type should be included; otherwise <c>false</c></returns>
        bool Filter(TypeDefinition type);
    }
}