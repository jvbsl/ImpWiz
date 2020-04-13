using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ImpWiz.Import.Marshalers
{
    public readonly struct StringMarshalerInfo
    {
        public StringMarshalerInfo([MarshalerInfoInitialization("CharSet")]CharSet charSet, [MarshalerInfoInitialization("UnmanagedType")]UnmanagedType unmanagedType)
        {
            CharSet = charSet;
            UnmanagedType = unmanagedType;
        }
        public CharSet CharSet { get; }
        public UnmanagedType UnmanagedType { get; }
    }
    [MarshalerType(UnmanagedType.LPStr)]
    [MarshalerType(UnmanagedType.LPWStr)]
    public unsafe class CStringMarshaler : ImpWizMarshaler<CStringMarshaler, StringMarshalerInfo, IntPtr, string>
    {

        public static Encoding GetEncoding(StringMarshalerInfo info)
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
                    throw new NotSupportedException("CharSet: " + info.CharSet + " not supported");
            }
        }

        public static int GetStringLength(IntPtr nativeData)
        {
            byte* ptr = (byte*)nativeData;
            if (ptr == null)
                return 0;
            int i = 0;
            while (ptr[i] != '\0')
            {
                i++;
            }
            return i;
        }
        

        public override void MarshalManaged(StringMarshalerInfo info, string managed)
        {
            var data = new byte[(info.UnmanagedType == UnmanagedType.LPWStr ? 2 : 1) * managed.Length + 1];
            var text = managed.AsSpan();
            fixed (char* inputPtr = &text.GetPinnableReference())
            fixed(byte* dataPtr = data)
            {
                GetEncoding(info).GetBytes(inputPtr, text.Length, dataPtr, data.Length);
                MarshalInitialization<IntPtr, string>.ObjectInitialized((IntPtr)dataPtr);
            }
        }

        public override void MarshalNative(StringMarshalerInfo info, IntPtr native)
        {
            string res = null;
            if (native != IntPtr.Zero)
                res = GetEncoding(info).GetString((byte*) native, GetStringLength(native));
            MarshalInitialization<IntPtr, string>.ObjectInitialized(res);
        }
    }
}