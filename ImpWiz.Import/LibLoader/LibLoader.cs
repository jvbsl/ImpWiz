using System;
using System.Runtime.InteropServices;

namespace ImpWiz.Import.LibLoader
{
    /// <summary>
    /// A platform independent <see cref="ILibLoader"/> implementation.
    /// </summary>
    public sealed class LibLoader : ILibLoader
    {
        private readonly ILibLoader _internal;

        /// <summary>
        /// The singleton <see cref="LibLoader"/> instance.
        /// </summary>
        public static LibLoader Instance { get; }

        static LibLoader()
        {
            Instance = new LibLoader();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LibLoader"/> class.
        /// </summary>
        public LibLoader()
        {
            var platform = Environment.OSVersion;
            bool isBsd = RuntimeInformation.OSDescription.IndexOf("bsd", StringComparison.InvariantCultureIgnoreCase) !=
                         -1;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _internal = new WinLibLoader();
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || isBsd)
                _internal = new BsdLibLoader(); // used for OSX and BSD as libdl.dylib perhaps not available?
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                _internal = new UnixLibLoader(); // for now only linux: need to investigate if BsdLoader is enough for everything else
            
            throw new NotSupportedException("Platform not supported!");
        }

        /// <inheritdoc />
        public IntPtr LoadLibrary(string name)
        {
            var handle = _internal.LoadLibrary(name);
            if (handle == IntPtr.Zero)
                throw new DllNotFoundException("ImpWiz: " + GetError());
            return handle;
        }

        /// <inheritdoc />
        public IntPtr GetProcAddress(IntPtr handle, string name)
        {
            var symbol = _internal.GetProcAddress(handle, name);
            if (symbol == IntPtr.Zero)
                throw new EntryPointNotFoundException("ImpWiz: " + GetError());
            return symbol;
        }

        /// <inheritdoc />
        public bool FreeLibrary(IntPtr handle)
        {
            if (!_internal.FreeLibrary(handle))
                throw new Exception("ImpWiz: " + GetError());
            return true;
        }

        /// <inheritdoc />
        public string GetError()
        {
            return _internal.GetError();
        }

        /// <inheritdoc />
        public void Prepare()
        {
            _internal.Prepare();
        }
    }
}