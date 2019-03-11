using System.Collections.Generic;

namespace System.Linq
{
	public static partial class EnumerableEx
	{
		/// <summary>
        /// Returns a sequence resulting from applying a function to each
        /// element in the source sequence and its
        /// predecessor, with the exception of the first element which is
        /// only returned as the predecessor of the second element.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
        /// <typeparam name="TResult">The type of the element of the returned sequence.</typeparam>
        /// <param name="source">The source sequence.</param>
        /// <param name="resultSelector">A transform function to apply to
        /// each pair of sequence.</param>
        /// <returns>
        /// Returns the resulting sequence.
        /// </returns>
        /// <remarks>
        /// This operator uses deferred execution and streams its results.
        /// </remarks>
        /// <example>
        /// <code><![CDATA[
        /// int[] numbers = { 123, 456, 789 };
        /// var result = numbers.Pairwise((a, b) => a + b);
        /// ]]></code>
        /// The <c>result</c> variable, when iterated over, will yield
        /// 579 and 1245, in turn.
        /// </example>

        public static IEnumerable<TResult> Pairwise<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

	        return PairwiseCore(source, resultSelector);
        }
		
		public static IEnumerable<Pair<T>> Pairwise<T>(this IEnumerable<T> source)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			return PairwiseCore(source, (x, y) => new Pair<T>(x, y));
		}

		private static IEnumerable<TResult> PairwiseCore<TSource, TResult>(IEnumerable<TSource> source, Func<TSource, TSource, TResult> resultSelector)
	    {
	        using (var e = source.GetEnumerator())
	        {
	            if (!e.MoveNext())
	                yield break;

	            var previous = e.Current;
	            while (e.MoveNext())
	            {
	                yield return resultSelector(previous, e.Current);
	                previous = e.Current;
	            }
	        }
	    }
	}
    
    [Serializable]
    public struct Pair<T> : IEquatable<Pair<T>>
    {
        readonly T previous;
        readonly T current;

        public T Previous
        {
            get { return previous; }
        }

        public T Current
        {
            get { return current; }
        }

        public Pair(T previous, T current)
        {
            this.previous = previous;
            this.current = current;
        }

        public override int GetHashCode()
        {
            var comparer = EqualityComparer<T>.Default;

            int h0;
            h0 = comparer.GetHashCode(previous);
            h0 = (h0 << 5) + h0 ^ comparer.GetHashCode(current);
            return h0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Pair<T>)) return false;

            return Equals((Pair<T>)obj);
        }

        public bool Equals(Pair<T> other)
        {
            var comparer = EqualityComparer<T>.Default;

            return comparer.Equals(previous, other.Previous) &&
                   comparer.Equals(current, other.Current);
        }

        public override string ToString()
        {
            return string.Format("({0}, {1})", previous, current);
        }
    }
}