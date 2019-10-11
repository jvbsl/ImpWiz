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
        /// The Assembly to work on.
        /// </summary>
        public AssemblyDefinition Assembly { get; }
        
        public AssemblyProcessor(AssemblyDefinition assembly, ITypeFilterStrategy typeFilterStrategy = null)
        {
            Assembly = assembly;
            TypeFilterStrategy = typeFilterStrategy ?? Filters.TypeFilterStrategy.All;
        }
        public void Process()
        {
            foreach (var m in Assembly.Modules)
            {
                var moduleProcessor = new ModuleProcessor(this, m);
                moduleProcessor.Process();
            }
        }
    }
}