using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImpWiz.Import.Marshalers;
using Mono.Cecil;

namespace ImpWiz
{
    public class MarshalerType
    {
        public TypeDefinition TypeDefinition { get; }
        
        public HashSet<UnmanagedType> SupportedUnmanagedTypes { get; }
        
        public MarshalerType(TypeDefinition typeDefinition)
        {
            TypeDefinition = typeDefinition;
            SupportedUnmanagedTypes = new HashSet<UnmanagedType>();

            foreach (var ca in typeDefinition.CustomAttributes)
            {
                if (ca.AttributeType.Namespace == "ImpWiz.Import.Marshalers" &&
                    ca.AttributeType.Name == nameof(MarshalerTypeAttribute))
                {
                    SupportedUnmanagedTypes.Add((UnmanagedType) ca.ConstructorArguments[0].Value);
                }
            }
        }
    }
}