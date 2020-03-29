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
        
        
        public AssemblyProcessor(AssemblyDefinition assembly, ITypeFilterStrategy typeFilterStrategy = null, bool integrateImpWizImporter = false)
        {
            Assembly = assembly;
            IntegrateImpWizImporter = integrateImpWizImporter;
            TypeFilterStrategy = typeFilterStrategy ?? Filters.TypeFilterStrategy.All;
            NeededModuleReferences = new HashSet<string>();
            
            var asmPath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            
            if (IntegrateImpWizImporter)
            {
                //var libLoader = new TypeDefinition("ImpWiz.Import", "LibLoader", TypeAttributes.Class | TypeAttributes.Public, Assembly.MainModule.TypeSystem.Object);

                var originalLibLoaderAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(asmPath, "ImpWiz.Import.dll"));

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

                ImportAssemblyLibLoader = libLoader;
            }
            else
            {
                ImportAssembly = AssemblyDefinition.ReadAssembly(Path.Combine(asmPath, "ImpWiz.Import.dll"));

                ImportAssemblyLibLoader =
                    ImportAssembly.MainModule.Types.First(x => x.Namespace == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(Import.LibLoader) && x.Name == "LibLoader");
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
        }
    }
}