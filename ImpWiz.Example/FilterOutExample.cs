using System.Runtime.InteropServices;
using ImpWiz.Import;

namespace ImpWiz.Example
{
    [ImportFilter(false)]
    public class FilterOutExample
    {
        [DllImport("test.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Hmm();
    }
}