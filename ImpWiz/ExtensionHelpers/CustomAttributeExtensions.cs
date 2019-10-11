using System;
using System.Linq;
using Mono.Cecil;
using BindingFlags = System.Reflection.BindingFlags;

namespace ImpWiz
{
    /// <summary>
    /// Helpful extensions for <see cref="CustomAttribute"/>.
    /// </summary>
    public static class CustomAttributeExtensions
    {
        /// <summary>
        /// Creates instance of <typeparamref name="T"/> of the <see cref="CustomAttribute"/>.
        /// </summary>
        /// <param name="attribute">The <see cref="CustomAttribute"/> used for creating the instance.</param>
        /// <typeparam name="T">The type to create an instance of.</typeparam>
        /// <returns>The instance of the attribute on success; otherwise <c>null</c>.</returns>
        public static T CreateInstance<T>(this CustomAttribute attribute) where T : class
        {
            var ctorParamTypes = attribute.ConstructorArguments.Select(x => Type.GetType(x.Type.FullName)).ToArray();
            var ctor = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, ctorParamTypes,
                null);

            return ctor?.Invoke(attribute.ConstructorArguments.Select(x => x.Value).ToArray()) as T;
        }
    }
}