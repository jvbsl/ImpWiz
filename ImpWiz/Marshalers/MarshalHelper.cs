using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using ImpWiz.Marshalers;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace ImpWiz.Import
{
    public class MarshalHelper
    {
        public static IImpWizMarshaler GetMarshaler<T>(T parameter) where T : ICustomAttributeProvider, IMarshalInfoProvider
        {
            if (!parameter.HasMarshalInfo)
                return null;
            var attribute = parameter.MarshalInfo;
            switch (attribute)
            {
                case CustomMarshalInfo c:
                    return null;
            }

            switch (attribute.NativeType)
            {
                case NativeType.LPStr:
                    break;
            }
            return null;
        }
    }
}