using System;
using System.Linq;
using Mono.Cecil;

namespace ImpWiz
{
    public class ModuleProcessor
    {
        public AssemblyProcessor AssemblyContext { get; }
        public ModuleDefinition Module { get; }
        
        
        public readonly AssemblyNameReference Netstandard;

        public ModuleProcessor(AssemblyProcessor assemblyContext, ModuleDefinition module)
        {
            Module = module;
            AssemblyContext = assemblyContext;

            Netstandard = Module.AssemblyReferences.First(x => x.Name == "netstandard");
        }

        public void Process()
        {
            foreach (var t in Module.Types)
            {
                if (!t.IsClass)
                    continue;

                if (!AssemblyContext.TypeFilterStrategy.Filter(t))
                    continue;

                var typeProcessor = new TypeProcessor(this, t);
                typeProcessor.Process();
            }
            
            // TODO: better Mvid strategy?
            Module.Mvid = Guid.NewGuid();
        }
    }
}