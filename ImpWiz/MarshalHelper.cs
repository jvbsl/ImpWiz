using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ImpWiz;
using ImpWiz.Processors;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace ImpWiz.Import.Marshalers
{
    public class MarshalHelper
    {

        public static MarshalProcessor GetMarshaler(MethodProcessor methodProcessor, ParameterDefinition parameter)
        {
            var assemblyProcessor = methodProcessor.TypeContext.ModuleContext.AssemblyContext;
            if (!parameter.HasMarshalInfo && parameter.ParameterType.IsValueType)
                return null;
            if (!parameter.ParameterType.IsValueType && !parameter.HasMarshalInfo)
            {
                return null;
            }
            var attribute = parameter.MarshalInfo;
            switch (attribute)
            {
                case CustomMarshalInfo c:
                    var managedType = c.ManagedType.Resolve();
                    var interfaces = managedType.Interfaces;
                    var marshalerInterface =
                        assemblyProcessor.ImportAssembly.MainModule.GetType("ImpWiz.Import.Marshalers",
                            "IImpWizMarshaler");
                    if (interfaces.Any(x =>
                        x.InterfaceType.Namespace == "System.Runtime.InteropServices" &&
                        x.InterfaceType.Name == "ICustomMarshaler"))
                    {
                        return new MarshalProcessor(new MarshalerType(assemblyProcessor.ImportAssembly.MainModule.GetType(typeof(ImpWizCustomMarshaler<>).FullName)
                            .MakeGenericInstanceType(c.ManagedType).Resolve()), methodProcessor, parameter);
                    }
                    else if (marshalerInterface.IsAssignableFrom(managedType))
                    {
                        return new MarshalProcessor(new MarshalerType(managedType), methodProcessor, parameter);
                    }

                    break;
            }

            if (assemblyProcessor.SupportedMarshalers.TryGetValue((UnmanagedType)attribute.NativeType, out var marshalers))
            {
                return new MarshalProcessor(marshalers.First(), methodProcessor, parameter);
            }
            return null;
        }
        
        public static MarshalProcessor GetMarshaler(MethodProcessor methodProcessor, MethodReturnType parameter)
        {
            var assemblyProcessor = methodProcessor.TypeContext.ModuleContext.AssemblyContext;
            if (!parameter.HasMarshalInfo && parameter.ReturnType.IsValueType)
                return null;
            if (!parameter.ReturnType.IsValueType && !parameter.HasMarshalInfo)
            {
                return null;
            }
            var attribute = parameter.MarshalInfo;
            switch (attribute)
            {
                case CustomMarshalInfo c:
                    var managedType = c.ManagedType.Resolve();
                    var interfaces = managedType.Interfaces;
                    var marshalerInterface =
                        assemblyProcessor.ImportAssembly.MainModule.GetType("ImpWiz.Import.Marshalers",
                            "IImpWizMarshaler");
                    if (interfaces.Any(x =>
                        x.InterfaceType.Namespace == "System.Runtime.InteropServices" &&
                        x.InterfaceType.Name == "ICustomMarshaler"))
                    {
                        return new MarshalProcessor(new MarshalerType(assemblyProcessor.ImportAssembly.MainModule.GetType(typeof(ImpWizCustomMarshaler<>).FullName)
                            .MakeGenericInstanceType(c.ManagedType).Resolve()), methodProcessor, parameter);
                    }
                    else if (marshalerInterface.IsAssignableFrom(managedType))
                    {
                        return new MarshalProcessor(new MarshalerType(managedType), methodProcessor, parameter);
                    }

                    break;
            }

            if (assemblyProcessor.SupportedMarshalers.TryGetValue((UnmanagedType)attribute.NativeType, out var marshalers))
            {
                return new MarshalProcessor(marshalers.First(), methodProcessor, parameter);
            }
            return null;
        }
    }
}