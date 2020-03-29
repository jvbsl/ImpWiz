using System.IO;
using System.Linq;
using ImpWiz.Import.LibLoader;
using Mono.Cecil;

namespace ImpWiz.CecilAttributes
{
    /// <summary>
    /// A Mono.Cecil object representation of a <see cref="ImpWiz.Import.ImportLoaderAttribute"/>.
    /// </summary>
    public class ImportLoaderCecil
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context">
        /// The <see cref="ModuleProcessor"/> context to get <see cref="ImpWiz.Import"/> types and methods.
        /// </param>
        /// <param name="libLoaderTypeDef">
        /// The library loader<see cref="TypeDefinition"/> to use for library and symbol resolving.
        /// <c>null</c> for fallback to the default <see cref="LibLoader"/>
        /// </param>
        /// <param name="loaderCookie">The <see cref="LoaderCookie"/>.</param>
        public ImportLoaderCecil(ModuleProcessor context, TypeDefinition libLoaderTypeDef = null, string loaderCookie = null)
        {
            LoaderCookie = loaderCookie;
            libLoaderTypeDef ??= context.AssemblyContext.ImportAssemblyLibLoader;
            
            var methodInfo = libLoaderTypeDef.Methods.First(x =>
                x.Name == "GetInstance" && x.IsStatic && x.IsPublic && x.Parameters.Count == 1 &&
                x.Parameters[0].ParameterType.FullName == "System.String");

            GetInstanceMethod = methodInfo;


            LibraryLoaderTypeDefinition = libLoaderTypeDef;
        }
        
        /// <summary>
        /// The loader cookie to pass to the <see cref="ImpWiz.Import.LibLoader.LibLoader"/> or custom lib loaders.
        /// </summary>
        public string LoaderCookie { get; }
        
        /// <summary>
        /// The <see cref="LibLoader.GetInstance"/> method or GetInstance of a custom lib loader,
        /// to get a singleton library and symbol resolver.
        /// </summary>
        public MethodReference GetInstanceMethod { get; }
        
        /// <summary>
        /// The library and symbol resolver to use.
        /// </summary>
        public TypeDefinition LibraryLoaderTypeDefinition { get; }
    }
}