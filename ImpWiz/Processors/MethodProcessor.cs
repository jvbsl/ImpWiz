using System;
using System.Linq;
using ImpWiz.CecilAttributes;
using ImpWiz.Import;
using ImpWiz.Import.LibLoader;
using ImpWiz.Marshalers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ImpWiz
{
    public class MethodProcessor
    {
        private static string GetMangledFunctionPointer(string methodName) => "_<" + methodName + "_fnptr>";
        private static string GetMangledLazyInitMethod(string methodName) => "<" + methodName + "_Lazy>";
        private static string GetMangledInitMethod(string methodName) => "<Init" + methodName + ">";
        private static string GetMangledLibraryPointer(string libName) => "_<" + libName + ">";
        private static string GetMangledLibraryLoadMethod(string libName) => "<Load_" + libName + ">";

        
        public TypeProcessor TypeContext { get; }

        public TypeDefinition DeclaringType => TypeContext.Type;

        public ModuleDefinition Module => TypeContext.Module;
        public MethodDefinition Method { get; }
        public MethodProcessor(TypeProcessor typeContext, MethodDefinition method)
        {
            TypeContext = typeContext;
            Method = method;
        }
        
        private static Type GetConvention(MethodCallingConvention convention)
        {
            switch (convention)
            {
                case MethodCallingConvention.C:
                    return typeof(System.Runtime.CompilerServices.CallConvCdecl);
                case MethodCallingConvention.Default:
                    return null;
                case MethodCallingConvention.FastCall:
                    return typeof(System.Runtime.CompilerServices.CallConvFastcall);
                case MethodCallingConvention.StdCall:
                    return typeof(System.Runtime.CompilerServices.CallConvStdcall);
                case MethodCallingConvention.ThisCall:
                    return typeof(System.Runtime.CompilerServices.CallConvThiscall);
                default:
                    throw new NotSupportedException();
            }
        }
        
        private MethodDefinition CreateMethod(TypeDefinition declaringType,TypeReference returnType, string name, MethodCallingConvention convention)
        {
            var module = declaringType.Module;
            var convModifier = GetConvention(convention);
            var newReturnType = returnType;
            if (convModifier != null)
                newReturnType = returnType.MakeOptionalModifierType(module.ImportReference(convModifier));
            var m = new MethodDefinition(GetMangledLazyInitMethod(name), MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, newReturnType);

            int returnLocal = -1;
            if (returnType != Module.TypeSystem.Void)
            {
                returnLocal = m.Body.Variables.Count;
                m.Body.Variables.Add(new VariableDefinition(Module.ImportReference(returnType)));
            }
            
            var ilProcessor = m.Body.GetILProcessor();

            var locker = new LockHelper(module, m);
            locker.BeginLock(TypeContext.LockObject);

            CreateLibraryLoadSymbol(m, _mangledName, ilProcessor);

            CreateInteropCall(ilProcessor, FunctionPointer, convention);

            if (returnType != Module.TypeSystem.Void)
            {
                ilProcessor.EmitStloc(returnLocal);
            }

            var endInstruction = (returnType != Module.TypeSystem.Void)
                        ? ilProcessor.CreateLdloc(returnLocal)
                        : ilProcessor.Create(OpCodes.Ret);

            locker.EndLock(endInstruction);
            if (returnType != Module.TypeSystem.Void)
            {
                ilProcessor.Emit(OpCodes.Ret);
            }
            return m;
        }

        private MethodDefinition CreateOrGetLibraryLoadLib(ImportLoaderCecil importLoaderAttribute)
        {
            string libLoaderName = GetMangledLibraryLoadMethod(_libraryName);
            if (TypeContext.LibraryLoaders.TryGetValue(libLoaderName, out var m))
                return m;

            m = new MethodDefinition(libLoaderName, MethodAttributes.Private | MethodAttributes.Static,
                Module.TypeSystem.IntPtr);
            TypeContext.LibraryLoaders.Add(libLoaderName, m);
            TypeContext.Type.Methods.Add(m);
            m.Body.Variables.Add(new VariableDefinition(Module.TypeSystem.IntPtr));
            m.Body.InitLocals = true;


            var processor = m.Body.GetILProcessor();
            var handleField = new FieldDefinition(GetMangledLibraryPointer(_libraryName), FieldAttributes.Private | FieldAttributes.Static,
                Module.TypeSystem.IntPtr);
            TypeContext.Type.Fields.Add(handleField);

            var returnInstruction = processor.Create(OpCodes.Ret);
            processor.Emit(OpCodes.Ldsfld, handleField);
            processor.Emit(OpCodes.Dup);
            processor.EmitStloc(0);
            
            //var libLoaderInstance = new MethodReference("GetInstance" + nameof(LibLoader.Instance), TypeContext.InteropLibTypeReference, TypeContext.InteropLibTypeReference );

            var loadingCode = (importLoaderAttribute.LoaderCookie == null)
                ? processor.Create(OpCodes.Ldnull)
                : processor.Create(OpCodes.Ldstr, importLoaderAttribute.LoaderCookie);
            
            processor.Emit(OpCodes.Brfalse_S, loadingCode);

            processor.EmitLdloc(0);
            processor.Emit(OpCodes.Ret);
            

            processor.Append(loadingCode);
            
            processor.Emit(OpCodes.Call, Module.ImportReference(importLoaderAttribute.GetInstanceMethod));
            processor.Emit(OpCodes.Ldstr, _libraryName);

            
            var libLoaderLoad = new MethodReference(nameof(ICustomLibraryLoader.LoadLibrary), Module.TypeSystem.IntPtr, Module.ImportReference(importLoaderAttribute.GetInstanceMethod.ReturnType) );
            libLoaderLoad.HasThis = true;
            libLoaderLoad.Parameters.Add(new ParameterDefinition(Module.TypeSystem.String));
            // TODO: can't be null -> call?
            processor.Emit(OpCodes.Callvirt, libLoaderLoad);
            processor.Emit(OpCodes.Dup);

            processor.EmitStloc(0);
            var reloadPtr = processor.CreateLdloc(0);
            
            processor.Emit(OpCodes.Brtrue_S, reloadPtr);

            //TODO: perhaps special error handling
            
            processor.Append(reloadPtr);
            processor.Emit(OpCodes.Dup);
            
            processor.Emit(OpCodes.Stsfld, handleField);
            

            processor.Append(returnInstruction);
            return m;
        }

        private ImportLoaderCecil GetImportLoaderAttribute()
        {
            var attr = Method.CustomAttributes.FirstOrDefault(x => x.AttributeType.FullName == nameof(ImpWiz) + "." + nameof(Import) + "." + nameof(ImportLoaderAttribute));
            if (attr == null)
                return new ImportLoaderCecil(TypeContext.ModuleContext);

            var attrValue = attr.CreateInstance<ImportLoaderCecil>(TypeContext.ModuleContext);
            if (attrValue.LibraryLoaderTypeDefinition == null)
                return new ImportLoaderCecil(TypeContext.ModuleContext, attrValue.LibraryLoaderTypeDefinition, attrValue.LoaderCookie);
            return attrValue;
        }

        private void CreateLibraryLoadSymbol(MethodDefinition m, string mangledName, ILProcessor processor)
        {
            int fnPtrLocalIndex = m.Body.Variables.Count;
            m.Body.Variables.Add(new VariableDefinition(Module.TypeSystem.IntPtr));

            var importLoaderAttribute = GetImportLoaderAttribute();

            if (importLoaderAttribute.LoaderCookie == null)
                processor.Emit(OpCodes.Ldnull);
            else
                processor.Emit(OpCodes.Ldstr, importLoaderAttribute.LoaderCookie);
            
            processor.Emit(OpCodes.Call, Module.ImportReference(importLoaderAttribute.GetInstanceMethod));
            
            processor.Emit(OpCodes.Call, CreateOrGetLibraryLoadLib(importLoaderAttribute));
            
            var libLoaderGetProc = new MethodReference(nameof(ICustomLibraryLoader.GetProcAddress), Module.TypeSystem.IntPtr, Module.ImportReference(importLoaderAttribute.GetInstanceMethod.ReturnType));
            libLoaderGetProc.HasThis = true;
            libLoaderGetProc.Parameters.Add(new ParameterDefinition(Module.TypeSystem.IntPtr));
            libLoaderGetProc.Parameters.Add(new ParameterDefinition(Module.TypeSystem.String));

            processor.Emit(OpCodes.Ldstr, _mangledName);
            processor.Emit(OpCodes.Callvirt, libLoaderGetProc);

            processor.Emit(OpCodes.Dup);
            processor.EmitStloc(fnPtrLocalIndex);

            var reloadFnPtr = processor.CreateLdloc(fnPtrLocalIndex);
            
            processor.Emit(OpCodes.Brtrue_S, reloadFnPtr);

            //TODO: perhaps special error handling

            processor.Append(reloadFnPtr);
            processor.Emit(OpCodes.Stsfld, FunctionPointer);
        }

        private void CreateInteropCall(ILProcessor processor, FieldDefinition fnPtr, MethodCallingConvention convention)
        {
            var callSite = new CallSite(Module.ImportReference(Method.ReturnType));
            int index = 0;

            var parameterMarshalers = new (IImpWizMarshaler, VariableDefinition)[Method.Parameters.Count];

            for (int i = 0; i < Method.Parameters.Count;i++)
            {
                var p = Method.Parameters[i];
                callSite.Parameters.Add(p);

                processor.Emit(OpCodes.Ldarg, index++);
                
                IImpWizMarshaler marshaler = MarshalHelper.GetMarshaler(p);

                //var variable = marshaler.CreateManagedAlloc(Method, processor);
                //marshaler.CreateManagedToNative(Method, variable, processor);
                
                parameterMarshalers[i] = (marshaler, null);
            }

            foreach (var c in Method.MethodReturnType.CustomAttributes)
            {
                if (c.AttributeType.Name == "DllImport")
                    continue;
                callSite.MethodReturnType.CustomAttributes.Add(c);
            }

            callSite.CallingConvention = convention;
            
            processor.Emit(OpCodes.Ldsfld, fnPtr);
            processor.Emit(OpCodes.Calli, callSite);
            
            for (int i = 0; i < Method.Parameters.Count;i++)
            {
                //(ICustomMarshaler marshaler, VariableDefinition variable) = parameterMarshalers[i];
                //marshaler?.CreateNativeFree(Method, variable, processor);
            }
            
            //ICustomMarshaler returnMarshaler = MarshalHelper.GetMarshaler(Method.MethodReturnType);
            /*if (returnMarshaler != null)
            {
                var returnVariable = returnMarshaler.CreateManagedAlloc(Method, processor);
                returnMarshaler.CreateNativeToManaged(Method, returnVariable, processor);

                processor.EmitLdloc(returnVariable);
            }*/
        }
        
        private MethodDefinition LazyMethodLoader { get; set; }

        private FieldDefinition FunctionPointer { get; set; }
        

        private MethodDefinition CreateInitMethod()
        {
            var initMethod = new MethodDefinition(GetMangledInitMethod(Method.Name), MethodAttributes.Static | MethodAttributes.Private | MethodAttributes.HideBySig, Module.TypeSystem.Void);

            var processor = initMethod.Body.GetILProcessor();
            if (LazyMethodLoader == null)
            {
                CreateLibraryLoadSymbol(initMethod, _mangledName, processor);
            }
            else
            {
                processor.Emit(OpCodes.Ldftn, LazyMethodLoader);
                processor.Emit(OpCodes.Stsfld, FunctionPointer);
            }
            
            processor.Emit(OpCodes.Ret);

            return initMethod;
        }

        private string _libraryName;
        private string _mangledName;
        
        private MethodCallingConvention _convention = MethodCallingConvention.Default;//TODO: calling convention

        private void FindNames()
        {
            var attr = Method.PInvokeInfo;
            if (attr != null)
            {
                if (attr.IsCallConvCdecl)
                {
                    _convention = MethodCallingConvention.C;
                }
                else if (attr.IsCallConvFastcall)
                {
                    _convention = MethodCallingConvention.FastCall;
                }
                else if (attr.IsCallConvThiscall)
                {
                    _convention = MethodCallingConvention.ThisCall;
                }
                else if (attr.IsCallConvStdCall)
                {
                    _convention = MethodCallingConvention.StdCall;
                }
                else if (attr.IsCallConvWinapi)
                {
                    throw new NotSupportedException();
                }

                _libraryName = attr.Module.Name;
                _mangledName = attr.EntryPoint;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public MethodDefinition Process()
        {
            FindNames();
            var name = Method.Name;
            FunctionPointer = new FieldDefinition(GetMangledFunctionPointer(name), FieldAttributes.Static | FieldAttributes.Private,
                Module.TypeSystem.IntPtr);
            LazyMethodLoader = CreateMethod(DeclaringType, Method.ReturnType, name, _convention);
            DeclaringType.Methods.Add(LazyMethodLoader);
            
            DeclaringType.Fields.Add(FunctionPointer);

            Method.Attributes &= ~(MethodAttributes.PInvokeImpl | MethodAttributes.FamANDAssem);
            Method.Attributes |= MethodAttributes.Public;

            var ilProcessor = Method.Body.GetILProcessor();

            CreateInteropCall(ilProcessor, FunctionPointer, _convention);
            ilProcessor.Emit(OpCodes.Ret);

            var initMethod = CreateInitMethod();
            DeclaringType.Methods.Add(initMethod);
            return initMethod;
        }
    }
}