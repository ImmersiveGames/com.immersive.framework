using System;
using System.Collections.Generic;

namespace Immersive.Framework.Common
{
    internal static class FrameworkIssueCounting
    {
        internal static int Count<T>(IReadOnlyList<T> items)
        {
            return items?.Count ?? 0;
        }

        internal static int CountWhere<T>(IReadOnlyList<T> items, Predicate<T> predicate)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (predicate(items[i]))
                {
                    count++;
                }
            }

            return count;
        }

        internal static bool HasAny<T>(IReadOnlyList<T> items)
        {
            return Count(items) > 0;
        }

        internal static bool HasAnyWhere<T>(IReadOnlyList<T> items, Predicate<T> predicate)
        {
            return CountWhere(items, predicate) > 0;
        }

        internal static int Sum<T>(IReadOnlyList<T> items, Func<T, int> selector)
        {
            if (items == null || items.Count == 0)
            {
                return 0;
            }

            int sum = 0;
            for (int i = 0; i < items.Count; i++)
            {
                sum += selector(items[i]);
            }

            return sum;
        }
    }
}
