using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace System.Linq
{
    public static partial class EnumerableEx
    {
        public static IEnumerable<IndexerPair<T>> Enumerated<T>(this IEnumerable<T> ienum)
        {
            var i = 0;
            
            var en = ienum.GetEnumerator();
            try
            {
                while (en.MoveNext())
                    yield return new IndexerPair<T> {index = i++, item = en.Current};
            }
            finally
            {
                if (en != null)
                    en.Dispose();
            }
            en = null;
        }
    
        public static void Each<T>(this IEnumerable<T> ienum, Action<T> action = null)
        {
            foreach (var elem in ienum)
                if (action != null) action(elem);
        }
    }
    
    public struct IndexerPair<T>
    {
        public T item;
        public int index;
    }
}