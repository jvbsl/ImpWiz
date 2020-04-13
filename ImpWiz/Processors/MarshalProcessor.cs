using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using ImpWiz.Import.Marshalers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ImpWiz.Processors
{
    public class MarshalProcessor
    {
        private static readonly Dictionary<string, Func<MarshalProcessor, object>> Properties;
        static MarshalProcessor()
        {
            Properties = new Dictionary<string, Func<MarshalProcessor, object>>();
            foreach (var prop in typeof(MarshalProcessor).GetProperties())
            {
                if (prop.GetMethod == null)
                    continue;
                var thisParam = Expression.Parameter(typeof(MarshalProcessor));
                UnaryExpression castToObject = Expression.Convert(Expression.Call(thisParam, prop.GetMethod), typeof(object));
                var getterCall = Expression.Lambda<Func<MarshalProcessor, object>>(castToObject, thisParam).Compile();
                Properties.Add(prop.Name, getterCall);
            }
        }
        public TypeDefinition Marshaler { get; }
        
        public ParameterDefinition Parameter { get; }
        
        public MethodReturnType ReturnParameter { get; }
        public PInvokeInfo PInvokeInfo { get; }
        
        public MethodProcessor MethodProcessor { get; }
        
        public TypeDefinition MarshalInfoVariableType { get; }
        
        public TypeDefinition NativeType { get; }
        public TypeDefinition ManagedType { get; }
        
        public Instruction CleanUpInstruction { get; set; }

        public CharSet CharSet
        {
            get
            {
                if (PInvokeInfo.IsCharSetAnsi)
                    return CharSet.Ansi;
                if (PInvokeInfo.IsCharSetAuto)
                    return CharSet.Auto;
                if (PInvokeInfo.IsCharSetUnicode)
                    return CharSet.Unicode;
                if (PInvokeInfo.IsCharSetNotSpec)
                    return CharSet.None;
                throw new NotSupportedException("No charset given in PInvokeInfo.");
            }
        }
        
        public UnmanagedType UnmanagedType { get; }
        
        private static GenericInstanceType GetMarshalerBaseClass(TypeDefinition typeDef)
        {
            if (typeDef.BaseType == null)
                return null;
            var baseType = typeDef.BaseType;
            if (baseType.Namespace == "ImpWiz.Import.Marshalers" && baseType.Name == "ImpWizMarshaler`4" &&
                baseType.IsGenericInstance && ((GenericInstanceType)baseType).GenericArguments.Count == 4)
                return ((GenericInstanceType)baseType);
            return GetMarshalerBaseClass(baseType.Resolve());
        }

        protected MarshalProcessor(MarshalerType marshalerType, MethodProcessor methodProcessor,ParameterDefinition param, MethodReturnType retParam)
        {
            if (param == null && retParam == null)
                throw new ArgumentNullException("either '" + nameof(param) + "' or '" + nameof(retParam) + "' must not be null");
            if (param != null && retParam != null)
                throw new InvalidOperationException("either '" + nameof(param) + "' or '" + nameof(retParam) + "' must be null");
            MethodProcessor = methodProcessor;
            Marshaler = marshalerType.TypeDefinition;
            ReturnParameter = retParam;
            Parameter = param;
            PInvokeInfo = methodProcessor.Method.PInvokeInfo;
            if (param != null)
                UnmanagedType = (UnmanagedType)param.MarshalInfo.NativeType;
            else
                UnmanagedType = (UnmanagedType)retParam.MarshalInfo.NativeType;
            
            var supportedUnmanagedTypes = new HashSet<UnmanagedType>();
            
            if (!marshalerType.SupportedUnmanagedTypes.Contains(UnmanagedType))
                throw new NotSupportedException($"UnmanagedType: '{UnmanagedType}' not supported by {Marshaler.FullName} marshaler for {methodProcessor.Method.FullName}.");

            var marshalerBaseClass = GetMarshalerBaseClass(Marshaler);

            NativeType = marshalerBaseClass.GenericArguments[2].Resolve();
            ManagedType = marshalerBaseClass.GenericArguments[3].Resolve();

            MarshalInfoVariableType = marshalerBaseClass.GenericArguments[1].Resolve();
            
            MethodDefinition fittingCtor = null;

            static string GetParameterInitReference(ParameterDefinition parameter)
            {
                foreach (var ca in parameter.CustomAttributes)
                {
                    if (ca.AttributeType.Namespace == "ImpWiz.Import.Marshalers" &&
                        ca.AttributeType.Name == "MarshalerInfoInitializationAttribute")
                    {
                        return (string) ca.ConstructorArguments[0].Value;
                    }
                }

                return null;
            }

            object GetObjectProperty(object obj, string target)
            {
                if (obj == null)
                    throw new ArgumentNullException(nameof(obj));
                
                var tSplit = target.IndexOf('.');
                var propName = tSplit == -1 ? target : target.Substring(0, tSplit);

                object propValue;
                if (obj == this)
                {
                    if (!Properties.TryGetValue(propName, out var propGetter))
                        throw new NotSupportedException($"{obj.GetType()} does not contain a {propName} property!");
                    propValue = propGetter(this);
                }
                else
                {
                    var prop = obj.GetType().GetProperty(propName);
                    if (prop == null)
                        throw new NotSupportedException($"{obj.GetType()} does not contain a {propName} property!");
                    propValue = prop.GetValue(obj);
                }

                if (tSplit == -1)
                    return propValue;
                return GetObjectProperty(propValue, target.Substring(tSplit + 1));

            }

            var arguments = new List<object>();
            foreach (var ctor in MarshalInfoVariableType.GetConstructors()
                .OrderBy(x => -x.Parameters.Count))
            {
                arguments.Clear();
                bool fits = true;
                foreach (var p in ctor.Parameters)
                {
                    var target = GetParameterInitReference(p);

                    try
                    {
                        arguments.Add(GetObjectProperty(this, target)); // TODO: more complex ctor arguments
                    }
                    catch
                    {
                        fits = false;
                        break;
                    }
                }

                if (fits)
                {
                    fittingCtor = ctor;
                    break;
                }
            }
            if (fittingCtor == null)
                throw new NotSupportedException($"Can't create {marshalerBaseClass.GenericArguments[1].FullName} marshaler info type for {methodProcessor.Method.FullName}.");

            _fittingCtor = (fittingCtor, arguments);
        }
        public MarshalProcessor(MarshalerType marshalerType, MethodProcessor methodProcessor, MethodReturnType parameter)
            : this(marshalerType, methodProcessor, null, parameter)
        {

        }
        public MarshalProcessor(MarshalerType marshalerType, MethodProcessor methodProcessor, ParameterDefinition parameter)
            : this(marshalerType, methodProcessor, parameter, null)
        {
        }

        private (MethodDefinition ctor, List<object> arguments) _fittingCtor;

        private VariableDefinition _marshalInfoVariable;
        private VariableDefinition _marshalReturnVariable;
        
        public void InitializeMarshalInfo(ILProcessor processor)
        {
            CleanUpInstruction = null;
            _marshalInfoVariable = new VariableDefinition(MethodProcessor.Module.ImportReference(MarshalInfoVariableType));
            processor.Body.Variables.Add(_marshalInfoVariable);
            
            
            processor.EmitLdloca(_marshalInfoVariable);
            foreach (var a in _fittingCtor.arguments)
            {
                var paramType = a.GetType();
                if (paramType.IsEnum)
                {
                    var intSize = Marshal.SizeOf(Enum.GetUnderlyingType(paramType));
                    switch (intSize)
                    {
                        case 1:
                        case 2:
                        case 4:
                            processor.EmitLdc_4((int)a);
                            break;
                        case 8:
                            processor.Emit(OpCodes.Ldc_I8, (long)a);
                            break;
                    }
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
            processor.Emit(OpCodes.Call, MethodProcessor.Module.ImportReference(_fittingCtor.ctor));
            
        }

        public void MarshalManaged(ILProcessor processor, Instruction insertPos)
        {
            var lastInstr = processor.Create(OpCodes.Nop);

            var marshalManagedMeth =
                Marshaler.Methods.First(x => x.IsVirtual && (x.IsNewSlot || x.IsReuseSlot) && x.Name == "MarshalManaged");


            int variableOffset = processor.Body.Variables.Count;
            foreach (var v in marshalManagedMeth.Body.Variables)
            {
                processor.Body.Variables.Add(new VariableDefinition(ResolveTypeRef(v.VariableType, processor.Body.Method.Module, null, marshalManagedMeth.Module)));
            }

            marshalManagedMeth.Body.SimplifyMacros();
            var instructions = new List<Instruction>();
            var firstInstr = CloneInstructionTree(marshalManagedMeth.Body.Instructions[0], marshalManagedMeth, processor.Body.Method, instructions, true, variableOffset, lastInstr);

            foreach (var i in instructions)
            {
                if (insertPos == null)
                    processor.Append(i);
                else
                    processor.InsertAfter(insertPos, i);
                insertPos = i;
            }
            
            processor.Append(lastInstr);
        }
        private static MethodReference ResolveMethodReference(MethodReference method, ModuleDefinition currentModule, ModuleReference originalModule)
        {
            TypeReference declaringType = ResolveTypeRef(method.DeclaringType, currentModule, null, originalModule);
            if (declaringType.IsGenericInstance)
                declaringType = ResolveTypeRef(((GenericInstanceType) declaringType).ElementType.Resolve(), currentModule, null, originalModule);
            var resolved = method.Resolve();
            var newMethod = resolved.Module == originalModule ? resolved.Clone((TypeDefinition)declaringType) : currentModule.ImportReference(method);


            if (method.DeclaringType.IsGenericInstance)
            {
                //newMethod.DeclaringType = ;
                var resRet = currentModule.ImportReference(ResolveTypeRef(method.ReturnType, currentModule, newMethod, originalModule), newMethod);
                var methRef = new MethodReference(newMethod.Name, resRet,
                    ResolveTypeRef(method.DeclaringType, currentModule, newMethod, originalModule));
                methRef.CallingConvention = newMethod.CallingConvention;
                foreach (var p in newMethod.Parameters)
                {
                    methRef.Parameters.Add(p);
                }
                methRef.HasThis = method.HasThis;
                methRef.ExplicitThis = method.ExplicitThis;
                return currentModule.ImportReference(methRef, newMethod);
            }


            return newMethod;
        }
        private static TypeReference ImportGeneric(ModuleDefinition module, TypeReference typeReference, IGenericParameterProvider tmpOwner)
        {
            if (typeReference.IsGenericParameter)
            {
                var gp = ((GenericParameter) typeReference);
                var owner = gp.Owner;
                if (owner is TypeDefinition ownerTd)
                {
                    var newOwner = ownerTd.Clone(module);
                    return module.ImportReference(gp.Clone(newOwner), newOwner);
                }
                else if (owner is MethodDefinition ownerMd)
                {
                    var newOwner = ResolveMethodReference(ownerMd, module, owner.Module).Resolve();
                    return module.ImportReference(gp.Clone(newOwner), newOwner);
                }

                throw new NotSupportedException();
            }

            if (!typeReference.IsGenericInstance)
            {
                return ResolveTypeRef(typeReference, module, tmpOwner, typeReference.Module);
            }

            var gi = (GenericInstanceType) typeReference;

            TypeReference type = ImportGeneric(module, gi.ElementType, tmpOwner);
            type = type.MakeGenericInstanceType(gi.GenericArguments.Select(
                x =>
                {
                    if (x.IsGenericParameter)
                        return ImportGeneric(module, x, tmpOwner);
                    return ResolveTypeRef(x, module, tmpOwner, typeReference.Module);
                }).ToArray());//gi.Clone(module);
            /*var newGi = new GenericInstanceType(type);
            foreach (var a in gi.GenericArguments)
            {
                newGi.GenericArguments.Add(ImportGeneric(module, a, tmpOwner));
            }*/

            return module.ImportReference(type, tmpOwner);
        }
        public static TypeReference ResolveTypeRef(TypeReference typeReference, ModuleDefinition newModule, IGenericParameterProvider context, ModuleReference originalLib)
        {
            var resolved = typeReference.Resolve();
            
            if (typeReference.IsOptionalModifier)
            {
                var optMod = (OptionalModifierType) typeReference;
                return new OptionalModifierType(ResolveTypeRef(optMod.ModifierType, newModule, context, originalLib), ResolveTypeRef(optMod.ElementType, newModule, context, originalLib));
            }

            if (typeReference.IsRequiredModifier)
            {
                var reqMod = (RequiredModifierType) typeReference;
                return new RequiredModifierType(ResolveTypeRef(reqMod.ModifierType, newModule, context, originalLib), ResolveTypeRef(reqMod.ElementType, newModule, context, originalLib));

            }

            if (typeReference.IsByReference)
            {
                var reqMod = (ByReferenceType) typeReference;
                return new ByReferenceType(ResolveTypeRef(reqMod.ElementType, newModule, context, originalLib));
            }
            
            if (resolved == null)
                return typeReference;
            
            if ((!typeReference.IsDefinition || originalLib == resolved.Module) && resolved.Module == typeReference.Module)
            {
                if (typeReference.IsGenericInstance)
                {
                    return ImportGeneric(newModule, typeReference, context);
                }
                if (typeReference == resolved)
                    return ((TypeDefinition) typeReference).Clone(newModule);
            }

            if (typeReference.IsDefinition)
                return typeReference;
            return newModule.ImportReference(typeReference, context);
        }
        private Instruction CloneInstructionTree(Instruction instruction, MethodDefinition originalMethod, MethodDefinition currentMethod, List<Instruction> instructions, bool replaceParams, int variableOffset, Instruction callInstruction)
        {
            for (int i = 0; i < instructions.Count; i++)
            {
                var testInstruction = instructions[i];
                if (testInstruction.Offset > instruction.Offset)
                    break;
                if (testInstruction.Offset == instruction.Offset)
                {
                    if (testInstruction.OpCode == instruction.OpCode && 
                        (testInstruction.Operand != null && instruction.Operand != null && testInstruction.Operand.GetType() == instruction.Operand.GetType())
                        || (testInstruction.Operand == instruction.Operand && testInstruction.Operand == null))
                        return testInstruction;
                    break;
                }
            }
                

            bool isCleanupCode = false;
            Instruction newInstr;
            switch (instruction.Operand)
            {
                case FieldReference field:
                {
                    TypeReference declaringType = ResolveTypeRef(field.DeclaringType, currentMethod.Module, null, originalMethod.Module);
                    var resolved = field.Resolve();
                    var newField = resolved.Module == originalMethod.Module ? resolved.Clone(declaringType) : currentMethod.Module.ImportReference(field);
                    newInstr = Instruction.Create(instruction.OpCode, newField);
                    break;
                }
                case Instruction target:
                    var newTarget = CloneInstructionTree(target, originalMethod, currentMethod, instructions, replaceParams, variableOffset, callInstruction);
                    newInstr = Instruction.Create(instruction.OpCode, newTarget);
                    break;
                case Instruction[] targets:
                    var newTargets = targets.Select(x => CloneInstructionTree(x, originalMethod, currentMethod, instructions, replaceParams, variableOffset, callInstruction)).ToArray();
                    newInstr = Instruction.Create(instruction.OpCode, newTargets);
                    break;
                case null:
                    newInstr = Instruction.Create(instruction.OpCode);
                    break;
                case MethodReference method:
                {
                    if (method.Name == "ObjectInitialized" &&
                        method.DeclaringType.Namespace == "ImpWiz.Import.Marshalers" &&
                        method.DeclaringType.Name == "MarshalInitialization`2")
                    {
                        isCleanupCode = true;
                        newInstr = Instruction.Create(OpCodes.Nop);
                    }
                    else
                    {
                        var newMethod = ResolveMethodReference(method, currentMethod.Module, originalMethod.Module);
                        newInstr = Instruction.Create(instruction.OpCode, newMethod);
                    }
                    break;
                }
                case VariableDefinition variable:
                {
                    var newVariable = currentMethod.Body.Variables[variable.Index + variableOffset];
                    newInstr = Instruction.Create(instruction.OpCode, newVariable);
                    break;
                }
                case ParameterDefinition parameter:
                {
                    var newMethod = ResolveMethodReference((MethodReference)parameter.Method, currentMethod.Module, originalMethod.Module);
                    if (replaceParams)
                    {
                        int pIndex = originalMethod.Parameters.IndexOf(parameter);
                        if (parameter.Index == -1)
                            pIndex = pIndex;
                        if (pIndex == -1)
                        {
                            throw new NotSupportedException("this access not supported!");
                        }
                        else if (pIndex == 0)
                        {
                            if (instruction.OpCode == OpCodes.Ldarga || instruction.OpCode == OpCodes.Ldarga_S)
                                newInstr = Instruction.Create(OpCodes.Ldloca, _marshalInfoVariable);
                            else if (instruction.OpCode == OpCodes.Ldarg || instruction.OpCode == OpCodes.Ldarg_S 
                                    || instruction.OpCode == OpCodes.Ldarg_0 || instruction.OpCode == OpCodes.Ldarg_1
                                    || instruction.OpCode == OpCodes.Ldarg_2 || instruction.OpCode == OpCodes.Ldarg_3)
                                newInstr = Instruction.Create(OpCodes.Ldloc, _marshalInfoVariable);
                            else
                            {
                                throw new NotSupportedException();
                            }
                            break;
                        }
                        else if (pIndex == 1)
                        {
                            if (Parameter != null)
                                newInstr = Instruction.Create(instruction.OpCode, Parameter);
                            else
                            {
                                if (instruction.OpCode == OpCodes.Ldarga || instruction.OpCode == OpCodes.Ldarga_S)
                                    newInstr = Instruction.Create(OpCodes.Ldloca,_marshalReturnVariable);
                                else if (instruction.OpCode == OpCodes.Ldarg || instruction.OpCode == OpCodes.Ldarg_S 
                                                                             || instruction.OpCode == OpCodes.Ldarg_0 || instruction.OpCode == OpCodes.Ldarg_1
                                                                             || instruction.OpCode == OpCodes.Ldarg_2 || instruction.OpCode == OpCodes.Ldarg_3)
                                    newInstr = Instruction.Create(OpCodes.Ldloc,_marshalReturnVariable);
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            break;
                        }
                    }
                    newInstr = Instruction.Create(instruction.OpCode, newMethod.Parameters.First(x => x.Name == parameter.Name));
                    break;
                }
                case TypeReference type:
                {
                    TypeReference newType;
                    if (type.IsGenericInstance || type.IsGenericParameter)
                        newType = ImportGeneric(currentMethod.Module, type, currentMethod);
                    else
                        newType = ResolveTypeRef(type, currentMethod.Module, null, originalMethod.Module);
                    newInstr = Instruction.Create(instruction.OpCode, newType);
                    break;
                }
                case string strValue:
                    newInstr = Instruction.Create(instruction.OpCode, strValue);
                    break;
                case SByte sbyteValue:
                    newInstr = Instruction.Create(instruction.OpCode, sbyteValue);
                    break;
                case int intValue:
                    newInstr = Instruction.Create(instruction.OpCode, intValue);
                    break;
                case double doubleValue:
                    newInstr = Instruction.Create(instruction.OpCode, doubleValue);
                    break;
                case float floatValue:
                    newInstr = Instruction.Create(instruction.OpCode, floatValue);
                    break;
                default:
                    throw new NotImplementedException();
            }

            if (instruction.OpCode == OpCodes.Ret)
            {
                newInstr = Instruction.Create(OpCodes.Nop);
            }
            
            newInstr.Offset = instruction.Offset;

            int insertPos;
            for (insertPos = 0; insertPos < instructions.Count; insertPos++)
            {
                if (instructions[insertPos].Offset > newInstr.Offset)
                    break;
            }
            instructions.Insert(insertPos, newInstr);

            if (instruction.Next != null)
            {
                var followingInstructions = CloneInstructionTree(instruction.Next, originalMethod, currentMethod, instructions, replaceParams, variableOffset, callInstruction);
                if (isCleanupCode)
                    CleanUpInstruction = followingInstructions;
                newInstr.Next = followingInstructions;
            }
            return newInstr;
        }

        public void MarshalNative(ILProcessor processor, Instruction cleanupInstructions)
        {
            var retVar = new VariableDefinition(NativeType);
            processor.Body.Variables.Add(retVar);
            _marshalReturnVariable = retVar;
            if (cleanupInstructions == null)
                processor.Append(Instruction.Create(OpCodes.Stloc, retVar));
            else
                processor.InsertBefore(cleanupInstructions, Instruction.Create(OpCodes.Stloc, retVar));
            
            var lastInstr = cleanupInstructions;

            var marshalManagedMeth =
                Marshaler.Methods.First(x => x.IsVirtual && (x.IsNewSlot || x.IsReuseSlot) && x.Name == "MarshalNative");

            int variableOffset = processor.Body.Variables.Count;
            foreach (var v in marshalManagedMeth.Body.Variables)
            {
                processor.Body.Variables.Add(new VariableDefinition(ResolveTypeRef(v.VariableType, processor.Body.Method.Module, null, marshalManagedMeth.Module)));
            }

            marshalManagedMeth.Body.SimplifyMacros();
            var instructions = new List<Instruction>();
            var firstInstr = CloneInstructionTree(marshalManagedMeth.Body.Instructions[0], marshalManagedMeth, processor.Body.Method, instructions,true, variableOffset, lastInstr);
            Instruction insertPos = cleanupInstructions;
            foreach (var i in instructions)
            {
                if (insertPos == null)
                    processor.Append(i);
                else
                    processor.InsertAfter(insertPos, i);
                insertPos = i;
            }
        }
    }
}