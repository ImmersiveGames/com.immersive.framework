using UnityEngine;

namespace Immersive.Framework.Common
{
    internal static class UnityObjectReference
    {
        internal static bool IsClrNull(Object value) => ReferenceEquals(value, null);

        internal static bool IsUnityFakeNull(Object value) =>
            !ReferenceEquals(value, null) && value == null;

        internal static bool IsAlive(Object value) =>
            !IsClrNull(value) && !IsUnityFakeNull(value);
    }
}
