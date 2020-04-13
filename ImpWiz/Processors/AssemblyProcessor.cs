using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ImpWiz.Filters;
using Mono.Cecil;

namespace ImpWiz
{
    /// <summary>
    /// Processes assemblies by searching for matching types and included <see cref="DllImportAttribute"/> and
    /// rewriting them to a ImpWiz implementation.
    /// </summary>
    public class AssemblyProcessor
    {
        /// <summary>
        /// A filter strategy to exclude or include specific types by.
        /// <seealso cref="TypeFilterStrategy"/>
        /// </summary>
        public ITypeFilterStrategy TypeFilterStrategy { get; }
        
        /// <summary>
        /// Gets a value indicating whether the ImpWiz Importer should be integrated into the assembly.
        /// </summary>
        public bool IntegrateImpWizImporter { get; }
        
        /// <summary>
        /// The Assembly to work on.
        /// </summary>
        public AssemblyDefinition Assembly { get; }
        
        /// <summary>
        /// The import assembly.
        /// </summary>
        public AssemblyDefinition ImportAssembly { get; }
        
        /// <summary>
        /// The import assembly default lib loader.
        /// </summary>
        public TypeDefinition ImportAssemblyLibLoader { get; }
        
        /// <summary>
        /// The needed module references, which should not be removed from the rewritten library.
        /// </summary>
        public HashSet<string> NeededModuleReferences { get; }
        
        public Dictionary<UnmanagedType, HashSet<MarshalerType>> SupportedMarshalers { get; }


        private static bool IsMarshaler(TypeDefinition typeDef)
        {
            if (typeDef.BaseType == null)
                return false;
            var baseType = typeDef.BaseType;
            if (baseType.Namespace == "ImpWiz.Import.Marshalers" && baseType.Name == "ImpWizMarshaler`4" &&
                baseType.IsGenericInstance && ((GenericInstanceType)baseType).GenericArguments.Count == 4)
                return true;
            if (typeDef.Interfaces.Any(x => x.InterfaceType.Namespace == "ImpWiz.Import.Marshalers"
                                            && x.InterfaceType.Name == "IImpWizMarshaler`3"))
                return true;
            return IsMarshaler((baseType.Resolve()));
        }
        
        public AssemblyProcessor(AssemblyDefinition assembly, ITypeFilterStrategy typeFilterStrategy = null, bool integrateImpWizImporter = false)
        {
            Assembly = assembly;
            IntegrateImpWizImporter = integrateImpWizImporter;
            TypeFilterStrategy = typeFilterStrategy ?? Filters.TypeFilterStrategy.All;
            NeededModuleReferences = new HashSet<string>();
            SupportedMarshalers = new Dictionary<UnmanagedType, HashSet<MarshalerType>>();
            
            var asmPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            var originalLibLoaderAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(asmPath, "ImpWiz.Import.dll"));
            
            
            
            if (IntegrateImpWizImporter)
            {
                //var libLoader = new TypeDefinition("ImpWiz.Import", "LibLoader", TypeAttributes.Class | TypeAttributes.Public, Assembly.MainModule.TypeSystem.Object);

                

                var originalLibLoaderType = originalLibLoaderAssembly.MainModule.Types.First(x => x.Namespace == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(Import.LibLoader) && x.Name == "LibLoader");

                var libLoader = originalLibLoaderType.Clone(Assembly.MainModule);
                
                foreach (var importLibType in originalLibLoaderAssembly.MainModule.Types)
                {
                    importLibType.Clone(Assembly.MainModule);
                }




                foreach (var modRef in originalLibLoaderAssembly.MainModule.ModuleReferences)
                {
                    NeededModuleReferences.Add(modRef.Name);
                }


                ImportAssembly = Assembly;

                ImportAssemblyLibLoader = (TypeDefinition)libLoader;
            }
            else
            {
                ImportAssembly = originalLibLoaderAssembly;

                ImportAssemblyLibLoader =
                    ImportAssembly.MainModule.Types.First(x => x.Namespace == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(Import.LibLoader) && x.Name == "LibLoader");
            }

            var allTypes = IntegrateImpWizImporter ? assembly.MainModule.Types : originalLibLoaderAssembly.MainModule.Types.Concat(assembly.MainModule.Types);
            foreach (var type in allTypes)
            {
                if (type.IsAbstract || type.IsInterface || !type.IsClass)
                    continue;

                if (IsMarshaler(type))
                {
                    var marshaler = new MarshalerType(type);
                    foreach (var unmanagedType in marshaler.SupportedUnmanagedTypes)
                    {
                        HashSet<MarshalerType> marshalers;
                        if (!SupportedMarshalers.TryGetValue(unmanagedType, out marshalers))
                        {
                            marshalers = new HashSet<MarshalerType>();
                            SupportedMarshalers.Add(unmanagedType, marshalers);
                        }

                        marshalers.Add(marshaler);
                    }
                }
                    
            }
        }

        private void MarshalerCleanup()
        {
            foreach (var marshalerMap in SupportedMarshalers)
            {
                foreach (var marshaler in marshalerMap.Value)
                {
                    var marshalerType = marshaler.TypeDefinition;
                    for (int i = marshalerType.Methods.Count - 1; i >= 0; i--)
                    {
                        if (!marshalerType.Methods[i].IsStatic)
                        {
                            marshalerType.Methods.RemoveAt(i);
                        }
                    }
                    
                    for (int i = marshalerType.Fields.Count - 1; i >= 0; i--)
                    {
                        if (!marshalerType.Fields[i].IsStatic)
                        {
                            marshalerType.Fields.RemoveAt(i);
                        }
                    }
                    
                    for (int i = marshalerType.Properties.Count - 1; i >= 0; i--)
                    {
                        if (marshalerType.Properties[i].HasThis)
                        {
                            marshalerType.Properties.RemoveAt(i);
                        }
                    }
                    for (int i = marshalerType.Interfaces.Count - 1; i >= 0; i--)
                    {
                        if (marshalerType.Interfaces[i].InterfaceType.Namespace == "ImpWiz.Import.Marshalers" &&
                            (marshalerType.Interfaces[i].InterfaceType.Name == "IImpWizMarshaler`3" ||
                             marshalerType.Interfaces[i].InterfaceType.Name == "IImpWizMarshaler"))
                        {
                            marshalerType.Interfaces.RemoveAt(i);
                        }
                    }

                    int remainingMembers = marshalerType.Properties.Count + marshalerType.Fields.Count +
                                           marshalerType.Methods.Count;
                    
                    marshalerType.BaseType = marshalerType.Module.TypeSystem.Object;
                    if (remainingMembers == 0)
                    {
                        Assembly.MainModule.Types.Remove(marshalerType);
                    }

                }
            }

            var mainModule = Assembly.MainModule;

            for (int i = mainModule.Types.Count - 1; i >= 0; i--)
            {
                if (mainModule.Types[i].Namespace == "ImpWiz.Import.Marshalers" &&
                    (mainModule.Types[i].Name == "ImpWizMarshaler`4" ||
                     mainModule.Types[i].Name == "IImpWizMarshaler`3" ||
                     mainModule.Types[i].Name == "IImpWizMarshaler" ||
                     mainModule.Types[i].Name == "MarshalInitialization`2"))
                {
                    mainModule.Types.RemoveAt(i);
                }
            }
        }

        public void Process()
        {
            foreach (var m in Assembly.Modules)
            {
                if (IntegrateImpWizImporter)
                {
                    for (int i = 0; i < m.AssemblyReferences.Count; i++)
                    {
                        if (m.AssemblyReferences[i].Name == ImportAssembly.Name.Name)
                        {
                            m.AssemblyReferences.RemoveAt(i);
                            break;
                        }
                    }
                }
                var moduleProcessor = new ModuleProcessor(this, m);
                moduleProcessor.Process();
            }

            MarshalerCleanup();
        }
    }
}