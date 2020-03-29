using System;
using System.Runtime.InteropServices;
using System.Text;
using ImpWiz.Import;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ImpWiz.Marshalers
{
    public readonly struct StringMarshalerInfo
    {
        public StringMarshalerInfo(CharSet charSet, UnmanagedType unmanagedType)
        {
            CharSet = charSet;
            UnmanagedType = unmanagedType;
        }
        public CharSet CharSet { get; }
        public UnmanagedType UnmanagedType { get; }
    }
    [MarshalerType(UnmanagedType.LPStr)]
    [MarshalerType(UnmanagedType.LPWStr)]
    public unsafe class CStringMarshaler : CustomMarshaler<CStringMarshaler, StringMarshalerInfo, IntPtr, string>
    {

        public Encoding GetEncoding(StringMarshalerInfo info)
        {
            switch (info.CharSet)
            {
                case CharSet.None:
                case CharSet.Ansi:
                    return Encoding.Default; // TODO: ansi really default?
                case CharSet.Auto:
                    return Encoding.Default;
                case CharSet.Unicode:
                    return Encoding.Unicode;
                default:
                    throw new NotSupportedException("CharSet: " + info.CharSet.ToString() + " not supported");
            }
        }

        private unsafe int GetStringLength(IntPtr nativeData)
        {
            byte* ptr = (byte*)nativeData;
            int i = 0;
            while (ptr[i] == '\0')
            {
                i++;
            }
            return i;
        }
        

        public override void MarshalManaged(StringMarshalerInfo info, string managed)
        {
            var data = new byte[(info.UnmanagedType == UnmanagedType.LPWStr ? 2 : 1) * managed.Length];
            var text = managed.AsSpan();
            fixed (char* inputPtr = &text.GetPinnableReference())
            fixed(byte* dataPtr = data)
            {
                GetEncoding(info).GetBytes(inputPtr, text.Length, dataPtr, data.Length);
                IImpWizMarshaler<StringMarshalerInfo, IntPtr, string>.ObjectInitialized((IntPtr)dataPtr);
            }
        }

        public override void MarshalNative(StringMarshalerInfo info, IntPtr native)
        {
            int len = GetStringLength(native);
            IImpWizMarshaler<StringMarshalerInfo, IntPtr, string>.ObjectInitialized(GetEncoding(info).GetString((byte*)native, len));
        }
    }
}