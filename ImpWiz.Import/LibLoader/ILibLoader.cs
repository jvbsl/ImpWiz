using System;

namespace ImpWiz.Import.LibLoader
{
    /// <summary>
    /// A simple interface for native library loading.
    /// </summary>
    public interface ILibLoader
    {
        /// <summary>
        /// Loads a native library by the the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The library name or path by which to load the native library by.</param>
        /// <returns>A library handle on success; otherwise <see cref="IntPtr.Zero"/>.</returns>
        IntPtr LoadLibrary(string name);
        
        /// <summary>
        /// Gets a native method address from a Library specified by <paramref name="handle"/>
        /// with a given <paramref name="name"/>.
        /// </summary>
        /// <param name="handle">The library handle to specify the library from which the symbol should be loaded.</param>
        /// <param name="name">The function name of which the symbol should be loaded.</param>
        /// <returns>A resolved function pointer on success; otherwise <see cref="IntPtr.Zero"/>.</returns>
        IntPtr GetProcAddress(IntPtr handle, string name);
        
        /// <summary>
        /// Frees a dynamically loaded library.
        /// </summary>
        /// <param name="handle">The library handle to specify the library which should be closed.</param>
        /// <returns>Whether freeing the library was successful or not.</returns>
        bool FreeLibrary(IntPtr handle);

        /// <summary>
        /// Gets an error message text.
        /// </summary>
        /// <returns>An error message text.</returns>
        string GetError();

        /// <summary>
        /// Initializes the used P/Invoke methods, to prevent problems occuring with error handling.
        /// (Problem being, that occured error from 'dlerror' will be overwritten by P/Invoke lazy loading).
        /// </summary>
        void Prepare();
    }
}