using System;
using System.Collections.Generic;
using System.IO;
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
            if (typeDefinition.BaseType != null)
            {
                TypeReference resolved = typeDefinition.BaseType.Resolve();
                baseTypeRef = resolved.Module == originalModule ? ((TypeDefinition)  resolved).Clone(newModule) : newModule.ImportReference(typeDefinition.BaseType);
            }
            
            var newType = new TypeDefinition(typeDefinition.Namespace, typeDefinition.Name, typeDefinition.Attributes, baseTypeRef);
            foreach(var nestedType in typeDefinition.NestedTypes)
            {
                newType.NestedTypes.Add(nestedType.Clone(newModule));
            }
            newModule.Types.Add(newType);

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
                if (nonTopLevelInterfaces.Contains(inter))
                    continue;
                TypeReference resolved = inter.InterfaceType.Resolve();
                resolved = resolved.Module == originalModule ? ((TypeDefinition) resolved).Clone(newModule) : newModule.ImportReference(inter.InterfaceType);
                
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
        
        public static PropertyDefinition Clone(this PropertyDefinition prop, TypeDefinition newType)
        {
            var alreadyExistingProp = newType.Properties.FirstOrDefault(x => x.Name == prop.Name);
            if (alreadyExistingProp != null)
                return alreadyExistingProp;
            TypeReference type = prop.PropertyType.Resolve();
            type = type.Module == prop.Module ? ((TypeDefinition)type).Clone(newType.Module) : newType.Module.ImportReference(prop.PropertyType);
            var newProp = new PropertyDefinition(prop.Name, prop.Attributes, type);
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

        public static FieldDefinition Clone(this FieldDefinition field, TypeDefinition newType)
        {
            var alreadyExistingField = newType.Fields.FirstOrDefault(x =>
                x.Name == field.Name);
            if (alreadyExistingField != null)
                return alreadyExistingField;
            TypeReference type = field.FieldType.Resolve();
            type = type.Module == field.Module ? ((TypeDefinition)type).Clone(newType.Module) : newType.Module.ImportReference(field.FieldType);
            var newField = new FieldDefinition(field.Name, field.Attributes, type);
            newType.Fields.Add(newField);
            newField.Constant = field.Constant;
            newField.InitialValue = field.InitialValue;
            newField.HasDefault = field.HasDefault;
            newField.HasConstant = field.HasConstant;
            return newField;
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
            TypeReference retType = method.MethodReturnType.ReturnType.Resolve();
            retType = retType.Module == method.Module ? ((TypeDefinition) retType).Clone(newType.Module) : newType.Module.ImportReference(method.MethodReturnType.ReturnType);
            
            var newMethod = new MethodDefinition(method.Name, method.Attributes, retType);

            newType.Methods.Add(newMethod);
            foreach (var p in method.Parameters)
            {
                TypeReference pType = p.ParameterType.Resolve();
                pType = pType.Module == method.Module ? ((TypeDefinition) pType).Clone(newType.Module) : newType.Module.ImportReference(p.ParameterType);
                var newParam = new ParameterDefinition(p.Name, p.Attributes, pType);
                newParam.Constant = p.Constant;
                newParam.HasConstant = p.HasConstant;
                newParam.HasDefault = p.HasDefault;
                newMethod.Parameters.Add(newParam);
            }
            newMethod.ImplAttributes = method.ImplAttributes;
            newMethod.SemanticsAttributes = method.SemanticsAttributes;
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
            /*TypeDefinition attributeType = attribute.AttributeType.Resolve();
            attributeType = attributeType.Module == originalModule
                ? attributeType.Clone(newModule)
                : newModule.ImportReference(attribute.AttributeType).Resolve();
            var newType = attribute.AttributeType;*/
            
            var method = ResolveMethodReference(attribute.Constructor, newModule, originalModule);

            return new CustomAttribute(method, attribute.GetBlob());
        }

        private static MethodReference ResolveMethodReference(MethodReference method, ModuleDefinition currentModule, ModuleReference originalModule)
        {
            TypeDefinition declaringType = method.DeclaringType.Resolve();
            declaringType = declaringType.Module == originalModule
                ? declaringType.Clone(currentModule)
                : currentModule.ImportReference(method.DeclaringType).Resolve();
            var resolved = method.Resolve();
            var newMethod = resolved.Module == originalModule ? resolved.Clone(declaringType) : currentModule.ImportReference(method);
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
                    TypeDefinition declaringType = field.DeclaringType.Resolve();
                    declaringType = declaringType.Module == originalMethod.Module
                        ? declaringType.Clone(methodBody.Method.Module)
                        : methodBody.Method.Module.ImportReference(field.DeclaringType).Resolve();
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
                    var resolved = variable.Resolve();
                    var newVariable = variable.Clone(originalMethod, methodBody);
                    newInstr = Instruction.Create(instruction.OpCode, newVariable);
                    break;
                }
                case TypeReference type:
                {
                    TypeReference newType = type.Resolve().Module == originalMethod.Module
                        ? ((TypeDefinition)type).Clone(methodBody.Method.Module)
                        : methodBody.Method.Module.ImportReference(type);
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
            variableType = variableType.Module == originalMethod.Module
                ? ((TypeDefinition)variableType).Clone(methodBody.Method.Module)
                : methodBody.Method.Module.ImportReference(variable.VariableType);
            if (variable.IsPinned)
                variableType = variableType.MakePinnedType();
            var newVariable = new VariableDefinition(variableType);
            methodBody.Variables.Insert(Math.Min(methodBody.Variables.Count, variable.Index), newVariable);
            return newVariable;
        }
    }
}