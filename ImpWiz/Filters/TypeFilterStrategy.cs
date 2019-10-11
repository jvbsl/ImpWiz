using System.Runtime.InteropServices;
using ImpWiz.Import;

namespace ImpWiz.Filters
{
    /// <summary>
    /// A static class containing some basic filter strategies.
    /// </summary>
    public static partial class TypeFilterStrategy
    {
        /// <summary>
        /// Does not filter and includes all types to be searched for <see cref="DllImportAttribute"/>.
        /// </summary>
        public static ITypeFilterStrategy All { get; }

        /// <summary>
        /// Includes types by default and only excludes if marked with <see cref="ImportFilterAttribute"/> with
        /// given given <see cref="ImportFilterAttribute.Include"/> of <c>false</c>,
        /// to be searched for <see cref="DllImportAttribute"/>.
        /// </summary>
        public static ITypeFilterStrategy Exclude { get; }

        /// <summary>
        /// Only includes types marked by <see cref="ImportFilterAttribute"/>
        /// with given <see cref="ImportFilterAttribute.Include"/> of <c>true</c> and excludes by default,
        /// to be searched for <see cref="DllImportAttribute"/>.
        /// </summary>
        public static ITypeFilterStrategy Include { get; }

        static TypeFilterStrategy()
        {
            All = new FilterStrategyAll();
            Exclude = new FilterStrategyExclude();
            Include = new FilterStrategyInclude();
        }
    }
}