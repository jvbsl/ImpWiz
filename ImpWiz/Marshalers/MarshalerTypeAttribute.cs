using System;
using System.Runtime.InteropServices;

namespace ImpWiz.Marshalers
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class MarshalerTypeAttribute : Attribute
    {
        public UnmanagedType UnmanagedType { get; }

        public MarshalerTypeAttribute(UnmanagedType unmanagedType)
        {
            UnmanagedType = unmanagedType;
        }
    }
}