using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace ImpWiz
{
    /// <summary>
    /// Extension methods to help handle reference conversion between <see cref="System.Reflection"/>
    /// and <see cref="Mono.Cecil"/>.
    /// </summary>
    public static class ReflectionCecilExtensions
    {
        /// <summary>
        /// Converts a <see cref="System.Reflection.MethodInfo"/> to a <see cref="MethodReference"/> using the given
        /// <paramref name="usingModule"/> as an import context.
        /// </summary>
        /// <param name="info">The <see cref="MethodInfo"/> to convert.</param>
        /// <param name="usingModule">The <see cref="ModuleDefinition"/> to use as an import context.</param>
        /// <returns>The resulting <see cref="MethodReference"/>.</returns>
        public static MethodReference ToCecilReference(this MethodInfo info, ModuleDefinition usingModule)
        {
            
            var mRef = new MethodReference(info.Name, info.ReturnType.ToCecilReference(usingModule), info.DeclaringType.ToCecilReference(usingModule));
            
            mRef.HasThis = !info.IsStatic;
            foreach (var p in info.GetGenericArguments())
            {
                var typeRef = p.ToCecilReference(usingModule);
                mRef.GenericParameters.Add(new GenericParameter(typeRef));
            }
            foreach (var p in info.GetParameters())
            {
                mRef.Parameters.Add(new ParameterDefinition(p.Name, (ParameterAttributes) p.Attributes, p.ParameterType.ToCecilReference(usingModule)));
            }
            
            if (mRef.Module == usingModule)
                mRef.MetadataToken = mRef.Resolve().MetadataToken;
            
            return usingModule.ImportReference(mRef);
        }

        /// <summary>
        /// Converts a <see cref="Type"/> to a <see cref="TypeReference"/> using the given
        /// <paramref name="usingModule"/> as an import context.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to convert.</param>
        /// <param name="usingModule">The <see cref="ModuleDefinition"/> to use as an import context.</param>
        /// <returns>The resulting <see cref="TypeReference"/>.</returns>
        public static TypeReference ToCecilReference(this Type type, ModuleDefinition usingModule)
        {
            
            var typeRef = new TypeReference(type.Namespace, type.Name, usingModule, (type.DeclaringType ?? type).Assembly.ToCecilReference(usingModule));
            if (typeRef.Module == usingModule)
                return usingModule.ImportReference(typeRef).Resolve();
            return usingModule.ImportReference(typeRef);
        }

        
        /// <summary>
        /// Converts a <see cref="Assembly"/> to a <see cref="AssemblyNameReference"/> using the given
        /// <paramref name="usingModule"/> as an import context.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> to convert.</param>
        /// <param name="usingModule">The <see cref="ModuleDefinition"/> to use as an import context.</param>
        /// <returns>The resulting <see cref="AssemblyNameReference"/>.</returns>
        public static AssemblyNameReference ToCecilReference(this Assembly assembly, ModuleDefinition usingModule)
        {
            var asmName = assembly.GetName();
            if (usingModule.Assembly.Name.Name == asmName.Name && usingModule.Assembly.Name.Version == asmName.Version)
                return usingModule.Assembly.Name;
            foreach (var x in usingModule.AssemblyReferences)
            {
                if (x.Name == asmName.Name && x.Version == asmName.Version)
                    return x;
                if (x.Name == "netstandard" && asmName.Name == "System.Private.CoreLib")
                    return x;
            }
            throw new NotSupportedException("non existing AssemblyRef not supported");
        }
    }
}