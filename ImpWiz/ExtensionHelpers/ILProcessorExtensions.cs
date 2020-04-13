using Mono.Cecil.Cil;

namespace ImpWiz
{
    /// <summary>
    /// Helpful extensions for <see cref="ILProcessor"/>.
    /// </summary>
    public static class IlProcessorExtensions
    {
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Ldloca"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning creating the special OpCodes(e.g. <see cref="OpCodes.Ldloc_S"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to load.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateLdloca(this ILProcessor processor, int index)
        {
            return processor.Create(OpCodes.Ldloca, index);
        }
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Ldloca"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CreateLdloca(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to load.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateLdloca(this ILProcessor processor, VariableDefinition variable)
        {
            return processor.CreateLdloca(variable.Index);
        }
        
        /// <summary>
        /// Emits optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning Creating the special OpCodes(e.g. <see cref="OpCodes.Ldloc_0"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to load.</param>
        public static void EmitLdloca(this ILProcessor processor, int index)
        {
            processor.Append(processor.CreateLdloca(index));
        }

        /// <summary>
        /// Emits optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="EmitLdloc(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to load.</param>
        public static void EmitLdloca(this ILProcessor processor, VariableDefinition variable)
        {
            processor.Append(processor.CreateLdloca(variable));
        }
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning creating the special OpCodes(e.g. <see cref="OpCodes.Ldloc_0"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to load.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateLdloc(this ILProcessor processor, int index)
        {
            switch (index)
            {
                case 0:
                    return processor.Create(OpCodes.Ldloc_0);
                case 1:
                    return processor.Create(OpCodes.Ldloc_1);
                case 2:
                    return processor.Create(OpCodes.Ldloc_2);
                case 3:
                    return processor.Create(OpCodes.Ldloc_3);
                default:
                    return processor.Create(OpCodes.Ldloc, index);
            }
        }
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CreateLdloc(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to load.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateLdloc(this ILProcessor processor, VariableDefinition variable)
        {
            return processor.CreateLdloc(variable.Index);
        }

        /// <summary>
        /// Emits optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning Creating the special OpCodes(e.g. <see cref="OpCodes.Ldloc_0"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to load.</param>
        public static void EmitLdloc(this ILProcessor processor, int index)
        {
            processor.Append(processor.CreateLdloc(index));
        }

        /// <summary>
        /// Emits optimal <see cref="OpCodes.Ldloc"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="EmitLdloc(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to load.</param>
        public static void EmitLdloc(this ILProcessor processor, VariableDefinition variable)
        {
            processor.Append(processor.CreateLdloc(variable));
        }
        
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Stloc"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning creating the special OpCodes(e.g. <see cref="OpCodes.Stloc_0"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to store in.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateStloc(this ILProcessor processor, int index)
        {
            switch (index)
            {
                case 0:
                    return processor.Create(OpCodes.Stloc_0);
                case 1:
                    return processor.Create(OpCodes.Stloc_1);
                case 2:
                    return processor.Create(OpCodes.Stloc_2);
                case 3:
                    return processor.Create(OpCodes.Stloc_3);
                default:
                    return processor.Create(OpCodes.Stloc, index);
            }
        }
        
        /// <summary>
        /// Creates optimal <see cref="OpCodes.Stloc"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CreateStloc(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to store in.</param>
        /// <returns>The created <see cref="Instruction"/>.</returns>
        public static Instruction CreateStloc(this ILProcessor processor, VariableDefinition variable)
        {
            return processor.CreateStloc(variable.Index);
        }

        /// <summary>
        /// Emits optimal <see cref="OpCodes.Stloc"/> instructions by a given <paramref name="index"/>.
        /// </summary>
        /// <remarks>
        /// Optimal meaning creating the special OpCodes(e.g. <see cref="OpCodes.Stloc_0"/>...)
        /// if <paramref name="index"/> is smaller than 4.
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="index">The index of the variable to store in.</param>
        public static void EmitStloc(this ILProcessor processor, int index)
        {
            processor.Append(processor.CreateStloc(index));
        }
        
        /// <summary>
        /// Emits optimal <see cref="OpCodes.Stloc"/> instructions by a given <paramref name="variable"/>.
        /// </summary>
        /// <remarks>
        /// <see cref="CreateStloc(ILProcessor, int)"/>
        /// </remarks>
        /// <param name="processor">The <see cref="ILProcessor"/>.</param>
        /// <param name="variable">The variable to store in.</param>
        public static void EmitStloc(this ILProcessor processor, VariableDefinition variable)
        {
            processor.Append(processor.CreateStloc(variable));
        }

        public static void EmitLdc_4(this ILProcessor processor, int val)
        {
            switch (val)
            {
                case 0:
                    processor.Emit(OpCodes.Ldc_I4_0);
                    break;
                case 1:
                    processor.Emit(OpCodes.Ldc_I4_1);
                    break;
                case 2:
                    processor.Emit(OpCodes.Ldc_I4_2);
                    break;
                case 3:
                    processor.Emit(OpCodes.Ldc_I4_3);
                    break;
                case 4:
                    processor.Emit(OpCodes.Ldc_I4_4);
                    break;
                case 5:
                    processor.Emit(OpCodes.Ldc_I4_5);
                    break;
                case 6:
                    processor.Emit(OpCodes.Ldc_I4_6);
                    break;
                case 7:
                    processor.Emit(OpCodes.Ldc_I4_7);
                    break;
                case 8:
                    processor.Emit(OpCodes.Ldc_I4_8);
                    break;
                default:
                    if (val >= sbyte.MinValue && val <= sbyte.MaxValue)
                        processor.Emit(OpCodes.Ldc_I4_S, (sbyte) val);
                    else if (val >= byte.MinValue && val <= byte.MaxValue)
                        processor.Emit(OpCodes.Ldc_I4_S, (byte) val);
                    else
                        processor.Emit(OpCodes.Ldc_I4, val);
                    break;
            }
        }
    }
}