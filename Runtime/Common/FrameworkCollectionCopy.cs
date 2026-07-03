using System;
using System.Collections.Generic;

namespace Immersive.Framework.Common
{
    internal static class FrameworkCollectionCopy
    {
        internal static bool IsNullOrEmpty<T>(IReadOnlyList<T> items)
        {
            return items == null || items.Count == 0;
        }

        internal static T[] ToArrayOrEmpty<T>(IReadOnlyList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return Array.Empty<T>();
            }

            var copy = new T[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                copy[i] = items[i];
            }

            return copy;
        }
    }
}
