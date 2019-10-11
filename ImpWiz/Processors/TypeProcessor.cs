using System;
using System.Collections.Generic;
using System.Linq;
using ImpWiz.Import.LibLoader;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ImpWiz
{
    public class TypeProcessor
    {
        public ModuleProcessor ModuleContext { get; }

        public ModuleDefinition Module => ModuleContext.Module;
        
        public TypeDefinition Type { get; }
        
        public FieldDefinition LockObject { get; private set; }
        
        public Dictionary<string, MethodDefinition> LibraryLoaders { get; }

        public TypeProcessor(ModuleProcessor moduleContext, TypeDefinition type)
        {
            ModuleContext = moduleContext;
            Type = type;
            
            LibraryLoaders = new Dictionary<string, MethodDefinition>();
            
            InteropLibScope = Module.AssemblyReferences.First(x => x.Name == nameof(ImpWiz) + "." + nameof(Import));
            
            InteropLibTypeReference = new TypeReference(nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(Import.LibLoader), nameof(LibLoader), Module, InteropLibScope);
            
            
            LibLoaderGetInstance = new MethodReference("get_" + nameof(LibLoader.Instance), InteropLibTypeReference, InteropLibTypeReference);
        }

        public IMetadataScope InteropLibScope { get; }

        public TypeReference InteropLibTypeReference { get; }
        
        public MethodReference LibLoaderGetInstance { get; }

        
        
        private MethodReference GetObjectCtor()
        {
            var typeRef = new TypeReference(nameof(System), nameof(Object), Module, ModuleContext.Netstandard);
            var methodRef = new MethodReference(".ctor", Module.TypeSystem.Void, typeRef);
            methodRef.HasThis = true;
            return methodRef;
        }
        
        public void Process()
        {
            var initMethod = new MethodDefinition("<Init>", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, ModuleContext.Module.TypeSystem.Void);

            var processor = initMethod.Body.GetILProcessor();
            bool processed = false;
            
            LockObject = new FieldDefinition("_<lockObject>", FieldAttributes.Private | FieldAttributes.Static | FieldAttributes.InitOnly, ModuleContext.Module.TypeSystem.Object);
            
            Type.Fields.Add(LockObject);
            
            foreach (var m in Type.Methods.ToArray())
            {
                if (!m.IsStatic)
                    continue;
                if (m.HasBody)
                    continue;
                var methodProcessor = new MethodProcessor(this, m);
                var methodInit = methodProcessor.Process();
                processor.Emit(OpCodes.Call, methodInit);
                processed = true;
            }
            
            processor.Emit(OpCodes.Ret);

            if (processed)
            {
                Type.Methods.Add(initMethod);

                var staticCtor = Type.GetStaticConstructor();

                if (staticCtor == null)
                {
                    staticCtor = new MethodDefinition(".cctor", MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName, ModuleContext.Module.TypeSystem.Void);
                    staticCtor.Body.GetILProcessor().Emit(OpCodes.Ret);
                    
                    Type.Methods.Add(staticCtor);
                }

                var ctorProcessor = staticCtor.Body.GetILProcessor();

                var prepareLibLoader = new MethodReference(nameof(ILibLoader.Prepare), Module.TypeSystem.Void, InteropLibTypeReference);
                prepareLibLoader.HasThis = true;

                var callInitInstruction = ctorProcessor.Create(OpCodes.Call, initMethod);
                
                var createLockObj = ctorProcessor.Create(OpCodes.Newobj, GetObjectCtor());
                
                ctorProcessor.InsertBefore(staticCtor.Body.Instructions.Last(), createLockObj);
                var storeLockObj = ctorProcessor.Create(OpCodes.Stsfld, LockObject);
                ctorProcessor.InsertAfter(createLockObj, storeLockObj);
                
                ctorProcessor.InsertAfter(storeLockObj, callInitInstruction);

                var getLibInst = ctorProcessor.Create(OpCodes.Call, LibLoaderGetInstance);
                
                ctorProcessor.InsertAfter(callInitInstruction, getLibInst);
                ctorProcessor.InsertAfter(getLibInst, ctorProcessor.Create(OpCodes.Callvirt, prepareLibLoader));
            }
        }
    }
}