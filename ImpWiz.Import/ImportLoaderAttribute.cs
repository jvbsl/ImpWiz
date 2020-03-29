using System;
using System.Reflection;

namespace ImpWiz.Import
{
    public class ImportLoaderAttribute : Attribute
    {
        public ImportLoaderAttribute(string loaderCookie = null)
            : this(typeof(LibLoader.LibLoader), loaderCookie)
        {
        }
        public ImportLoaderAttribute(Type libLoaderTypeRef, string loaderCookie = null)
        {
            if (!typeof(ICustomLibraryLoader).IsAssignableFrom(libLoaderTypeRef))
                throw new ArgumentException("The type reference needs to implement the ICustomLibraryLoader interface.", nameof(libLoaderTypeRef));

            var methodInfo = libLoaderTypeRef.GetMethod("GetInstance", BindingFlags.Public | BindingFlags.Static, null,
                new[] {typeof(string)}, null);
            if (methodInfo == null)
                throw new ArgumentException("The type needs to implement a `public static ICustomLibraryLoader GetInstance(string)` method.", nameof(libLoaderTypeRef));

            GetInstanceMethod = methodInfo;

            LoaderCookie = loaderCookie;

            LibraryLoaderTypeReference = libLoaderTypeRef;
        }
        
        public string LoaderCookie { get; }
        
        public MethodInfo GetInstanceMethod { get; }
        
        public Type LibraryLoaderTypeReference { get; }
    }
}