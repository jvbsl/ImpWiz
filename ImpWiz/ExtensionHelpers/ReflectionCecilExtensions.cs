using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        // https://stackoverflow.com/questions/40018991/how-to-implement-isassignablefrom-with-mono-cecil
        /// <summary>
        /// Is childTypeDef a subclass of parentTypeDef. Does not test interface inheritance
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentTypeDef"></param>
        /// <returns></returns>
        public static bool IsSubclassOf(this TypeDefinition childTypeDef, TypeDefinition parentTypeDef) =>
            childTypeDef.MetadataToken
            != parentTypeDef.MetadataToken
            && childTypeDef
                .EnumerateBaseClasses()
                .Any(b => b.MetadataToken == parentTypeDef.MetadataToken);

        /// <summary>
        /// Does childType inherit from parentInterface
        /// </summary>
        /// <param name="childType"></param>
        /// <param name="parentInterfaceDef"></param>
        /// <returns></returns>
        public static bool DoesAnySubTypeImplementInterface(this TypeDefinition childType,
            TypeDefinition parentInterfaceDef)
        {
            Debug.Assert(parentInterfaceDef.IsInterface);
            return childType
                .EnumerateBaseClasses()
                .Any(typeDefinition => typeDefinition.DoesSpecificTypeImplementInterface(parentInterfaceDef));
        }

        /// <summary>
        /// Does the childType directly inherit from parentInterface. Base
        /// classes of childType are not tested
        /// </summary>
        /// <param name="childTypeDef"></param>
        /// <param name="parentInterfaceDef"></param>
        /// <returns></returns>
        public static bool DoesSpecificTypeImplementInterface(this TypeDefinition childTypeDef,
            TypeDefinition parentInterfaceDef)
        {
            Debug.Assert(parentInterfaceDef.IsInterface);
            return childTypeDef
                .Interfaces
                .Any(ifaceDef => DoesSpecificInterfaceImplementInterface(ifaceDef.InterfaceType.Resolve(), parentInterfaceDef));
        }

        /// <summary>
        /// Does interface iface0 equal or implement interface iface1
        /// </summary>
        /// <param name="iface0"></param>
        /// <param name="iface1"></param>
        /// <returns></returns>
        public static bool DoesSpecificInterfaceImplementInterface(TypeDefinition iface0, TypeDefinition iface1)
        {
            Debug.Assert(iface1.IsInterface);
            Debug.Assert(iface0.IsInterface);
            return iface0.Namespace == iface1.Namespace && iface0.Name == iface1.Name || iface0.DoesAnySubTypeImplementInterface(iface1);
        }

        /// <summary>
        /// Is source type assignable to target type
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsAssignableFrom(this TypeDefinition target, TypeDefinition source)
            => target == source
               || target.MetadataToken == source.MetadataToken
               || source.IsSubclassOf(target)
               || target.IsInterface && source.DoesAnySubTypeImplementInterface(target);

        /// <summary>
        /// Enumerate the current type, it's parent and all the way to the top type
        /// </summary>
        /// <param name="klassType"></param>
        /// <returns></returns>
        public static IEnumerable<TypeDefinition> EnumerateBaseClasses(this TypeDefinition klassType)
        {
            for (var typeDefinition = klassType;
                typeDefinition != null;
                typeDefinition = typeDefinition.BaseType?.Resolve())
            {
                yield return typeDefinition;
            }
        }

        /// <summary>
        /// Converts a <see cref="System.Reflection.MethodInfo"/> to a <see cref="MethodReference"/> using the given
        /// <paramref name="usingModule"/> as an import context.
        /// </summary>
        /// <param name="info">The <see cref="MethodInfo"/> to convert.</param>
        /// <param name="usingModule">The <see cref="ModuleDefinition"/> to use as an import context.</param>
        /// <returns>The resulting <see cref="MethodReference"/>.</returns>
        public static MethodReference ToCecilReference(this MethodInfo info, ModuleDefinition usingModule)
        {
            var mRef = new MethodReference(info.Name, info.ReturnType.ToCecilReference(usingModule),
                info.DeclaringType.ToCecilReference(usingModule));

            mRef.HasThis = !info.IsStatic;
            foreach (var p in info.GetGenericArguments())
            {
                var typeRef = p.ToCecilReference(usingModule);
                mRef.GenericParameters.Add(new GenericParameter(typeRef));
            }

            foreach (var p in info.GetParameters())
            {
                mRef.Parameters.Add(new ParameterDefinition(p.Name, (ParameterAttributes) p.Attributes,
                    p.ParameterType.ToCecilReference(usingModule)));
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
            var typeRef = new TypeReference(type.Namespace, type.Name, usingModule,
                (type.DeclaringType ?? type).Assembly.ToCecilReference(usingModule));
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