using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ImpWiz
{
    internal static class CecilCloner
    {
        public static TypeDefinition Clone(this TypeDefinition typeDefinition, ModuleDefinition newModule)
        {
            var alreadyExistingType = newModule.Types.FirstOrDefault(x =>
                x.Namespace == typeDefinition.Namespace && x.Name == typeDefinition.Name);
            if (alreadyExistingType != null)
                return alreadyExistingType;
            var originalModule = typeDefinition.Module;
            var baseTypeRef = typeDefinition.BaseType;

            
            var newType = new TypeDefinition(typeDefinition.Namespace, typeDefinition.Name, typeDefinition.Attributes, baseTypeRef);
            newModule.Types.Add(newType);
            
            if (baseTypeRef != null)
                newType.BaseType = ResolveTypeRef(typeDefinition.BaseType, newModule, newType, typeDefinition.Module);

            
            foreach (var cA in typeDefinition.CustomAttributes)
            {
                newType.CustomAttributes.Add(cA.Clone(newType.Module, typeDefinition.Module));
            }
            foreach (var gP in typeDefinition.GenericParameters)
            {
                gP.Clone(newType);
            }
            if (typeDefinition.BaseType != null)
            {
                baseTypeRef =
                    ImportGeneric(newModule, typeDefinition.BaseType,
                        newType);
                newType.BaseType = baseTypeRef;
            }
            foreach(var nestedType in typeDefinition.NestedTypes)
            {
                newType.NestedTypes.Add(nestedType.Clone(newModule));
            }


            HashSet<InterfaceImplementation> nonTopLevelInterfaces = new HashSet<InterfaceImplementation>();
            for (int i = 0; i < typeDefinition.Interfaces.Count; i++)
            {
                var interface1 = typeDefinition.Interfaces[i];
                var interface1Type = interface1.InterfaceType.Resolve();
                for (int j = i + 1; j < typeDefinition.Interfaces.Count; j++)
                {
                    var interface2 = typeDefinition.Interfaces[j];
                    var interface2Type = interface2.InterfaceType.Resolve();
                    if (interface1Type.Interfaces.Any(x => x.InterfaceType == interface2.InterfaceType))
                    {
                        nonTopLevelInterfaces.Add(interface2);
                    }
                    else if (interface2Type.Interfaces.Any(x => x.InterfaceType == interface1.InterfaceType))
                    {
                        nonTopLevelInterfaces.Add(interface1);
                    }
                }
            }
            
            foreach(var inter in typeDefinition.Interfaces)
            {
                //if (nonTopLevelInterfaces.Contains(inter))
                //    continue;
                TypeReference resolved = ResolveTypeRef(inter.InterfaceType, newModule, newType, typeDefinition.Module);
                if (inter.InterfaceType.IsGenericParameter || inter.InterfaceType.IsGenericInstance)
                {
                    resolved = ImportGeneric(newModule, inter.InterfaceType, newType);
                }
                
                newType.Interfaces.Add(new InterfaceImplementation(resolved));
            }

            foreach (var field in typeDefinition.Fields)
            {
                field.Clone(newType);
            }

            foreach (var method in typeDefinition.Methods)
            {
                method.Clone(newType);
            }

            foreach (var prop in typeDefinition.Properties)
            {
                prop.Clone(newType);
            }

            for(int i=0;i<typeDefinition.Methods.Count;i++)
            {
                var oldMethod = typeDefinition.Methods[i];
                var newMethod = newType.Methods[i];
                oldMethod.Body?.Clone(newMethod);
            }

            return newType;
            
        }

        public static GenericParameter Clone(this GenericParameter gp, TypeDefinition typeDefinition)
        {
            var alreadyExistingGp = typeDefinition.GenericParameters.FirstOrDefault(x => x.Name == gp.Name);
            if (alreadyExistingGp != null)
                return alreadyExistingGp;
            var newParam = new GenericParameter(gp.Name, typeDefinition);
            newParam.Attributes = gp.Attributes;
            foreach (var constraint in gp.Constraints)
            {
                constraint.Clone(newParam, typeDefinition);
            }

            foreach (var ca in gp.CustomAttributes)
            {
                newParam.CustomAttributes.Add(ca.Clone(newParam.Module, gp.Module));
            }
            
            typeDefinition.GenericParameters.Insert(Math.Min(typeDefinition.GenericParameters.Count, gp.Position), newParam);

            return newParam;
        }

        public static GenericParameterConstraint Clone(this GenericParameterConstraint constraint, GenericParameter parameter, TypeDefinition typeDefinition)
        {
            var newConstraint =
                new GenericParameterConstraint(ImportGeneric(typeDefinition.Module, constraint.ConstraintType,
                    typeDefinition));
            parameter.Constraints.Add(newConstraint);
            foreach (var ca in constraint.CustomAttributes)
            {
                newConstraint.CustomAttributes.Add(ca.Clone(parameter.Module, constraint.ConstraintType.Module));
            }
            return newConstraint;
        }
        public static GenericParameter Clone(this GenericParameter gp, MethodDefinition methodDefinition)
        {
            var alreadyExistingGp = methodDefinition.GenericParameters.FirstOrDefault(x => x.Name == gp.Name);
            if (alreadyExistingGp != null)
                return alreadyExistingGp;
            var newParam = new GenericParameter(gp.Name, methodDefinition);
            
            methodDefinition.GenericParameters.Insert(Math.Min(methodDefinition.GenericParameters.Count, gp.Position), newParam);
            return newParam;
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

        public static PropertyDefinition Clone(this PropertyDefinition prop, TypeDefinition newType)
        {
            var alreadyExistingProp = newType.Properties.FirstOrDefault(x => x.Name == prop.Name);
            if (alreadyExistingProp != null)
                return alreadyExistingProp;
            TypeReference type = ResolveTypeRef(prop.PropertyType, newType.Module, newType, prop.Module);
            var newProp = new PropertyDefinition(prop.Name, prop.Attributes, type);
            foreach (var cA in prop.CustomAttributes)
            {
                newProp.CustomAttributes.Add(cA.Clone(newProp.Module, prop.Module));
            }
            newType.Properties.Add(newProp);
            newProp.Constant = prop.Constant;
            newProp.HasDefault = prop.HasDefault;
            newProp.HasConstant = prop.HasConstant;
            if (prop.GetMethod != null)
                newProp.GetMethod = ResolveMethodReference(prop.GetMethod, newType.Module, prop.Module).Resolve();
            if (prop.SetMethod != null)
                newProp.SetMethod = ResolveMethodReference(prop.SetMethod, newType.Module, prop.Module).Resolve();
            return newProp;
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

        public static FieldReference Clone(this FieldDefinition field, TypeReference newTypeReference)
        {
            TypeDefinition newType;
            if (newTypeReference.IsGenericInstance)
                newType = (TypeDefinition)((GenericInstanceType) newTypeReference).ElementType;
            else
            {
                newType = (TypeDefinition) newTypeReference;
            }
            var alreadyExistingField = newType.Fields.FirstOrDefault(x =>
                x.Name == field.Name);
            if (alreadyExistingField != null)
                return new FieldReference(alreadyExistingField.Name,alreadyExistingField.FieldType, newTypeReference);
            TypeReference type = ResolveTypeRef(field.FieldType, newType.Module, newType, field.Module);
            var newField = new FieldDefinition(field.Name, field.Attributes, type);
            newType.Fields.Add(newField);
            foreach (var cA in field.CustomAttributes)
            {
                newField.CustomAttributes.Add(cA.Clone(newField.Module, field.Module));
            }
            newField.Constant = field.Constant;
            newField.InitialValue = field.InitialValue;
            newField.HasDefault = field.HasDefault;
            newField.HasConstant = field.HasConstant;
            return new FieldReference(newField.Name,newField.FieldType, newTypeReference);
        }

        private static ModuleReference GetOrAddModuleReference(ModuleDefinition module, string name)
        {
            var alreadyExistingModule = module.ModuleReferences.FirstOrDefault(x => x.Name == name);
            if (alreadyExistingModule != null)
                return alreadyExistingModule;
            var newModule = new ModuleReference(name);
            module.ModuleReferences.Add(newModule);
            return newModule;
        }
        public static MethodDefinition Clone(this MethodDefinition method, TypeDefinition newType)
        {
            var alreadyExistingMethod = newType.Methods.FirstOrDefault(x =>
                x.Name == method.Name && (x.Parameters.Count == method.Parameters.Count && x.Parameters.Zip(method.Parameters, (p1, p2) => p1.ParameterType.Namespace == p2.ParameterType.Namespace && p1.ParameterType.Name == p2.ParameterType.Name).All(same=> same)));
            if (alreadyExistingMethod != null)
                return alreadyExistingMethod;
            TypeReference retType = ResolveTypeRef(method.MethodReturnType.ReturnType, newType.Module, newType, method.Module);
            
            var newMethod = new MethodDefinition(method.Name, method.Attributes, retType);

            newType.Methods.Add(newMethod);
            foreach (var p in method.Parameters)
            {
                TypeReference pType = p.ParameterType.Resolve();
                if (p.ParameterType.IsGenericParameter)
                    pType = p.ParameterType;
                else
                    pType = ResolveTypeRef(p.ParameterType, newType.Module, newMethod, method.Module);
                var newParam = new ParameterDefinition(p.Name, p.Attributes, pType);
                newParam.Constant = p.Constant;
                newParam.HasConstant = p.HasConstant;
                newParam.HasDefault = p.HasDefault;
                foreach(var ca in p.CustomAttributes)
                    newParam.CustomAttributes.Add(ca.Clone(newMethod.Module, method.Module));
                newMethod.Parameters.Add(newParam);
            }

            foreach (var gP in method.GenericParameters)
            {
                gP.Clone(newMethod);
            }
            
            newMethod.ImplAttributes = method.ImplAttributes;
            newMethod.SemanticsAttributes = method.SemanticsAttributes;
            newMethod.HasThis = method.HasThis;
            newMethod.ExplicitThis = method.ExplicitThis;
            if (method.PInvokeInfo != null)
            {
                newMethod.PInvokeInfo = new PInvokeInfo(method.PInvokeInfo.Attributes, method.PInvokeInfo.EntryPoint,
                    GetOrAddModuleReference(newType.Module, method.PInvokeInfo.Module.Name));
            }

            foreach (var cA in method.CustomAttributes)
            {
                newMethod.CustomAttributes.Add(cA.Clone(newType.Module, method.Module));
            }


            

            return newMethod;
        }

        public static CustomAttribute Clone(this CustomAttribute attribute, ModuleDefinition newModule, ModuleReference originalModule)
        {
            var method = ResolveMethodReference(attribute.Constructor, newModule, originalModule);

            var newCa = new CustomAttribute(method, attribute.GetBlob());
            foreach (var arg in attribute.ConstructorArguments)
            {
                TypeReference type = ResolveTypeRef(arg.Type, newModule, null, originalModule);
                newCa.ConstructorArguments.Add(new CustomAttributeArgument(type, arg.Value));
            }

            foreach (var f in attribute.Fields)
            {
                TypeReference type = ResolveTypeRef(f.Argument.Type, newModule, null, originalModule);
                newCa.Fields.Add(new CustomAttributeNamedArgument(f.Name, new CustomAttributeArgument(type, f.Argument.Value)));
            }
            foreach (var p in attribute.Properties)
            {
                TypeReference type = ResolveTypeRef(p.Argument.Type, newModule, null, originalModule);
                newCa.Fields.Add(new CustomAttributeNamedArgument(p.Name, new CustomAttributeArgument(type, p.Argument.Value)));
            }

            return newCa;
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

        public static void Clone(this MethodBody methodBody, MethodDefinition method)
        {
            var newMethodBody = new MethodBody(method);
            newMethodBody.InitLocals = methodBody.InitLocals;
            newMethodBody.MaxStackSize = methodBody.MaxStackSize;
            foreach (var variable in methodBody.Variables)
            {
                variable.Clone(methodBody.Method, newMethodBody);
            }

            if (methodBody.Instructions.Count > 0)
            {
                methodBody.Instructions[0].CloneInstructionTree(methodBody.Method, newMethodBody);
            }

            method.Body = newMethodBody;
        }

        public static Instruction CloneInstructionTree(this Instruction instruction,MethodDefinition originalMethod, MethodBody methodBody)
        {
            for (int i = 0; i < methodBody.Instructions.Count; i++)
            {
                if (methodBody.Instructions[i].Offset > instruction.Offset)
                    break;
                if (methodBody.Instructions[i].Offset == instruction.Offset)
                    return methodBody.Instructions[i];
            }
            Instruction newInstr;
            switch (instruction.Operand)
            {
                case FieldReference field:
                {
                    TypeReference declaringType = ResolveTypeRef(field.DeclaringType, methodBody.Method.Module, null, originalMethod.Module);
                    var resolved = field.Resolve();
                    var newField = resolved.Module == originalMethod.Module ? resolved.Clone(declaringType) : methodBody.Method.Module.ImportReference(field);
                    newInstr = Instruction.Create(instruction.OpCode, newField);
                    break;
                }
                case Instruction target:
                    var newTarget = target.CloneInstructionTree(originalMethod, methodBody);
                    newInstr = Instruction.Create(instruction.OpCode, newTarget);
                    break;
                case Instruction[] targets:
                    var newTargets = targets.Select(x => x.CloneInstructionTree(originalMethod, methodBody)).ToArray();
                    newInstr = Instruction.Create(instruction.OpCode, newTargets);
                    break;
                case null:
                    newInstr = Instruction.Create(instruction.OpCode);
                    break;
                case MethodReference method:
                {
                    var newMethod = ResolveMethodReference(method, methodBody.Method.Module, originalMethod.Module);
                    newInstr = Instruction.Create(instruction.OpCode, newMethod);
                    break;
                }
                case VariableDefinition variable:
                {
                    var newVariable = variable.Clone(originalMethod, methodBody);
                    newInstr = Instruction.Create(instruction.OpCode, newVariable);
                    break;
                }
                case ParameterDefinition parameter:
                {
                    var newMethod = ResolveMethodReference((MethodReference)parameter.Method, methodBody.Method.Module, originalMethod.Module);
                    
                    newInstr = Instruction.Create(instruction.OpCode, newMethod.Parameters.First(x => x.Name == parameter.Name));
                    break;
                }
                case TypeReference type:
                {
                    TypeReference newType;
                    if (type.IsGenericInstance || type.IsGenericParameter)
                        newType = ImportGeneric(methodBody.Method.Module, type, methodBody.Method);
                    else
                        newType = ResolveTypeRef(type, methodBody.Method.Module, null, originalMethod.Module);
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
            newInstr.Offset = instruction.Offset;

            int insertPos;
            for (insertPos = 0; insertPos < methodBody.Instructions.Count; insertPos++)
            {
                if (methodBody.Instructions[insertPos].Offset > newInstr.Offset)
                    break;
            }
            methodBody.Instructions.Insert(insertPos, newInstr);

            if (instruction.Next != null)
                newInstr.Next = instruction.Next.CloneInstructionTree(originalMethod, methodBody);
            return newInstr;
        }

        public static VariableDefinition Clone(this VariableDefinition variable, MethodDefinition originalMethod, MethodBody methodBody)
        {
            var alreadyExistingVariable = methodBody.Variables.FirstOrDefault(x =>
                x.Index == variable.Index);
            if (alreadyExistingVariable != null)
                return alreadyExistingVariable;
            TypeReference variableType = variable.VariableType.Resolve();
            if (variable.VariableType.IsGenericParameter)
                variableType = variable.VariableType;
            else
                variableType = ResolveTypeRef(variable.VariableType, methodBody.Method.Module, methodBody.Method, originalMethod.Module);

            var newVariable = new VariableDefinition(variableType);
            methodBody.Variables.Insert(Math.Min(methodBody.Variables.Count, variable.Index), newVariable);
            return newVariable;
        }
    }
}