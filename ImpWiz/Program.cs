using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Loader;
using ImpWiz.Filters;
using Mono.Cecil;

namespace ImpWiz
{
    /// <summary>
    /// The main program.
    /// </summary>
    public static class Program
    {
        
        private static readonly Dictionary<AssemblyDefinition, string> LoadedAssemblies = new Dictionary<AssemblyDefinition, string>();

        private static readonly AssemblyLoadContext LoadContext = new AssemblyLoadContext("sandbox", true);

        /// <summary>
        /// The programs main function.
        /// </summary>
        /// <param name="args">The arguments passed by command line.</param>
        public static void Main(string[] args)
        {
            var arguments = new Arguments();
            arguments.ParseArguments(args);

            if (arguments.ShowHelp)
            {
                Console.WriteLine("Usage: ImpWiz -i InputFile [...] [-o OutputFile ...].");
                Console.WriteLine("");
                Console.WriteLine("Options:");
                Console.WriteLine("\t--help\t\tShows this help.");
                Console.WriteLine("\t-i\t\tThe following assembly files will be rewritten.");
                Console.WriteLine("\t-o\t\tThe following destination files will be used for the input assemblies.");
                return;
            }
            if (arguments.InputFiles.Count == 0)
            {
                Console.Error.WriteLine("Error: No files specified.");
                return;
            }
            if (arguments.InputFiles.Count < arguments.OutputFiles.Count)
            {
                Console.Error.WriteLine("Error: Not enough input files specified, or too much output files specified.");
                return;
            }

            for (int i = 0; i < arguments.InputFiles.Count; i++)
            {
                var inputFile = arguments.InputFiles[i];
                var outputFile = arguments.OutputFiles[i];
                if (!File.Exists(inputFile))
                {
                    Console.Error.WriteLine("Input file: " + inputFile + " doesn't exist.");
                    return;
                }
                try
                {
                    bool overwriteOriginal = Path.GetFullPath(inputFile) == Path.GetFullPath(outputFile);
                    //AssemblyDefinition outputAssembly = AssemblyDefinition.ReadAssembly(outputFile);
            
                    AssemblyDefinition asm = AssemblyDefinition.ReadAssembly(inputFile, new ReaderParameters(ReadingMode.Deferred) {ReadWrite = overwriteOriginal});
                    LoadedAssemblies.Add(asm, inputFile);

                    ITypeFilterStrategy strategy = TypeFilterStrategy.Exclude;

                    bool integrateImpWiz = true;


                    var assemblyProcessor = new AssemblyProcessor(asm, strategy, integrateImpWiz);
            
                    assemblyProcessor.Process();

                    LoadContext.Unload();
                    if (overwriteOriginal)
                        asm.Write();
                    else
                        asm.Write(outputFile);
                    /*if (File.Exists(inputFile))
                        File.Delete(inputFile);
    
                    if (File.Exists(outputFile))
                        File.Move(outputFile, inputFile);*/
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e);
                    throw;
                }
            }





        }
    }
}