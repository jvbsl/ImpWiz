using System;

namespace ImpWiz.Import.LibLoader
{
    /// <summary>
    /// A simple interface for native library loading.
    /// </summary>
    public interface ILibLoader : ICustomLibraryLoader
    {
        /// <summary>
        /// Initializes the used P/Invoke methods, to prevent problems occuring with error handling.
        /// (Problem being, that occured error from 'dlerror' will be overwritten by P/Invoke lazy loading).
        /// </summary>
        void Prepare();
    }
}