using System;
using System.Collections.Generic;
using System.Text;

namespace Slush
{
    internal static class Util
    {
        public static IList<T> CloneList<T>(IList<T> source)
        {
            T[] clone = new T[source.Count];
            source.CopyTo(clone, 0);
            return clone;
        }

        public static IList<T> SliceList<T>(IList<T> source, int start, int count)
        {
            T[] slice = new T[count];
            for (int i = 0; i < count; i++)
            {
                slice[i] = source[start + i];
            }
            return slice;
        }
    }
}
