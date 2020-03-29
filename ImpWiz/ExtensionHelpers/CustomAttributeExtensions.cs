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
        /// <param name="context">The <see cref="ModuleProcessor"/> context the attribute is created in.</param>
        /// <typeparam name="T">The type to create an instance of.</typeparam>
        /// <returns>The instance of the attribute on success; otherwise <c>null</c>.</returns>
        public static T CreateInstance<T>(this CustomAttribute attribute, ModuleProcessor context = null) where T : class
        {
            if (attribute == null)
                throw new ArgumentNullException();
            var ctorParamTypes = context == null ? 
                attribute.ConstructorArguments.Select(x => x.Value?.GetType() ?? Type.GetType(x.Type.FullName)).ToArray() :
                attribute.ConstructorArguments.Select(x => x.Value?.GetType() ?? Type.GetType(x.Type.FullName)).Prepend(typeof(ModuleProcessor)).ToArray();
            var ctor = typeof(T).GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, ctorParamTypes,
                null);

            int contextArgsCount = (context == null ? 0 : 1);
            var paramValues = new object[attribute.ConstructorArguments.Count + contextArgsCount];
            if (context != null)
                paramValues[0] = context;
            for (int i = contextArgsCount; i < paramValues.Length; i++)
            {
                var argValue = attribute.ConstructorArguments[i - contextArgsCount].Value;
                paramValues[i] = argValue;
            }

            return ctor?.Invoke(paramValues) as T;
        }
    }
}