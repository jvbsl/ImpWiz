using System;

namespace ImpWiz.Import
{
    /// <summary>
    /// 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class ImportFilterAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImportFilterAttribute"/> class.
        /// </summary>
        /// <param name="include">Whether to include the marked class or method.</param>
        public ImportFilterAttribute(bool include = true)
        {
            Include = include;
        }
        
        /// <summary>
        /// Gets whether to include the marked class or method.
        /// </summary>
        public bool Include { get; }
    }
}