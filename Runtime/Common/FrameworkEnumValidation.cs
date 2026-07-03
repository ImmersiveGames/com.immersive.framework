using System;
using System.Collections.Generic;

namespace Immersive.Framework.Common
{
    internal static class FrameworkEnumValidation
    {
        internal static bool IsDefined<TEnum>(TEnum value)
            where TEnum : struct, Enum
        {
            return Enum.IsDefined(typeof(TEnum), value);
        }

        internal static bool IsDefinedAndNot<TEnum>(TEnum value, TEnum invalid)
            where TEnum : struct, Enum
        {
            return IsDefined(value) && !EqualityComparer<TEnum>.Default.Equals(value, invalid);
        }

        internal static void ThrowIfUndefined<TEnum>(TEnum value, string paramName, string message)
            where TEnum : struct, Enum
        {
            if (!IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(paramName, value, message);
            }
        }

        internal static void ThrowIfUndefinedOr<TEnum>(TEnum value, TEnum invalid, string paramName, string message)
            where TEnum : struct, Enum
        {
            if (!IsDefinedAndNot(value, invalid))
            {
                throw new ArgumentOutOfRangeException(paramName, value, message);
            }
        }
    }
}
