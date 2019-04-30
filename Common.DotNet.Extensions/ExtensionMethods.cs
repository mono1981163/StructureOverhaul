using System;
using System.Collections;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Common.DotNet.Extensions
{
    public static class LinqExtensionMethods
    {
        [Pure]
        public static Option<string> GetAttributeGuid(this Type type)
        {
            return type.GetCustomAttribute<System.Runtime.InteropServices.GuidAttribute>().Transform(x => x.Value);
        }

        [Pure]
        public static Option<T> GetCustomAttribute<T>(this Type type) where T : Attribute
        {
            if (type == null) throw new ArgumentNullException("type");
            var assembly = type.Assembly;
            return assembly.GetCustomAttributes(typeof(T), true).OptionFirst().OfType<T>();
        }

        [Pure]
        public static IEnumerable<TSource> Cycle<TSource>(this IEnumerable<TSource> source)
        {
            var sourceCache = source.ToArray();
            while (true)
                foreach (var s in sourceCache)
                    yield return s;
        }

        [Pure]
        public static IEnumerable<Tuple<int, TSource>> WithIndex<TSource>(this IEnumerable<TSource> source, int startFrom = 0)
        {
            return source.Select((x,i) => Tuple.Create(i+startFrom,x));
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T t in source)
                action(t);
        }

        [Pure]
        public static string AsString(this IEnumerable<char> source)
        {
            return new string(source.ToArray());
        }

        [Pure]
        public static string StringJoin(this IEnumerable<char> source, string separator)
        {
            if (separator == "")
                return source.AsString();
            else
                return source.Select(c => c.ToString()).StringJoin("");
        }

        // http://www.interact-sw.co.uk/iangblog/2007/12/13/natural-sorting
        /// <summary>
        /// Attempts to perform at "natural" sort, where numbers inside strings are sorted correctly. E.g. ["temp", "test1", "test2", "test03", "test10x", "test100", "tree"]
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="stringSelector"></param>
        /// <param name="useFloatingPoint"> </param>
        /// <returns></returns>
        [Pure]
        public static IOrderedEnumerable<TSource> OrderNaturallyBy<TSource>(this IEnumerable<TSource> source, Func<TSource, string> stringSelector, bool useFloatingPoint = false)
        {
            return source.OrderBy(item => NaturalStringComparer.SplitByNumberOrString(stringSelector(item), useFloatingPoint), new EnumerableComparer<object>());
        }

        [Pure]
        public static IOrderedEnumerable<TSource> OrderNaturallyByDescending<TSource>(this IEnumerable<TSource> source, Func<TSource, string> stringSelector, bool useFloatingPoint = false)
        {
            return source.OrderByDescending(item => NaturalStringComparer.SplitByNumberOrString(stringSelector(item), useFloatingPoint), new EnumerableComparer<object>());
        }

        /// <summary>
        /// Attempts to perform at "natural" sort, where numbers inside strings are sorted correctly. E.g. ["temp", "test1", "test2", "test03", "test10x", "test100", "tree"]
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="stringSelector"></param>
        /// <param name="useFloatingPoint"> </param>
        /// <returns></returns>
        [Pure]
        public static IOrderedEnumerable<TSource> ThenNaturallyBy<TSource>(this IOrderedEnumerable<TSource> source, Func<TSource, string> stringSelector, bool useFloatingPoint = false)
        {
            return source.ThenBy(item => NaturalStringComparer.SplitByNumberOrString(stringSelector(item), useFloatingPoint), new EnumerableComparer<object>());
        }

        [Pure]
        public static IOrderedEnumerable<TSource> ThenNaturallyByDescending<TSource>(this IOrderedEnumerable<TSource> source, Func<TSource, string> stringSelector, bool useFloatingPoint = false)
        {
            return source.ThenByDescending(item => NaturalStringComparer.SplitByNumberOrString(stringSelector(item), useFloatingPoint), new EnumerableComparer<object>());
        }

        /// <summary>
        /// Attempts to perform at "natural" sort, where numbers inside strings are sorted correctly. E.g. ["temp", "test1", "test2", "test03", "test10x", "test100", "tree"]
        /// </summary>
        /// <param name="source"></param>
        /// <param name="useFloatingPoint"> </param>
        /// <returns></returns>
        [Pure]
        public static IOrderedEnumerable<string> OrderNaturally(this IEnumerable<string> source, bool useFloatingPoint = false)
        {
            return source.OrderNaturallyBy(x => x, useFloatingPoint);
        }

        [Pure]
        public static Tuple<Option<T>, IEnumerable<T>> HeadAndTail<T>(this IEnumerable<T> source)
        {
            if (source == null) throw new ArgumentNullException("source");
            var x = HeadAndTailEnumerable<T>.GetHeadsAndTail(source, 1);
            return Tuple.Create(x.Item1.OptionFirst(), x.Item2);
        }

        [Pure]
        public static Tuple<T[], IEnumerable<T>> HeadsAndTail<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            return HeadAndTailEnumerable<T>.GetHeadsAndTail(source, count);
        }

        private class HeadAndTailEnumerable<T> : IEnumerable<T>
        {
            public static Tuple<T[], IEnumerable<T>> GetHeadsAndTail(IEnumerable<T> source, int headCount)
            {
                if (headCount <= 0)
                    return Tuple.Create(new T[0], source);

                var enumerator = source.GetEnumerator();

                var list = new List<T>();
                for (int i = 0; i < headCount && enumerator.MoveNext(); i++)
                    list.Add(enumerator.Current);

                IEnumerable<T> theRest = new HeadAndTailEnumerable<T> {Enumerator = enumerator, SkippedSource = source.Skip(headCount)};

                return Tuple.Create(list.ToArray(), theRest);
            }

            private IEnumerator<T> Enumerator;
            private IEnumerable<T> SkippedSource;

            public IEnumerator<T> GetEnumerator()
            {
                if (Enumerator != null)
                {
                    var e = Enumerator;
                    Enumerator = null;
                    return e;
                }

                return SkippedSource.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }

        [Pure]
        public static IEnumerable<TSource> Take<TSource>(this IEnumerable<TSource> source, Option<int> count)
        {
            return count.IsSome ? source.Take(count.Get()) : source;
        }

        [Pure]
        public static IEnumerable<TSource> TakeUntilInclusive<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            foreach (var item in source)
            {
                yield return item;
                if (predicate(item))
                    yield break;
            }
        }

        /// <summary>
        /// Like TakeWhile, but includes one more element (the one that fails the test).
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="source"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<TSource> TakeUntil<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
        {
            if (source == null) throw new ArgumentNullException("source");
            foreach (var item in source)
            {
                yield return item;
                if (!predicate(item))
                    yield break;
            }
        }

        [Pure]
        public static Option<T> OptionElementAt<T>(this IEnumerable<T> source, int index)
        {
            return source.Select(x => Option.GetSome(x)).ElementAtOrDefault(index);
        }

        public static Option<T> OptionElementAt<T>(this IList<T> source, int index)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (index >= 0 && index < source.Count)
                return Option.GetSome(source[index]);
            return Option.None;
        }

        [Pure]
        public static IEnumerable<T> SkipLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0)
                return source;
            if (count == 1)
                return _SkipLast1(source);
            if (count == 2)
                return _SkipLast2(source);

            return _SkipLastN(source, count);
            //return source.SlidingWindow(count+1)
            //             .Skip(count)
            //             .TakeWhile(window => window.Length > count)
            //             .Select(window => window[0]);
        }

        private static IEnumerable<T> _SkipLast1<T>(IEnumerable<T> source)
        {
            using(var it = source.GetEnumerator())
            {
                if (!it.MoveNext())
                    yield break;
                T last = it.Current;
                while (it.MoveNext())
                {
                    yield return last;
                    last = it.Current;
                }
            }
        }

        private static IEnumerable<T> _SkipLast2<T>(IEnumerable<T> source)
        {
            using (var it = source.GetEnumerator())
            {
                if (!it.MoveNext())
                    yield break;
                T twoBack = it.Current;
                if (!it.MoveNext())
                    yield break;
                T oneBack = it.Current;
                while (it.MoveNext())
                {
                    yield return twoBack;
                    twoBack = oneBack;
                    oneBack = it.Current;
                }
            }
        }

        private static IEnumerable<T> _SkipLastN<T>(IEnumerable<T> source, int count)
        {
            var buf = new CircularBuffer<T>(count);
            foreach (var item in source)
            {
                if(buf.Count == count)
                    yield return buf.First();
                buf.Add(item);
            }
        }

        [Pure]
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int count)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (count <= 0)
                return new T[0];

            if (count == 1)
                return source.OptionLast().ToArray();

            if (count == 2)
                return _TakeLast2(source);

            return _TakeLast(source, count);
        }

        private static IEnumerable<T> _TakeLast2<T>(IEnumerable<T> source)
        {
            using (var it = source.GetEnumerator())
            {
                if (!it.MoveNext())
                    return new T[0];
                T twoBack = it.Current;
                if (!it.MoveNext())
                    return new[] {twoBack};
                T oneBack = it.Current;
                while (it.MoveNext())
                {
                    twoBack = oneBack;
                    oneBack = it.Current;
                }
                return new[] { twoBack, oneBack };
            }
        }

        private static IEnumerable<T> _TakeLast<T>(this IEnumerable<T> source, int count)
        {
            var buf = new CircularBuffer<T>(count);
            foreach (var item in source)
                buf.Add(item);
            return buf.ToArray();
        }

        [Pure]
        public static IEnumerable<T> TakeLast<T>(this IList<T> list, int count)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (count <= 0)
                return new T[0];
            var listCount = list.Count;
            if (count >= listCount)
                return list;

            return _Skip(list, listCount-count);
        }

        [Pure]
        public static Option<T> OptionFirst<T>(this IEnumerable<T> source)
        {
            return source.Select(x => Option.GetSome(x)).FirstOrDefault();
        }

        [Pure]
        public static Option<T> OptionSingle<T>(this IEnumerable<T> source)
        {
            var firstTwo = source.Take(2).ToArray();
            return firstTwo.Length == 1 ? Option.GetSome(firstTwo[0]) : Option.None;
        }

        [Pure]
        public static Option<T> OptionLast<T>(this IEnumerable<T> source)
        {
            return source.Select(x => Option.GetSome(x)).LastOrDefault();
        }

        [Pure]
        public static Option<T> OptionLast<T>(this IList<T> source)
        {
            if (source.Count == 0)
                return Option.None;
            return Option.GetSome(source.Last());
        }

        [Pure]
        public static Option<T> OptionAggregate<T>(this IEnumerable<T> source, Func<T,T,T> func)
        {
            var headAndTail = source.HeadAndTail();
            if(headAndTail.Item1.IsNone)
                return Option.None;
            return Option.GetSome(headAndTail.Item2.Aggregate(headAndTail.Item1.Get(), func));
        }

        [Pure]
        public static IEnumerable<T> Skip<T>(this IList<T> list, int count)
        {
            if (list == null) throw new ArgumentNullException("list");
            if (count <= 0)
                return list;

            if (count <= 10)
                return  list.CastTo<IEnumerable<T>>().Skip(count);

            return _Skip(list, count);
        }

        [Pure]
        public static IEnumerable<T> SkipLast<T>(this IList<T> list, int count)
        {
            if (list == null) throw new ArgumentNullException("list");
            var listCount = list.Count;
            if (count <= 0)
                return list;

            if (listCount - count <= 0)
                return Enumerable.Empty<T>();

            return list.Take(listCount - count);
        }

        private static IEnumerable<T> _Skip<T>(IList<T> list, int count)
        {
            using(var it = list.IsReadOnly ? null : list.GetEnumerator())
                for (int i = count; i < list.Count; i++)
                {
                    if (it != null) it.MoveNext(); // to get InvalidOperationException
                    yield return list[i];
                }
        }

        /// <summary>
        /// ToDict rather than ToDictionary to avoid name clash in buggy software.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="source"></param>
        /// <param name="comparer"> </param>
        /// <returns></returns>
        [Pure]
        public static Dictionary<TKey, TValue> ToDict<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToDictionary(x => x.Key, x => x.Value, comparer);
        }

        [Pure]
        public static Dictionary<TKey, TValue> ToDict<TKey, TValue>(this IEnumerable<Tuple<TKey, TValue>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToDictionary(x => x.Item1, x => x.Item2, comparer);
        }

        [Pure]
        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            var dict = source as Dictionary<TKey, TValue>;
            dict = dict ?? source.ToDictionary(kv => kv.Key, kv => kv.Value);
            return new SortedDictionary<TKey, TValue>(dict);
        }

        [Pure]
        public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source, IEqualityComparer<T> comparer = null)
        {
            return new HashSet<T>(source, comparer);
        }

        [Pure]
        public static HashSet<T> ToHashSetUsingReferenceEquals<T>(this IEnumerable<T> source) where T : class
        {
            return new HashSet<T>(source, new IdentityEqualityComparer<T>());
        }

        [Pure]
        public static Dictionary<TKey, TValue> WhereDict<TKey, TValue>(this Dictionary<TKey, TValue> source, Func<TKey, TValue, bool> predicate, IEqualityComparer<TKey> comparer = null)
        {
            comparer = comparer ?? source.Comparer;
            return source.Where(kv => predicate(kv.Key, kv.Value)).ToDictionary(x => x.Key, x => x.Value, comparer);
        }

        [Pure]
        public static IEnumerable<Tuple<TKey, TValue>> ToTuples<TKey, TValue>(this Dictionary<TKey, TValue> source)
        {
            return source.Select(x => Tuple.Create(x.Key, x.Value));
        }

        [Pure]
        public static Dictionary<TKey, List<T>> ToMultiDictionary<T, TKey>(this IEnumerable<KeyValuePair<TKey,T>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToMultiDictionary(x => x.Key, x => x.Value, comparer);
        }

        [Pure]
        public static Dictionary<TKey, List<T>> ToMultiDictionary<T, TKey>(this IEnumerable<Tuple<TKey,T>> source, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToMultiDictionary(x => x.Item1, x => x.Item2, comparer);
        }

        [Pure]
        public static Dictionary<TKey, List<T>> ToMultiDictionary<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keyFunc, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToMultiDictionary(keyFunc, x => x, comparer);
        }

        [Pure]
        public static Dictionary<TKey, List<TElement>> ToMultiDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer = null)
        {
            var result = new Dictionary<TKey, List<TElement>>(comparer);
            foreach (var item in source)
            {
                var key = keySelector(item);
                result.GetValueOrAddNew(key).Add(elementSelector(item));
            }
            return result;
        }

        [Pure]
        public static HashSet<T> IntersectionWith<T>(this HashSet<T> source, HashSet<T> other)
        {
            if (source.Count > other.Count)
                return other.IntersectionWith(source);

            var newSet = source.ToHashSet(source.Comparer);
            newSet.IntersectWith(other);
            return newSet;
        }

        /// <summary>
        /// Partition by a predicate. new []{ 1, 2, -3, 1, -5}.PartitionBy(x => x > 0) will return the tuple: ([1, 2, 1], [-3, -5]).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="keyFunc"></param>
        /// <returns></returns>
        [Pure]
        public static Tuple<T[], T[]> PartitionBy<T>(this IEnumerable<T> source, Func<T, bool> keyFunc)
        {
            var multiDictionary = source.ToMultiDictionary(keyFunc);
            return Tuple.Create(multiDictionary.OptionGetValue(true).Transform(x => x.ToArray()).Else(new T[0]),
                                multiDictionary.OptionGetValue(false).Transform(x => x.ToArray()).Else(new T[0]));
        }

        [Pure]
        public static Dictionary<TKey, List<T>> PartitionBy<T, TKey>(this IEnumerable<T> source, Func<T, TKey> keyFunc, IEqualityComparer<TKey> comparer = null)
        {
            return source.ToMultiDictionary(keyFunc, comparer);
        }

        [Pure]
        public static IEnumerable<TSource> DistinctByMany<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, IEnumerable<TKey>> keyFunction, IEqualityComparer<TKey> comparer = null)
        {
            var encountered = new HashSet<TKey>(comparer);
            foreach (var item in source)
            {
                bool added = false;
                foreach (var key in keyFunction(item))
                    added = encountered.Add(key) || added;
                if (added)
                    yield return item;
            }
        }

        [Pure]
        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keyFunction, IEqualityComparer<TKey> comparer = null)
        {
            return source.DistinctByMany(x => new[] {keyFunction(x)}, comparer);
        }

        [Pure]
        public static IEnumerable<Option<TSource>> DistinctByInner<TSource, TKey>(this IEnumerable<Option<TSource>> source, Func<TSource, TKey> keyFunction, IEqualityComparer<TKey> comparer = null)
        {
            return source.DistinctByManyConditional(x => x.Transform(y => new[] { keyFunction(y) }.AsEnumerable()), comparer);
        }

        [Pure]
        public static IEnumerable<TSource> DistinctByManyConditional<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, Option<IEnumerable<TKey>>> keyFunction, IEqualityComparer<TKey> comparer = null)
        {
            var encountered = new HashSet<TKey>(comparer);
            foreach (var item in source)
            {
                bool added = false;
                var keys = keyFunction(item);
                if (keys.IsNone)
                {
                    added = true;
                }
                else
                {
                    foreach (var key in keys.Get())
                        added = encountered.Add(key) || added;
                }
                if (added)
                    yield return item;
            }
        }

        public static T RemoveAndReturnLast<T>(this List<T> returnValues)
        {
            var lastIndex = returnValues.Count - 1;
            var returnValue = returnValues[lastIndex];
            returnValues.RemoveAt(lastIndex);
            return returnValue;
        }

        [Pure]
        public static IEnumerable<TResult> Zip3<TFirst, TSecond, TThird, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, Func<TFirst, TSecond, TThird, TResult> resultSelector)
        {
            if(resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            return first.Zip(second, Tuple.Create).Zip(third, (t, x) => resultSelector(t.Item1, t.Item2, x));
        }

        [Pure]
        public static IEnumerable<TResult> Zip4<TFirst, TSecond, TThird, TFourth, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, IEnumerable<TFourth> fourth, Func<TFirst, TSecond, TThird, TFourth, TResult> resultSelector)
        {
            if(resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            return first.Zip(second, Tuple.Create).Zip3(third, fourth, (t, x, x2) => resultSelector(t.Item1, t.Item2, x, x2));
        }

        [Pure]
        public static IEnumerable<TResult> Zip5<TFirst, TSecond, TThird, TFourth, TFifth, TResult>(this IEnumerable<TFirst> first, IEnumerable<TSecond> second, IEnumerable<TThird> third, IEnumerable<TFourth> fourth, IEnumerable<TFifth> fifth, Func<TFirst, TSecond, TThird, TFourth, TFifth, TResult> resultSelector)
        {
            if(resultSelector == null)
                throw new ArgumentNullException("resultSelector");
            return first.Zip(second, Tuple.Create).Zip4(third, fourth, fifth, (t, x, x2, x3) => resultSelector(t.Item1, t.Item2, x, x2, x3));
        }

        [Pure]
        public static T Last<T>(this IList<T> list)
        {
            return list[list.Count - 1];
        }

        [Pure]
        public static bool Any<T>(this IList<T> col)
        {
            return col.Count != 0;
        }

        [Pure]
        public static bool Any<T>(this ICollection<T> col)
        {
            return col.Count != 0;
        }

        [Pure]
        public static bool Any<T>(this Queue<T> col)
        {
            return col.Count != 0;
        }

        [Pure]
        public static List<T> Sorted<T>(this IEnumerable<T> list)
        {
            return list.OrderBy(x => x).ToList();
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                collection.Add(item);
            }
        }

        [Pure]
        public static IEnumerable<T> Append<T>(this IEnumerable<T> source, params T[] appendees)
        {
            return source.Concat(appendees);
        }

        [Pure]
        public static IEnumerable<T> Reversed<T>(this IList<T> source)
        {
            for (int i = source.Count - 1; i >= 0; i--)
                yield return source[i];
        }

        [Pure]
        public static IEnumerable<T> Reversed<T>(this IEnumerable<T> source)
        {
            return source.Reverse();
        }

        [Pure]
        public static IEnumerable<T> Prepend<T>(this IEnumerable<T> source, params T[] prependees)
        {
            return prependees.Concat(source);
        }

        [Pure]
        public static IEnumerable<T> ConcatLazy<T>(this IEnumerable<T> first, Lazy<IEnumerable<T>> second)
        {
            return first.ConcatLazy(() => second.Value);
        }

        [Pure]
        public static IEnumerable<T> ConcatLazy<T>(this IEnumerable<T> first, Func<IEnumerable<T>> second)
        {
            foreach (var item in first)
                yield return item;

            foreach (var item in second())
                yield return item;
        }

        [Obsolete]
        public static HashSet<T> ToSet<T>(this IEnumerable<T> source, IEqualityComparer<T> equalityComparer = null)
        {
            return source.ToHashSet(equalityComparer);
        }

        // http://stackoverflow.com/questions/419019/split-list-into-sublists-with-linq/419058#419058
        [Pure]
        public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> enumerable, int chunkSize)
        {
            if(chunkSize < 1)
                throw new ArgumentException("chunkSize must be >= 1", "chunkSize");

            var list = new List<T>(chunkSize);
            foreach (var item in enumerable)
            {
                list.Add(item);

                if (list.Count == chunkSize)
                {
                    yield return list.ToArray();
                    list.Clear();
                }
            }

            if (list.Count != 0)
                yield return list.ToArray();
        }

        [Pure]
        public static IEnumerable<T[]> ChunkBy<T>(this IEnumerable<T> source, IEnumerable<int> chunkSizes, bool skipTail = false)
        {
            using(var it = source.GetEnumerator())
            {
                foreach (var chunkSize in chunkSizes)
                {
                    if (chunkSize < 1)
                        throw new ArgumentException("chunkSize must be >= 1", "chunkSizes");

                    var list = new List<T>(chunkSize);
                    while (it.MoveNext())
                    {
                        var item = it.Current;
                        list.Add(item);

                        if (list.Count == chunkSize)
                        {
                            yield return list.ToArray();
                            list.Clear();
                            break;
                        }
                    }
                    if (list.Count != 0)
                        yield return list.ToArray();
                }

                if (skipTail)
                    yield break;

                var tail = new List<T>();
                while (it.MoveNext())
                    tail.Add(it.Current);
                if (tail.Any())
                    yield return tail.ToArray();
            }
        }


        /// <summary>
        /// A sliding window of max size windowSize. A windowSize of 2 and [1,2,3,4] would give [1], [1,2], [2,3], [3,4], [4]. [1,2] would give [1], [1,2], [2]. Note that for consistency a single element gives: [1], [1].
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="windowSize"></param>
        /// <returns></returns>
        [Pure]
        public static IEnumerable<T[]> SlidingWindow<T>(this IEnumerable<T> source, int windowSize)
        {
            if(windowSize == 0)
                return new T[0][];

            if(windowSize == 1)
                return source.Select(x => new[]{x});

            if(windowSize == 2)
                return _SlidingWindow2(source);

            return _SlidingWindow(source, windowSize);
            
        }

        private static IEnumerable<T[]> _SlidingWindow<T>(IEnumerable<T> source, int windowSize)
        {
            var buffer = new CircularBuffer<T>(windowSize);
            foreach (var item in source)
            {
                buffer.Add(item);
                yield return buffer.ToArray();
            }
            var last = buffer.ToArray();
            if(last.Any())
                for (int count = windowSize - 1; count >= 1; count--)
                    yield return last.TakeLast(count).ToArray();
        }

        private static IEnumerable<T[]> _SlidingWindow2<T>(IEnumerable<T> source)
        {
            using(var it = source.GetEnumerator())
            {
                if (!it.MoveNext())
                    yield break;
                T last = it.Current;
                yield return new[] { last };
                while (it.MoveNext())
                {
                    var current = it.Current;
                    yield return new[] { last, current };
                    last = current;
                }
                yield return new[] { last };
            }
        }

        [Obsolete]
        public static IEnumerable<T[]> SlidingWindowOld<T>(this IEnumerable<T> source, int windowSize)
        {
            if(windowSize == 0)
                return new T[0][];

            if(windowSize == 1)
                return source.Select(x => new[]{x});

            if(windowSize == 2)
                return _SlidingWindow2Old(source);

            return _SlidingWindowOld(source, windowSize);
        }

        private static IEnumerable<T[]> _SlidingWindow2Old<T>(IEnumerable<T> source)
        {
            using (var it = source.GetEnumerator())
            {
                if (!it.MoveNext())
                    yield break;
                T last = it.Current;
                yield return new[] { last };
                bool enteredWhile = false;
                while (it.MoveNext())
                {
                    enteredWhile = true;
                    var current = it.Current;
                    yield return new[] { last, current };
                    last = current;
                }
                if (enteredWhile)
                    yield return new[] { last };
            }
        }

        private static IEnumerable<T[]> _SlidingWindowOld<T>(IEnumerable<T> source, int windowSize)
        {
            var buffer = new CircularBuffer<T>(windowSize);
            foreach (var item in source)
            {
                buffer.Add(item);
                yield return buffer.ToArray();
            }
            var last = buffer.ToArray();
            foreach (var index in Enumerable.Range(0, last.Length).Skip(1))
                yield return last.Skip(index).ToArray();
        }

        // TODO: optimize to O(n)
        [Pure]
        public static Option<TSource> MaxByOption<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func)
        {
            return source.OrderByDescending(func).OptionFirst();
        }

        // TODO: optimize to O(n)
        [Pure]
        public static TSource MaxBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func)
        {
            return source.OrderByDescending(func).First();
        }

        // TODO: optimize to O(n)
        [Pure]
        public static Option<TSource> MinByOption<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func)
        {
            return source.OrderBy(func).OptionFirst();
        }

        // TODO: optimize to O(n)
        [Pure]
        public static TSource MinBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> func)
        {
            return source.OrderBy(func).First();
        }

        [Pure]
        public static List<T> Shuffle<T>(this IEnumerable<T> source)
        {
            var list = source.ToList();
            lock (Utils.StaticRandom.Lock)
            {
                int n = list.Count;
                while (n > 1)
                {
                    n--;
                    int k = Utils.StaticRandom.Next(n + 1);
                    T value = list[k];
                    list[k] = list[n];
                    list[n] = value;
                }
            }
            return list;
        }

        [Pure]
        public static Dictionary<string, string[]> ToDictionary(this System.Collections.Specialized.NameValueCollection source)
        {
            TypeCheck(source, s => s.ToDictionary()[""], s => s.GetValues(""));
            return source.AsEnumerable().ToDictionary(key => key, source.GetValues);
        }

        [System.Diagnostics.Conditional("ALWAYZ__EXKLUDE_ME_")]
        private static void TypeCheck<TSource, TItem>(TSource t1, Func<TSource, TItem> f1, Func<TSource, TItem> f2)
        {
        }
    }

    public static class SystemExtensionMethods
    {
        public static DisposeOnGCWrapper<T> WrapToDisposeOnGC<T>(this T value) where T : IDisposable
        {
            return new DisposeOnGCWrapper<T>(value);
        }

        public static Func<TKey, TResult> UseToMemoizeFunc<TKey, TResult>(this Dictionary<TKey, TResult> source, Func<TKey, TResult> function)
        {
            return key => source.UseAsCacheFor(function, key);
        }

        public static TResult UseAsCacheFor<TKey, TResult>(this Dictionary<TKey, TResult> source, Func<TKey, TResult> function, TKey key)
        {
            var existingValue = source.OptionGetValue(key);
            if(existingValue.IsSome)
                return existingValue.Get();

            var value = function(key);
            source[key] = value;
            return value;
        }

        // http://stackoverflow.com/questions/444798/case-insensitive-containsstring
        [Pure]
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }

        [Pure]
        public static string Replace(this string source, string oldValue, string newValue, StringComparison comparisonType)
        {
            if (source.Length == 0 || oldValue.Length == 0)
                return source;

            var result = new System.Text.StringBuilder();
            int startingPos = 0;
            int nextMatch;
            while ((nextMatch = source.IndexOf(oldValue, startingPos, comparisonType)) > -1)
            {
                result.Append(source, startingPos, nextMatch - startingPos);
                result.Append(newValue);
                startingPos = nextMatch + oldValue.Length;
            }
            result.Append(source, startingPos, source.Length - startingPos);

            return result.ToString();
        }


        [Pure]
        public static Option<double> OptionParseDouble(this string source)
        {
            double d;
            source = source.Replace(',', '.');
            if (double.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                return Option.GetSome(d);
            return Option.None;
        }

        [Pure]
        public static Option<long> OptionParseLong(this string source)
        {
            long f;
            if (long.TryParse(source, out f))
                return Option.GetSome(f);
            return Option.None;
        }

        [Pure]
        public static Option<decimal> OptionParseDecimal(this string source)
        {
            decimal d;
            source = source.Replace(',', '.');
            if (decimal.TryParse(source, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
                return Option.GetSome(d);
            return Option.None;
        }

        [Pure]
        public static Option<int> OptionParseInt(this string source)
        {
            int f;
            if(int.TryParse(source, out f))
                return Option.GetSome(f);
            return Option.None;
        }

        /// <summary>
        /// The elements of the array are sorted by the binary values of the enumeration constants (that is, by their unsigned magnitude).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static T[] GetValuesFromEnumType<T>(this T value) where T : struct, IConvertible
        {
            return GetValuesFromEnumType<T>();
        }

        /// <summary>
        /// The elements of the array are sorted by the binary values of the enumeration constants (that is, by their unsigned magnitude).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [Pure]
        public static T[] GetValuesFromEnumType<T>() where T : struct, IConvertible
        {
            return Enum.GetValues(GetEnumType<T>()).CastTo<T[]>();
        }

        /// <summary>
        /// The elements of the array are sorted by the binary values of the enumeration constants (that is, by their unsigned magnitude).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static string[] GetNamesFromEnumType<T>(this T value) where T : struct, IConvertible
        {
            return Enum.GetNames(GetEnumType<T>());
        }

        private static Type GetEnumType<T>() where T : struct, IConvertible
        {
            Type type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException("T must be an enum type");
            return type;
        }

        public static TResult WithDefaultIfNull<TArg, TResult>(this TArg source, TResult defaultValue, Func<TArg, TResult> fun) where TArg : class
        {
            if (source == null)
                return defaultValue;
            return fun(source);
        }

        public static TResult WithDefaultIfNull<TArg, TResult>(this TArg source, Func<TArg, TResult> fun) where TArg : class
        {
            if (source == null)
                return default(TResult);
            return fun(source);
        }

        public static TResult WithDefaultOnException<TArg, TResult>(this TArg source, TResult defaultValue, Func<TArg, TResult> fun)
        {
            try
            {
                return fun(source);
            }
            catch (Exception)
            {
                return defaultValue;
            }
        }

        //// http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        //[System.Diagnostics.DebuggerNonUserCode]
        // DebuggerNonUserCode doesn't help here
        public static TResult WithDefaultOnException<TArg, TResult>(this TArg source, Func<TArg, TResult> fun)
        {
            return source.WithDefaultOnException(default(TResult), fun);
        }

        [Pure]
        public static string[] Split(this string source, string separator, int? count = null)
        {
            if (count == null)
                return source.Split(new[] { separator }, StringSplitOptions.None);
            else
                return source.Split(new[] { separator }, count.Value, StringSplitOptions.None);
        }


        [Pure]
        public static string[] Lines(this string source)
        {
            return source.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        }


        [Pure]
        public static decimal Normalize(this decimal value)
        {
            return value / 1.000000000000000000000000000000000m;
        }

        [Pure]
        public static bool NotFinite(this double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value);
        }

        [Pure]
        public static string StringJoin(this IEnumerable<string> source, string separator)
        {
            return string.Join(separator, source);
        }

        [Pure]
        public static bool DictionaryEqualTo<TKey, TValue>(
            this IDictionary<TKey, TValue> first, IDictionary<TKey, TValue> second)
        {
            if (ReferenceEquals(first, second)) return true;
            if (first == null || second == null) return false;
            if (first.Count != second.Count) return false;

            var comparer = EqualityComparer<TValue>.Default;

            foreach (var kvp in first)
            {
                TValue secondValue;
                if (!second.TryGetValue(kvp.Key, out secondValue)) return false;
                if (!comparer.Equals(kvp.Value, secondValue)) return false;
            }
            return true;
        }

        [Pure]
        public static TValue GetValueOrAddNew<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : new()
        {
            var option = dictionary.OptionGetValue(key);
            if (option.IsSome)
                return option.Get();

            var value = new TValue();
            dictionary[key] = value;
            return value;
        }

        [Pure]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue)
        {
            if (dictionary.ContainsKey(key))
                return dictionary[key];
            else
                return defaultValue;
        }

        [Pure]
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue ret;
            // Ignore return value
            dictionary.TryGetValue(key, out ret);
            return ret;
        }

        [Pure]
        public static TValue GetValueOrNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
            where TValue : class
        {
            if (key == null)
                return null;
            return dictionary.GetValueOrDefault(key);
        }

        [Pure]
        public static Option<TValue> OptionGetValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            TValue ret;
            if (dictionary.TryGetValue(key, out ret))
                return Option.GetSome(ret);
            else
                return Option.None;
        }

        [Pure]
        public static bool AnyOf<T>(this T needle, params T[] haystack) where T : struct, IComparable, IFormattable, IConvertible // where T : Enum
        {
            return haystack.Contains(needle);
        }

        #region Stream.CopyTo: http://stackoverflow.com/a/1253049/579344

        public static void CopyTo(this System.IO.Stream src, System.IO.Stream dest)
        {
            int size = (src.CanSeek) ? Math.Min((int)(src.Length - src.Position), 0x2000) : 0x2000;
            byte[] buffer = new byte[size];
            int n;
            do
            {
                n = src.Read(buffer, 0, buffer.Length);
                dest.Write(buffer, 0, n);
            } while (n != 0);
        }

        public static void CopyTo(this System.IO.MemoryStream src, System.IO.Stream dest)
        {
            dest.Write(src.GetBuffer(), (int)src.Position, (int)(src.Length - src.Position));
        }

        public static void CopyTo(this System.IO.Stream src, System.IO.MemoryStream dest)
        {
            if (src.CanSeek)
            {
                int pos = (int)dest.Position;
                int length = (int)(src.Length - src.Position) + pos;
                dest.SetLength(length);

                while (pos < length)
                    pos += src.Read(dest.GetBuffer(), pos, length - pos);
            }
            else
                src.CopyTo(dest.CastTo<System.IO.Stream>());
        }

        #endregion

        [Pure]
        public static bool EqualsIgnoreCase(this string source, string value)
        {
            // http://msdn.microsoft.com/en-us/library/ms973919.aspx
            // DO: Switch current use of string operations based on the invariant 
            //     culture to use the non-linguistic StringComparison.Ordinal or 
            //     StringComparison.OrdinalIgnoreCase when the comparison is
            //     linguistically irrelevant (symbolic, for example).

            return source.Equals(value, StringComparison.OrdinalIgnoreCase);
        }

        [Pure]
        public static string TrimStringAtStart(this string source, string stringToRemove, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (source == null)
                return null;
            if (source.StartsWith(stringToRemove, comparisonType))
                source = source.Substring(stringToRemove.Length);

            return source;
        }

        [Pure]
        public static string TrimStringAtEnd(this string source, string stringToRemove, StringComparison comparisonType = StringComparison.Ordinal)
        {
            if (source == null)
                return null;
            if (source.EndsWith(stringToRemove, comparisonType))
                source = source.Substring(0, source.Length - stringToRemove.Length);

            return source;
        }

        [Pure]
        public static string ToPrettyFormat(this TimeSpan span)
        {
            if (span == TimeSpan.Zero) return "0 minutes";

            var units = new[]
            {
                new{ val = span.Days, text="day"},
                new{ val = span.Hours, text="hour"},
                new{ val = (int)Math.Round(span.Minutes + span.Seconds/60f), text="minute"},
            }.SkipWhile(x => x.val == 0)
             .Take(2)
             .Where(x => x.val > 0)
             .DefaultIfEmpty(new { val = span.Seconds, text = "second" })
             .ToArray();

            var strings = units.Select(x => string.Format("{0} {1}{2}", x.val, x.text, x.val > 1 ? "s" : ""));
            return string.Join(" ", strings.ToArray());
        }

        [Pure]
        public static TValue? GetValueOrNullable<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key) where TValue : struct
        {
            if (key == null)
                return null;
            if (!dictionary.ContainsKey(key))
                return null;
            return dictionary[key];
        }

        public static void DisposeUnlessNull<T>(this T disposable, ref T disposableRef) where T : class, IDisposable
        {
            if(!ReferenceEquals(disposable, disposableRef))
                throw new ArgumentException("Expected (disposable) this == disposableRef");
            Utils.DisposeUnlessNull(ref disposableRef);
        }

        public static void DisposeUnlessNone<T>(this Option<T> disposable, ref Option<T> disposableRef) where T : IDisposable
        {
            if (disposable.IsSome)
            {
                disposable.Get().Dispose();
                disposableRef = Option.None;
            }
        }


        [Pure]
        public static bool IsTrue(this bool? b)
        {
            return b.HasValue && b.Value;
        }

        [Pure]
        public static bool IsFalse(this bool? b)
        {
            return b.HasValue && !b.Value;
        }

        [Pure]
        public static double RoundToOneSignificantDigit(this double x)
        {
            double exp = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(x)))); // 679 => 100, 100 => 100
            return Math.Round(x / exp) * exp;
        }

        [Pure]
        public static string GetFriendlyTypeName(this Type type, bool withNamespaces = false)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            if (!type.IsGenericType)
            {
                if (withNamespaces)
                    return type.FullName;
                else
                    return type.Name;
            }

            var builder = new System.Text.StringBuilder();
            var name = type.Name;
            var index = name.IndexOf("`", StringComparison.Ordinal);
            if (withNamespaces)
                builder.AppendFormat("{0}.{1}", type.Namespace, name.Substring(0, index));
            else
                builder.Append(name.Substring(0, index));
            builder.Append('<');
            var first = true;
            foreach (var arg in type.GetGenericArguments())
            {
                if (!first)
                {
                    builder.Append(',');
                }
                builder.Append(arg.GetFriendlyTypeName(withNamespaces));
                first = false;
            }
            builder.Append('>');
            return builder.ToString();
        }

        [Pure]
        public static EnumerableTryCatcher<T> Try<T>(this IEnumerable<T> source)
        {
            return new EnumerableTryCatcher<T>(source);
        }

        [Pure]
        public static CachingDisposableIEnumerable<T> WrapInLazyCache<T>(this IEnumerable<T> enumerable)
        {
            return new CachingDisposableIEnumerable<T>(enumerable.GetEnumerator());
        }

        [Pure]
        public static CachingDisposableIEnumerable<T> WrapInLazyCache<T>(this IEnumerator<T> enumerator)
        {
            return new CachingDisposableIEnumerable<T>(enumerator);
        }
        
        [Pure]
        public static DisposableIEnumerator<T> Cast<T>(this System.Collections.IEnumerator enumerator)
        {
            return DisposableIEnumerator<T>.Wrap(enumerator);
        }

        [Pure]
        public static CachingDisposableIEnumerableCaster<TEnumerator> WrapInLazyCache<TEnumerator>(this TEnumerator enumerator) where TEnumerator : System.Collections.IEnumerator, IDisposable
        {
            return new CachingDisposableIEnumerableCaster<TEnumerator>(enumerator);
        }
    }

    public static class DisposableWrapper
    {
        public static DisposableActionWrapper<T> Wrap<T>(T t, Action<T> action)
        {
            return new DisposableActionWrapper<T>(t, () => action(t));
        }
    }

    public class DisposableActionWrapper<T> : IDisposable
    {
        public T Value { get; private set; }
        private Action _disposeAction;
        public bool Disposed
        {
            get { return _disposeAction == null; }
        }

        public DisposableActionWrapper(T value, Action disposeAction)
        {
            if (disposeAction == null)
                throw new ArgumentNullException("disposeAction");
            _disposeAction = disposeAction;
            Value = value;
        }

        public void Dispose()
        {
            if (_disposeAction == null)
                return;

            var disposeAction = _disposeAction;
            _disposeAction = null;
            disposeAction();
            Value = default(T);
        }
    }



    public class DisposableList : List<IDisposable>, IDisposable
    {
        public void Dispose()
        {
            for (int i = 0; i < Count; i++)
            {
                var disposable = this[i];
                disposable.DisposeUnlessNull(ref disposable);
                this[i] = disposable;
            }
            Clear();
        }

        public T AddAndReturn<T>(T disp) where T : IDisposable
        {
            Add(disp);
            return disp;
        }
    }

    public class EnumerableTryCatcher<T>
    {
        private IEnumerable<T> _source;

        public EnumerableTryCatcher(IEnumerable<T> source)
        {
            _source = source;
        }

        [Pure]
        public IEnumerable<T> CatchInto<TException>(List<TException> sink) where TException : Exception
        {
            return CatchAndProcess<TException>(sink.Add);
        }

        [Pure]
        public IEnumerable<T> CatchAndProcess<TException>(Action<TException> actionOnException) where TException : Exception
        {
            using (var e = _source.GetEnumerator())
                while (true)
                {
                    T current;
                    try
                    {
                        if (!e.MoveNext())
                            yield break;
                        current = e.Current;
                    }
                    catch (TException ex)
                    {
                        actionOnException(ex);
                        continue;
                    }
                    yield return current;
                }
        }

        [Pure]
        public IEnumerable<T> CatchAndSkip<TException>() where TException : Exception
        {
            return CatchAndProcess<TException>(ex => { });
        }

        [Pure]
        public IEnumerable<T> CatchAndSkipAnyException()
        {
            return CatchAndSkip<Exception>();
        }

        [Pure]
        public IEnumerable<TException> CatchToEnumerable<TException>() where TException : Exception
        {
            bool hasValue = false;
            TException value = null;
            IEnumerable<T> wrappedSource = CatchAndProcess<TException>(ex =>
            {
                hasValue = true;
                value = ex;
            });
            foreach (var dummy in wrappedSource)
            {
                if (hasValue)
                    yield return value;
                hasValue = false;
                value = null;
            }
        }
    }

    public class CachingDisposableIEnumerableCaster<TEnumerable> : System.Collections.IEnumerable, IDisposable where TEnumerable : System.Collections.IEnumerator, IDisposable
    {
        private readonly TEnumerable _enumerable;
        private CachingDisposableIEnumerable<object> _cacher;

        public CachingDisposableIEnumerableCaster(TEnumerable enumerable)
        {
            this._enumerable = enumerable;
        }

        public CachingDisposableIEnumerable<T> Cast<T>()
        {
            return new CachingDisposableIEnumerable<T>(_enumerable.Cast<T>());
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            if (_cacher == null)
                _cacher = Cast<object>();
            return _cacher.GetEnumerator();
        }

        public void Dispose()
        {
            _cacher.DisposeUnlessNull(ref _cacher);
        }
    }

    public static class DisposableIEnumerator
    {
        public static DisposableIEnumerator<TEnumerator> WrapAsDisposable<TEnumerator>(this TEnumerator source) where TEnumerator : System.Collections.IEnumerator, IDisposable
        {
            return new DisposableIEnumerator<TEnumerator>(source, source);
        }
    }

    public class DisposableIEnumerator<T> : IEnumerator<T>
    {
        private System.Collections.IEnumerator _sourceEnumerator;
        private IDisposable _sourceDisposable;

        public DisposableIEnumerator(System.Collections.IEnumerator sourceEnumerator, IDisposable sourceDisposable)
        {
            _sourceEnumerator = sourceEnumerator;
            _sourceDisposable = sourceDisposable;
        }

        public static DisposableIEnumerator<T> Wrap<TEnumerator>(TEnumerator source) where TEnumerator : System.Collections.IEnumerator, IDisposable
        {
            return new DisposableIEnumerator<T>(source, source);
        }

        public static DisposableIEnumerator<T> Wrap(System.Collections.IEnumerator source)
        {
            return new DisposableIEnumerator<T>(source, source as IDisposable);
        }

        public void Dispose()
        {
            _sourceDisposable.DisposeUnlessNull(ref _sourceDisposable);
            _sourceEnumerator = null;
        }

        public bool MoveNext()
        {
            return _sourceEnumerator.MoveNext();
        }

        public void Reset()
        {
            _sourceEnumerator.Reset();
        }

        public T Current
        {
            get { return (T) _sourceEnumerator.Current; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return _sourceEnumerator.Current; }
        }
    }

    public class CachingDisposableIEnumerable<T> : IEnumerable<T>, IDisposable
    {
        private IEnumerator<T> enumerator;
        private List<T> cache = new List<T>();

        public CachingDisposableIEnumerable(IEnumerator<T> enumerator)
        {
            this.enumerator = enumerator;
        }

        public IEnumerator<T> GetEnumerator()
        {
            int i = 0;
            while (true)
            {
                if (i >= cache.Count)
                {
                    if (enumerator != null && !enumerator.MoveNext())
                    {
                        enumerator.Dispose();
                        enumerator = null;
                    }
                    if (enumerator == null)
                        yield break;

                    cache.Add(enumerator.Current);
                }

                yield return cache[i];
                i++;
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Dispose()
        {
            if (enumerator != null)
            {
                enumerator.Dispose();
                enumerator = null;
            }
        }
    }

    namespace Winforms
    {
        public static class WinformsExtensionMethods
        {
            /// <summary>
            /// Makes the ToolTip text no longer associated with the specified control.
            /// </summary>
            /// <param name="control">The <see cref="T:System.Windows.Forms.Control"/> to deassociate the ToolTip text with. </param>
            public static void RemoveToolTip(this ToolTip toolTip, Control control)
            {
                toolTip.SetToolTip(control, null);
            }

            public static void SetUseCompatibleTextRendering(this Control control, bool value)
            {
                //var types = typeof (Label).Assembly.GetTypes().Where(t => t.GetProperties().Any(p => p.Name == "UseCompatibleTextRendering")).ToArray();
                //var typeNames = types.Select(x => x.FullName).ToArray();
                (control as ButtonBase).AsOption().IfSomeDo(x => x.UseCompatibleTextRendering = value);
                (control as CheckedListBox).AsOption().IfSomeDo(x => x.UseCompatibleTextRendering = value);
                (control as GroupBox).AsOption().IfSomeDo(x => x.UseCompatibleTextRendering = value);
                (control as Label).AsOption().IfSomeDo(x => x.UseCompatibleTextRendering = value);
                (control as PropertyGrid).AsOption().IfSomeDo(x => x.UseCompatibleTextRendering = value);

                foreach (var child in control.Controls.AsEnumerable())
                    SetUseCompatibleTextRendering(child, value);
            }

            public static void SetTextRightOfDelimiter(this Control control, string rightPartText, string delimiter = ": ", int skipDelimiters = 0)
            {
                var leftPart = control.GetTextLeftOfDelimiter(delimiter, skipDelimiters);
                control.Text = leftPart + delimiter + rightPartText;
            }

            public static string GetTextRightOfDelimiter(this Control control, string delimiter = ":", int skipDelimiters = 0)
            {
                return SplitControlTextWithDelimiter(control, delimiter, skipDelimiters).Item2;
            }

            public static string GetTextLeftOfDelimiter(this Control control, string delimiter = ":", int skipDelimiters = 0)
            {
                return SplitControlTextWithDelimiter(control, delimiter, skipDelimiters).Item1;
            }

            private static Tuple<string, string> SplitControlTextWithDelimiter(Control control, string delimiter, int skipDelimiters)
            {
                var split = control.Text.Split(delimiter);
                int itemsInLeftPart = 1 + skipDelimiters;
                var leftParts = split.Take(itemsInLeftPart);
                var rightParts = split.Skip(itemsInLeftPart);
                return Tuple.Create(string.Join(delimiter, leftParts), string.Join(delimiter, rightParts));
            }
        }
    }

    public static class TuplesExtensions
    {
        public static void PassTo<T1>(this Tuple<T1> args, Action<T1> action)
        {
            action(args.Item1);
        }

        public static void PassTo<T1, T2>(this Tuple<T1, T2> args, Action<T1, T2> action)
        {
            action(args.Item1, args.Item2);
        }

        public static void PassTo<T1, T2, T3>(this Tuple<T1, T2, T3> args, Action<T1, T2, T3> action)
        {
            action(args.Item1, args.Item2, args.Item3);
        }

        public static void PassTo<T1, T2, T3, T4>(this Tuple<T1, T2, T3, T4> args, Action<T1, T2, T3, T4> action)
        {
            action(args.Item1, args.Item2, args.Item3, args.Item4);
        }

        public static void PassTo<T1, T2, T3, T4, T5>(this Tuple<T1, T2, T3, T4, T5> args, Action<T1, T2, T3, T4, T5> action)
        {
            action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5);
        }

        public static void PassTo<T1, T2, T3, T4, T5, T6>(this Tuple<T1, T2, T3, T4, T5, T6> args, Action<T1, T2, T3, T4, T5, T6> action)
        {
            action(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6);
        }


        public static TResult PassTo<T1, TResult>(this Tuple<T1> args, Func<T1, TResult> func)
        {
            return func(args.Item1);
        }

        public static TResult PassTo<T1, T2, TResult>(this Tuple<T1, T2> args, Func<T1, T2, TResult> func)
        {
            return func(args.Item1, args.Item2);
        }

        public static TResult PassTo<T1, T2, T3, TResult>(this Tuple<T1, T2, T3> args, Func<T1, T2, T3, TResult> func)
        {
            return func(args.Item1, args.Item2, args.Item3);
        }

        public static TResult PassTo<T1, T2, T3, T4, TResult>(this Tuple<T1, T2, T3, T4> args, Func<T1, T2, T3, T4, TResult> func)
        {
            return func(args.Item1, args.Item2, args.Item3, args.Item4);
        }

        public static TResult PassTo<T1, T2, T3, T4, T5, TResult>(this Tuple<T1, T2, T3, T4, T5> args, Func<T1, T2, T3, T4, T5, TResult> func)
        {
            return func(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5);
        }

        public static TResult PassTo<T1, T2, T3, T4, T5, T6, TResult>(this Tuple<T1, T2, T3, T4, T5, T6> args, Func<T1, T2, T3, T4, T5, T6, TResult> func)
        {
            return func(args.Item1, args.Item2, args.Item3, args.Item4, args.Item5, args.Item6);
        }
    }
}
