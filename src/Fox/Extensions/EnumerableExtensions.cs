using System;
using System.Collections.Generic;
using System.Linq;

namespace Fox.Extensions
{
    public static class EnumerableExtensions
    {
        public static string Plural<T>(this IEnumerable<T> col, bool upper = false)
        {
            return col.Count() > 1 ? upper ? "S" : "s" : "";
        }

        public static IEnumerable<T> DistinctBy<T, TKey>(this IEnumerable<T> col, Func<T, TKey> keySelector)
        {
            return col.GroupBy(keySelector).Select(x => x.FirstOrDefault());
        }
    }
}
