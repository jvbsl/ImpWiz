using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;

namespace ImpWiz
{
    public class ModuleProcessor
    {
        public AssemblyProcessor AssemblyContext { get; }
        public ModuleDefinition Module { get; }
        
        
        public readonly AssemblyNameReference Netstandard;

        public readonly AssemblyNameReference ImportAssembly;

        public ModuleProcessor(AssemblyProcessor assemblyContext, ModuleDefinition module)
        {
            Module = module;
            AssemblyContext = assemblyContext;

            Netstandard = Module.AssemblyReferences.First(x => x.Name == "netstandard");

            ImportAssembly = Module.AssemblyReferences.FirstOrDefault(x => x.Name == "ImpWiz.Import");
            if (ImportAssembly == null)
                ImportAssembly = new AssemblyNameReference("ImpWiz.Import", new Version());
        }

        internal static void FixAttributeReferences(Collection<CustomAttribute> customAttributes, ModuleDefinition module)
        {
            for (int i = customAttributes.Count - 1; i >= 0; i--)
            {
                var customAttribute = customAttributes[i];
                var declaringType = FixTypeReference(customAttribute.AttributeType, module);
                if (declaringType != customAttribute.AttributeType)
                {
                    var newAttributeCtor = new MethodReference(customAttribute.Constructor.Name, module.TypeSystem.Void, declaringType);
                    newAttributeCtor.HasThis = customAttribute.Constructor.HasThis;
                    newAttributeCtor.ExplicitThis = customAttribute.Constructor.ExplicitThis;
                    var newCustomAttribute = new CustomAttribute(newAttributeCtor, customAttribute.GetBlob());
                    foreach (var p in customAttribute.Constructor.Parameters)
                    {
                        var newParam = new ParameterDefinition(module.ImportReference(p.ParameterType));
                        newParam.Constant = p.Constant;
                        newParam.HasConstant = p.HasConstant;
                        newParam.HasDefault = p.HasDefault;
                        foreach (var ca in p.CustomAttributes)
                        {
                            newParam.CustomAttributes.Add(new CustomAttribute(FixMethodReference(ca.Constructor, module), ca.GetBlob()));
                        }
                        newAttributeCtor.Parameters.Add(newParam);
                    }

                    foreach (var a in customAttribute.ConstructorArguments)
                    {
                        newCustomAttribute.ConstructorArguments.Add(new CustomAttributeArgument(FixTypeReference(a.Type,module), a.Value));
                    }
                    customAttributes.RemoveAt(i);
                    customAttributes.Add(newCustomAttribute);
                }
            }
        }

        internal static TypeReference FixTypeReference(TypeReference typeReference, ModuleDefinition module)
        {
            if (typeReference == null)
                return null;

            if (typeReference.Namespace.StartsWith("ImpWiz.Import"))
            {
                return module.GetType(typeReference.Namespace, typeReference.Name);
            }

            return typeReference;
        }

        internal static MethodReference FixMethodReference(MethodReference methodReference, ModuleDefinition module)
        {
            if (methodReference == null)
                return null;
            if (methodReference.DeclaringType.Namespace.StartsWith("ImpWiz.Import"))
            {
                var declaringType = FixTypeReference(methodReference.DeclaringType, module).Resolve();
                var newMethod = declaringType.Methods.First(
                    x => x.Name == methodReference.Name && x.Parameters.Count == methodReference.Parameters.Count &&
                         x.Parameters.Zip(methodReference.Parameters, (p1, p2) =>
                             p1.ParameterType.Namespace == p2.ParameterType.Namespace &&
                             p1.ParameterType.Name == p2.ParameterType.Name).All(same => same));
                return newMethod;
            }

            return methodReference;
        }

        internal static void FixTypeDefinitionReferences(TypeDefinition type, ModuleDefinition module)
        {
            if (type.Namespace.StartsWith("ImpWiz.Import"))
                return;
            type.BaseType = FixTypeReference(type.BaseType, module);
            FixAttributeReferences(type.CustomAttributes, module);
            for (int i = type.Interfaces.Count - 1; i >= 0; i--)
            {
                var inter = type.Interfaces[i];
                inter.InterfaceType = FixTypeReference(inter.InterfaceType, module);
                FixAttributeReferences(inter.CustomAttributes, module);
            }

            for (int i = type.Fields.Count - 1; i >= 0; i--)
            {
                var f =type.Fields[i];
                f.FieldType = FixTypeReference(f.FieldType, module);
                FixAttributeReferences(f.CustomAttributes, module);
            }

            for (int i = type.Properties.Count - 1; i >= 0; i--)
            {
                var p = type.Properties[i];
                p.PropertyType = FixTypeReference(p.PropertyType, module);
                FixAttributeReferences(p.CustomAttributes, module);
            }

            for (int i = type.Methods.Count - 1; i >= 0; i--)
            {
                var m = type.Methods[i];
                m.ReturnType = FixTypeReference(m.ReturnType, module);
                for (int j = m.Parameters.Count - 1; j >= 0; j--)
                {
                    var p = m.Parameters[j];
                    p.ParameterType = FixTypeReference(p.ParameterType, module);
                }
                FixAttributeReferences(type.Methods[i].CustomAttributes, module);

                if (m.HasBody)
                {
                    foreach (var variable in m.Body.Variables)
                    {
                        variable.VariableType = FixTypeReference(variable.VariableType, module);
                    }
                    foreach (var instruction in m.Body.Instructions)
                    {
                        switch (instruction.Operand)
                        {
                            case MethodReference method:
                                instruction.Operand = FixMethodReference(method, module);
                                break;
                            case TypeReference typeRef:
                                instruction.Operand = FixTypeReference(typeRef, module);
                                break;
                        }
                    }
                }
            }
        }

        public void Process()
        {
            HashSet<ModuleReference> neededRefs = new HashSet<ModuleReference>();
            foreach (var t in Module.Types)
            {
                if (!t.IsClass)
                    continue;
                if (AssemblyContext.IntegrateImpWizImporter && t.Namespace.StartsWith("ImpWiz.Import"))
                    continue;
                
                if (AssemblyContext.IntegrateImpWizImporter)
                {
                    FixTypeDefinitionReferences(t, Module);
                }

                if (!AssemblyContext.TypeFilterStrategy.Filter(t))
                {
                    foreach (var m in t.Methods)
                    {
                        if (!m.IsStatic)
                            continue;
                        if (m.HasBody)
                            continue;
                        neededRefs.Add(m.PInvokeInfo.Module);
                    }
                    continue;
                }

                var typeProcessor = new TypeProcessor(this, t);
                typeProcessor.Process();
            }

            for (int i = Module.ModuleReferences.Count - 1; i >= 0; i--)
            {
                var mRef = Module.ModuleReferences[i];
                if (!neededRefs.Contains((mRef)) && !AssemblyContext.NeededModuleReferences.Contains(mRef.Name))
                {
                    //TODO reimplement without removing ImpWiz.Import references...
                    Module.ModuleReferences.RemoveAt(i);
                }
            }

            if (AssemblyContext.IntegrateImpWizImporter)
            {
                for (int i = 0; i < Module.AssemblyReferences.Count; i++)
                {
                    if (Module.AssemblyReferences[i].Name == nameof(ImpWiz) + "." + nameof(Import))
                        Module.AssemblyReferences.RemoveAt(i);
                }
            }

            // TODO: better Mvid strategy?
            Module.Mvid = Guid.NewGuid();
            
            
        }
    }
}