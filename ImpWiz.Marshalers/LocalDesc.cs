using System;

namespace ImpWiz.Marshalers
{
    public enum ElementType : byte
    {
        ELEMENT_TYPE_VALUETYPE,
        ELEMENT_TYPE_INTERNAL,
        ELEMENT_TYPE_BYREF,
        ELEMENT_TYPE_PINNED,
        ELEMENT_TYPE_SZARRAY,
        ELEMENT_TYPE_PTR,
        ELEMENT_TYPE_BOOLEAN,
        ELEMENT_TYPE_I4,
        ELEMENT_TYPE_I2,
        ELEMENT_TYPE_I1
    }

    public class MethodTable
    {
        //TODO:
        
        public bool IsValueType => throw new NotImplementedException();
    }
    public class TypeHandle
    {
        private readonly MethodTable _pMt;

        public TypeHandle(MethodTable pMT)
        {
            _pMt = pMT;
        }
        //TODO: 
        public bool IsNativeValueType => throw new NotImplementedException();

        public MethodTable MethodTable => _pMt;
    }
    public unsafe struct LocalDesc
    {
        const int MAX_LOCALDESC_ELEMENTS = 8;

        fixed byte _elementType[MAX_LOCALDESC_ELEMENTS];
        int cbType;
        TypeHandle InternalToken; // only valid with ELEMENT_TYPE_INTERNAL

        // used only for E_T_FNPTR and E_T_ARRAY

        bool bIsCopyConstructed; // used for E_T_PTR

        public LocalDesc(ElementType elemType)
        {
            _elementType[0] = (byte) elemType;
            cbType = 1;
            bIsCopyConstructed = false;
            InternalToken = null;
        }

        public LocalDesc(TypeHandle thType)
        {
            _elementType[0] = (byte) ElementType.ELEMENT_TYPE_INTERNAL;
            cbType = 1;
            InternalToken = thType;
            bIsCopyConstructed = false;
        }

        public LocalDesc(MethodTable pMT)
        {
            _elementType[0] = (byte) ElementType.ELEMENT_TYPE_INTERNAL;
            cbType = 1;
            InternalToken = new TypeHandle(pMT);
            bIsCopyConstructed = false;
        }
        void MakeByRef()
        {
            ChangeType(ElementType.ELEMENT_TYPE_BYREF);
        }

        void MakePinned()
        {
            ChangeType(ElementType.ELEMENT_TYPE_PINNED);
        }

        void MakeArray()
        {
            ChangeType(ElementType.ELEMENT_TYPE_SZARRAY);
        }

        // makes the LocalDesc semantically equivalent to ET_TYPE_CMOD_REQD<IsCopyConstructed>/ET_TYPE_CMOD_REQD<NeedsCopyConstructorModifier>
        void MakeCopyConstructedPointer()
        {
            MakePointer();
            bIsCopyConstructed = true;
        }

        void MakePointer()
        {
            ChangeType(ElementType.ELEMENT_TYPE_PTR);
        }

        void ChangeType(ElementType elemType)
        {
            if ((MAX_LOCALDESC_ELEMENTS - 1) < cbType)
                throw new ArgumentException();

            for (var i = cbType; i >= 1; i--)
            {
                _elementType[i] = _elementType[i - 1];
            }

            _elementType[0] = (byte) elemType;
            cbType += 1;
        }

        bool IsValueClass()
        {
            bool lastElementTypeIsValueType = false;

            if (_elementType[cbType - 1] == (byte) ElementType.ELEMENT_TYPE_VALUETYPE)
            {
                lastElementTypeIsValueType = true;
            }
            else if ((_elementType[cbType - 1] == (byte) ElementType.ELEMENT_TYPE_INTERNAL) &&
                     (InternalToken.IsNativeValueType ||
                      InternalToken.MethodTable.IsValueType))
            {
                lastElementTypeIsValueType = true;
            }

            if (!lastElementTypeIsValueType)
            {
                return false;
            }

            // verify that the prefix element types don't make the type a non-value type
            // this only works on LocalDescs with the prefixes exposed in the Add* methods above.
            for (var i = 0; i < cbType - 1; i++)
            {
                if (_elementType[i] == (byte) ElementType.ELEMENT_TYPE_BYREF
                    || _elementType[i] == (byte) ElementType.ELEMENT_TYPE_SZARRAY
                    || _elementType[i] == (byte) ElementType.ELEMENT_TYPE_PTR)
                {
                    return false;
                }
            }

            return true;
        }
    };
}