using Mono.Cecil.Cil;

namespace ImpWiz
{
    /// <summary>
    /// Helpful extensions for <see cref="ILProcessor"/>.
    /// </summary>
    public static class IlProcessorExtensions
    {
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
    }
}