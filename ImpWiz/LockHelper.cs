using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace ImpWiz
{
    /// <summary>
    /// Helper class to create locks. (Nesting currently not possible).
    /// </summary>
    public class LockHelper
    {
        private readonly ModuleDefinition _module;
        private readonly MethodDefinition _method;
        private readonly ILProcessor _processor;
        
        private readonly AssemblyNameReference _netstandard;
        
        private Instruction _tryBegin;
        private int _lockCheckLocal;
        private int _lockLocal;

        /// <summary>
        /// Initializes a new instance of the <see cref="LockHelper"/> class.
        /// </summary>
        /// <param name="module">The module used for resolving of types.</param>
        /// <param name="method">The method the lock should be created in.</param>
        public LockHelper(ModuleDefinition module, MethodDefinition method)
        {
            _module = module;
            _method = method;
            _processor = method.Body.GetILProcessor();
            _netstandard = module.AssemblyReferences.First(x => x.Name == "netstandard");
        }
        private MethodReference GetMonitorEnter(ModuleDefinition m)
        {
            var typeRef = new TypeReference(nameof(System) + "." + nameof(System.Threading), nameof(System.Threading.Monitor), m, _netstandard);
            var methodRef = new MethodReference(nameof(System.Threading.Monitor.Enter), m.TypeSystem.Void, typeRef);
            methodRef.Parameters.Add(new ParameterDefinition(m.TypeSystem.Object));
            methodRef.Parameters.Add(new ParameterDefinition(m.TypeSystem.Boolean.MakeByReferenceType()));
            return methodRef;
        }
        private MethodReference GetMonitorExit(ModuleDefinition m)
        {
            var typeRef = new TypeReference(nameof(System) + "."  + nameof(System.Threading), nameof(System.Threading.Monitor), m, _netstandard);
            var methodRef = new MethodReference(nameof(System.Threading.Monitor.Exit), m.TypeSystem.Void, typeRef);
            methodRef.Parameters.Add(new ParameterDefinition(m.TypeSystem.Object));
            return methodRef;

        }

        /// <summary>
        /// Starts a locking-block scope using the given <paramref cref="lockObject"/> field.
        /// </summary>
        /// <param name="lockObject">The object to do the locking on.</param>
        /// <exception cref="NotSupportedException">Nested locks currently not supported.</exception>
        public void BeginLock(FieldDefinition lockObject)
        {
            var body = _method.Body;
            _method.Body.InitLocals = true;
            _lockLocal = body.Variables.Count;
            if (_tryBegin != null)
                throw new NotSupportedException("nested locks currently not supported");
            _tryBegin = _processor.CreateLdloc(_lockLocal);
            
            
            body.Variables.Add(new VariableDefinition(_module.TypeSystem.Object));
            _lockCheckLocal = body.Variables.Count;
            var checkLocalVariable = new VariableDefinition(_module.TypeSystem.Boolean);
            body.Variables.Add(checkLocalVariable);
            _processor.Emit(OpCodes.Ldsfld, lockObject);
            _processor.EmitStloc(_lockLocal);
            _processor.Emit(OpCodes.Ldc_I4_0);
            _processor.EmitStloc(_lockCheckLocal);
            
            _processor.Append(_tryBegin);
            _processor.Emit(OpCodes.Ldloca_S, checkLocalVariable);

            MethodReference monitorEnter = _module.ImportReference(GetMonitorEnter(_module));
            
            _processor.Emit(OpCodes.Call, monitorEnter);


        }

        /// <summary>
        /// Ends a locking-block scope before the given <paramref name="endInstruction"/>.
        /// </summary>
        /// <param name="endInstruction">The <see cref="Instruction"/> directly following the lock scope.</param>
        public void EndLock(Instruction endInstruction)
        {
            var endFinally = _processor.Create(OpCodes.Endfinally);
            
            var leaveTry = _processor.Create(OpCodes.Leave_S, endInstruction);
            _processor.Append(leaveTry);

            var startFinally = _processor.CreateLdloc(_lockCheckLocal);
            _processor.Append(startFinally);
            _processor.Emit(OpCodes.Brfalse_S, endFinally);

            _processor.EmitLdloc(_lockLocal);
            
            
            MethodReference monitorExit = _module.ImportReference(GetMonitorExit(_module));
            _processor.Emit(OpCodes.Call, monitorExit);
            
            _processor.Append(endFinally);

            _processor.Append(endInstruction);

            var handler = new ExceptionHandler(ExceptionHandlerType.Finally)
            {
                TryStart = _tryBegin,
                TryEnd = startFinally,
                HandlerStart = startFinally,
                HandlerEnd = endInstruction
            };

            _method.Body.ExceptionHandlers.Add(handler);

            _tryBegin = null;
        }
    }
}