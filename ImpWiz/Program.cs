using System;
using System.IO;
using ImpWiz.Filters;
using Mono.Cecil;

namespace ImpWiz
{
    /// <summary>
    /// The main program.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        /// The programs main function.
        /// </summary>
        /// <param name="args">The arguments passed by command line.</param>
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("No files specified.");
                return;
            }
            string inputFile = args[0];

            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine("Input file: " + inputFile + " doesn't exist.");
                return;
            }

            string outputFile = Path.Combine(Environment.CurrentDirectory, Path.GetFileNameWithoutExtension(inputFile) + ".Rewritten" + Path.GetExtension(inputFile));

            //AssemblyDefinition outputAssembly = AssemblyDefinition.ReadAssembly(outputFile);
            
            AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputFile);

            ITypeFilterStrategy strategy = TypeFilterStrategy.Exclude;


            var assemblyProcessor = new AssemblyProcessor(asm, strategy);
            
            assemblyProcessor.Process();

            
            asm.Write(outputFile);
            
            if (File.Exists(inputFile))
                File.Delete(inputFile);

            if (File.Exists(outputFile))
                File.Move(outputFile, inputFile);
        }
    }
}