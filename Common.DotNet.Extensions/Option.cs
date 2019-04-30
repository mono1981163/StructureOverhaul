using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Microsoft.FSharp.Core;

namespace Common.DotNet.Extensions
{
    public interface IOption
    {
        bool IsSome { get; }
        bool IsNone { get; }

        [Pure]
        object Get();
    }

    public interface IOption<out T> : IOption
    {
        //bool IsSome { get; }
        //bool IsNone { get; }

        [Pure]
        new T Get();

        string ToString();

        [Pure]
        IEnumerable<T> AsEnumerable();

        [Pure]
        T[] ToArray();

        void IfSomeDo(Action<T> someAction, Action noneAction = null);

        [Pure]
        Option<TResult> Transform<TResult>(Func<T, TResult> func);

        [Pure]
        Option<TResult> Bind<TResult>(Func<T, Option<TResult>> func);
    }

    /// <summary>
    /// Container with "Some" value or "None" (=missing value). Example: lastLength = strings.OptionLast().Transform(x => x.Length).Else(-1);
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public struct Option<T> : IEquatable<Option<T>>, IComparable<Option<T>>, IComparable, IOption<T>
    {
        private readonly bool _isSome;
        private readonly T _value;

        [System.Diagnostics.DebuggerNonUserCode]
        public Option(T value)
        {
            if(ReferenceEquals(null,value))
                throw new ArgumentNullException("value");
            _value = value;
            _isSome = true;
        }

        public bool IsSome
        {
            get { return _isSome; }
        }

        public bool IsNone
        {
            get { return !_isSome; }
        }

        object IOption.Get()
        {
            return Get();
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public T Get()
        {
            if(IsNone)
                throw new Exception("Attemted to get value from None.");
            return _value;
        }

        public override string ToString()
        {
            return this.Transform(x => "Some(" + x + ")")
                       .Else("None");
        }

        [Pure]
        public IEnumerable<T> AsEnumerable()
        {
            return ToArray();
        }

        [Pure]
        public T[] ToArray()
        {
            return _isSome ? new[]{_value} : new T[0];
        }

        [Pure]
        public T Else(T noneCase)
        {
            return _isSome ? _value : noneCase;
        }

        [Pure]
        public Option<T> ElseChain(Option<T> noneCase)
        {
            return _isSome ? this : noneCase;
        }

        [Pure]
        public T Else(Func<T> noneCase)
        {
            return _isSome ? _value : noneCase();
        }

        [Pure]
        public Option<T> ElseChain(Func<Option<T>> noneCase)
        {
            return _isSome ? this : noneCase();
        }

        public void IfSomeDo(Action<T> someAction, Action noneAction = null)
        {
            if (someAction == null) throw new ArgumentNullException("someAction");
            noneAction = noneAction ?? (() => { });

            if (IsSome)
                someAction(this._value);
            else
                noneAction();
        }

        [Pure]
        public Option<TResult> Transform<TResult>(Func<T, TResult> func)
        {
            if (func == null) throw new ArgumentNullException("func");
            if (_isSome)
                return Option.GetSome(func(_value));
            else
                return Option.GetNone<TResult>();
        }

        [Pure]
        public Option<TResult> Bind<TResult>(Func<T, Option<TResult>> func)
        {
            if (func == null) throw new ArgumentNullException("func");
            if(IsNone)
                return Option.GetNone<TResult>();
            return func(_value);
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public static Option<T> Return(T value)
        {
            return new Option<T>(value);
        }

        [Pure]
        public static Option<T> None
        {
            get { return default(Option<T>); }
        }

        // also allows conversion from null
        public static implicit operator Option<T>(Option.UnconvertedNoneOption o)
        {
            return Option.GetNone<T>();
        }

        public static bool operator ==(Option<T> a, Option<T> b)
        {
            if (a.IsNone && b.IsNone)
                return true;

            if (a.IsNone || b.IsNone)
                return false;

            return EqualityComparer<T>.Default.Equals(a._value, b._value);
        }

        public static bool operator !=(Option<T> v1, Option<T> v2)
        {
            return !(v1 == v2);
        }

        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(_value) ^ (_isSome ? 3 : 1);
        }

        // override object.Equals
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            if (!(obj is Option<T>))
                return false;

            return this.Equals((Option<T>)obj);
        }

        public bool Equals(Option<T> other)
        {
            return this == other;
        }

        public int CompareTo(object obj)
        {
            if(obj == null)
                throw new ArgumentException("Can't compare with null");
            var other = obj as Option<T>?;
            if(other == null)
                throw new ArgumentException("Can't compare two objects of different types");
            return CompareTo(other.Value);
        }

        public int CompareTo(Option<T> other)
        {
            if (this == other)
                return 0;

            if (this._isSome && other._isSome)
                return Comparer<T>.Default.Compare(this._value, other._value);
            else
                return other._isSome ? -1 : 1; // Option.None < Option.GetSome(-1)
        }
    }

    public class DisposableOptionWrapper<T> : IDisposable where T : IDisposable
    {
        [Pure]
        public Option<T> Unwrap()
        {
            return wrappedValue;
        }

        private Option<T> wrappedValue;

        public DisposableOptionWrapper(Option<T> wrappedValue)
        {
            this.wrappedValue = wrappedValue;
        }

        public void Dispose()
        {
            wrappedValue.IfSomeDo(x => x.Dispose());
        }
    }

    public static class Option
    {
        [Pure]
        public static Option<T> FromNull<T>(T? value) where T : struct
        {
            if (null == value)
                return GetNone<T>();

            return GetSome(value.Value);
        }

        [Pure]
        public static Option<T> FromNull<T>(T value) where T : class
        {
            if (ReferenceEquals(null, value))
                return GetNone<T>();

            return GetSome(value);
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public static Option<T> GetSome<T>(T value)
        {
            return Option<T>.Return(value);
        }

        [Pure]
        public static Option<T> GetNone<T>()
        {
            return new Option<T>();
        }

        [Pure]
        public static UnconvertedNoneOption None
        {
            get { return UnconvertedNoneOption.NoneOption; }
        }

        public class UnconvertedNoneOption
        {
            public static UnconvertedNoneOption NoneOption = new UnconvertedNoneOption();

            private UnconvertedNoneOption() { } 
        }

        [Pure]
        public static T Else<T>(this IOption<T> o, T noneCase)
        {
            if (o.IsNone)
                return noneCase;
            return o.Get();
        }

        [Pure]
        public static Option<T> ElseChain<T>(this IOption<T> o, IOption<T> noneCase)
        {
            return o.AsOption().ElseChain(noneCase.AsOption());
        }

        [Pure]
        public static T Else<T>(this IOption<T> o, Func<T> noneCase)
        {
            if (o.IsNone)
                return noneCase();
            return o.Get();
        }

        [Pure]
        public static Option<T> ElseChain<T>(this IOption<T> o, Func<IOption<T>> noneCase)
        {
            return o.AsOption().ElseChain(() => noneCase().AsOption());
        }


#region OptionError
        public delegate UnconvertedNoneOption ErrorHelper(string msg);

        // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        [System.Diagnostics.DebuggerNonUserCode]
        public static UnconvertedNoneOption ThrowErrorMessageException(string msg)
        {
            throw new ErrorMessageException(msg);
        }

        // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        [System.Diagnostics.DebuggerNonUserCode]
        public static UnconvertedNoneOption ThrowException(string msg)
        {
            throw new Exception(msg);
        }

        // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        [System.Diagnostics.DebuggerNonUserCode]
        public static ErrorHelper ThrowException(Func<string, Exception> eFunc)
        {
            return new ThrowExceptionHelper {eFunc = eFunc}.ThrowException;
        }

        private class ThrowExceptionHelper
        {
            public Func<string, Exception> eFunc;

            // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
            [System.Diagnostics.DebuggerNonUserCode]
            public UnconvertedNoneOption ThrowException(string msg)
            {
                throw eFunc(msg);
            }
        }

        public static UnconvertedNoneOption ReturnNone(string msg)
        {
            return None;
        }
#endregion
    }

    public static class OptionExtensions
    {
        [Pure]
        public static Option<TResult> Flatten<TResult>(this Option<Option<TResult>> source)
        {
            if (source.IsNone)
                return Option.None;
            return source.Get();
        }

        /// <summary>
        /// Checks if value == Option.GetSome(true). None returns false.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsTrue(this Option<bool> value)
        {
            return value == Option.GetSome(true);
        }

        /// <summary>
        /// Checks if value == Option.GetSome(false). None returns false.
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [Pure]
        public static bool IsFalse(this Option<bool> value)
        {
            return value == Option.GetSome(false);
        }

        [Pure]
        public static Option<T> OfType<T>(this IOption value)
        {
            return value.AsOption().Where(x => x is T).Transform(x => (T) x);
        }

        [Pure]
        public static Option<string> WhereNotEmpty(this Option<string> value)
        {
            return value.Where(s => !string.IsNullOrEmpty(s));
        }

        [Pure]
        public static Option<string> WhereNotEmptyOrWhiteSpace(this Option<string> value)
        {
            return value.Where(s => !string.IsNullOrWhiteSpace(s));
        }

        [Pure]
        public static Option<string> WhereNotNullOrEmpty(this Option<string> value)
        {
            return value.Where(s => !string.IsNullOrEmpty(s));
        }

        [Pure]
        public static Option<string> WhereNotNullOrWhiteSpace(this Option<string> value)
        {
            return value.Where(s => !string.IsNullOrWhiteSpace(s));
        }

        [Pure]
        public static Option<T> AsOption<T>(this T? value) where T : struct
        {
            return Option.FromNull(value);
        }

        [Pure]
        public static Option<T> AsOption<T>(this IOption<T> value)
        {
            if (value is Option<T>)
                return (Option<T>)value;
            if (value == null || value.IsNone)
                return Option<T>.None;
            return Option.GetSome(value.Get());
        }

        [Pure]
        public static Option<object> AsOption(this IOption value)
        {
            if (value == null || value.IsNone)
                return Option.None;
            return Option.GetSome(value.Get());
        }

        [Pure]
        public static Option<T> AsOption<T>(this T value)
        {
            if (ReferenceEquals(null, value))
                return Option.GetNone<T>();

            return Option.GetSome(value);
        }

        [Pure]
        public static Option<T> AsOptionOfType<T>(this object value)
        {
            if (value is T) // not null if true
                return Option.GetSome((T)value);
            return Option.None;
        }

        [Pure]
        public static DisposableOptionWrapper<T> Wrap<T>(this Option<T> value) where T : IDisposable
        {
            return new DisposableOptionWrapper<T>(value);
        }

        [Pure]
        public static T ToObject<T>(this Option<T> value) where T : class
        {
            return value.Else((T)null);
        }

        [Pure]
        public static T? ToNullable<T>(this Option<T> value) where T : struct
        {
            return value.Transform(x => (T?)x).Else((T?)null);
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public static T ElseException<T>(this Option<T> value, string errorMessage)
        {
            if (value.IsSome)
                return value.Get();
            throw new Exception(errorMessage);
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public static T ElseException<T, TException>(this Option<T> value, TException errorMessage) where TException : Exception
        {
            if (value.IsSome)
                return value.Get();
            throw errorMessage;
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [Pure]
        public static T ElseErrorMessage<T>(this Option<T> value, string errorMessage)
        {
            if (value.IsSome)
                return value.Get();
            throw new ErrorMessageException(errorMessage);
        }

        [Pure]
        public static IEnumerable<TResult> Choose<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, Option<TResult>> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            return source.Select(selector).WhereIsSome();
        }

        [Pure]
        public static Option<TResult> SelectMany<TSource, TCollection, TResult>(
            this Option<TSource> source,
            Func<TSource, Option<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (collectionSelector == null) throw new ArgumentNullException("collectionSelector");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            return source.Bind(s => collectionSelector(s).Select(c => resultSelector(s,c)));
        }

        [Pure]
        public static Option<TResult> Select<TSource, TResult>(this Option<TSource> source, Func<TSource, TResult> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            return source.Bind(x => Option<TResult>.Return(selector(x)));
        }

        [Pure]
        public static Option<TSource> Where<TSource>(this Option<TSource> source, Func<TSource, bool> predicate)
        {
            if (predicate == null) throw new ArgumentNullException("predicate");
            return SelectMany(source, x => predicate(x) ? Option.GetSome(x) : Option.GetNone<TSource>(), (x, y) => y);
        }

        [Pure]
        public static IEnumerable<T> WhereIsSome<T>(this IEnumerable<Option<T>> source)
        {
            return source.SelectMany(x => x.AsEnumerable());
        }


        [Pure]
        public static IEnumerable<T> Flatten<T>(this IEnumerable<Option<T>> source)
        {
            return source.SelectMany(x => x.AsEnumerable());
        }

        [Pure]
        public static IEnumerable<T> Flatten<T>(this IOption<IEnumerable<T>> source)
        {
            return source.Else(Enumerable.Empty<T>);
        }

        [Pure]
        public static Option<T[]> Sequence<T>(this IEnumerable<Option<T>> source)
        {
            var buf = new List<T>();
            foreach (var x in source)
            {
                if (x.IsNone)
                    return Option.None;
                else
                    buf.Add(x.Get());
            }
            return Option.GetSome(buf.ToArray());
        }

        [Pure]
        public static IEnumerable<Option<TResult>> TransformInner<TSource, TResult>(this IEnumerable<Option<TSource>> source, Func<TSource, TResult> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            return source.Select(x => x.Select(selector));
        }

        [Pure]
        public static IEnumerable<Option<TResult>> BindInner<TSource, TResult>(this IEnumerable<Option<TSource>> source, Func<TSource, Option<TResult>> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            return source.Select(x => x.Bind(selector));
        }

        [Pure]
        public static Option<Tuple<T1>> Sequence<T1>(this Tuple<Option<T1>> source)
        {
            return source.Item1.Transform(Tuple.Create);
        }

        [Pure]
        public static Option<Tuple<T1, T2>> Sequence<T1, T2>(this Tuple<Option<T1>, Option<T2>> source)
        {
            return Tuple.Create(source.Item2).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3>> Sequence<T1, T2, T3>(this Tuple<Option<T1>, Option<T2>, Option<T3>> source)
        {
            return Tuple.Create(source.Item2, source.Item3).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1, xs.Item2)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4>> Sequence<T1, T2, T3, T4>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1, xs.Item2, xs.Item3)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4, T5>> Sequence<T1, T2, T3, T4, T5>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>, Option<T5>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4, source.Item5).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1, xs.Item2, xs.Item3, xs.Item4)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4, T5, T6>> Sequence<T1, T2, T3, T4, T5, T6>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>, Option<T5>, Option<T6>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4, source.Item5, source.Item6).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1, xs.Item2, xs.Item3, xs.Item4, xs.Item5)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4, T5, T6, T7>> Sequence<T1, T2, T3, T4, T5, T6, T7>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>, Option<T5>, Option<T6>, Option<T7>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7).Sequence().Bind(xs => source.Item1.Transform(x => Tuple.Create(x, xs.Item1, xs.Item2, xs.Item3, xs.Item4, xs.Item5, xs.Item6)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>> Sequence<T1, T2, T3, T4, T5, T6, T7, T8>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>, Option<T5>, Option<T6>, Option<T7>, Tuple<Option<T8>>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7, source.Rest.Sequence()).Sequence().Bind(xs => source.Item1.Transform(x => new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8>>(x, xs.Item1, xs.Item2, xs.Item3, xs.Item4, xs.Item5, xs.Item6, xs.Item7)));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>> Sequence<T1, T2, T3, T4, T5, T6, T7, T8, T9>(this Tuple<Option<T1>, Option<T2>, Option<T3>, Option<T4>, Option<T5>, Option<T6>, Option<T7>, Tuple<Option<T8>, Option<T9>>> source)
        {
            return Tuple.Create(source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7, source.Rest.Sequence()).Sequence().Bind(xs => source.Item1.Transform(x => new Tuple<T1, T2, T3, T4, T5, T6, T7, Tuple<T8, T9>>(x, xs.Item1, xs.Item2, xs.Item3, xs.Item4, xs.Item5, xs.Item6, xs.Item7)));
        }


        [Pure]
        public static Option<Tuple<T1, T2>> SequenceByItem1<T1, T2>(this Tuple<Option<T1>, T2> source)
        {
            return source.Item1.Transform(x => Tuple.Create(x, source.Item2));
        }

        [Pure]
        public static Option<Tuple<T1, T2>> SequenceByItem2<T1, T2>(this Tuple<T1, Option<T2>> source)
        {
            return source.Item2.Transform(x => Tuple.Create(source.Item1, x));
        }


        [Pure]
        public static Option<Tuple<T1, T2, T3>> SequenceByItem1<T1, T2, T3>(this Tuple<Option<T1>, T2, T3> source)
        {
            return source.Item1.Transform(x => Tuple.Create(x, source.Item2, source.Item3));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3>> SequenceByItem2<T1, T2, T3>(this Tuple<T1, Option<T2>, T3> source)
        {
            return source.Item2.Transform(x => Tuple.Create(source.Item1, x, source.Item3));
        }

        [Pure]
        public static Option<Tuple<T1, T2, T3>> SequenceByItem3<T1, T2, T3>(this Tuple<T1, T2, Option<T3>> source)
        {
            return source.Item3.Transform(x => Tuple.Create(source.Item1, source.Item2, x));
        }


        public static void IfSomePassTo<T1>(this Option<Tuple<T1>> args, Action<T1> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }

        public static void IfSomePassTo<T1, T2>(this Option<Tuple<T1, T2>> args, Action<T1, T2> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }

        public static void IfSomePassTo<T1, T2, T3>(this Option<Tuple<T1, T2, T3>> args, Action<T1, T2, T3> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }

        public static void IfSomePassTo<T1, T2, T3, T4>(this Option<Tuple<T1, T2, T3, T4>> args, Action<T1, T2, T3, T4> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }

        public static void IfSomePassTo<T1, T2, T3, T4, T5>(this Option<Tuple<T1, T2, T3, T4, T5>> args, Action<T1, T2, T3, T4, T5> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }

        public static void IfSomePassTo<T1, T2, T3, T4, T5, T6>(this Option<Tuple<T1, T2, T3, T4, T5, T6>> args, Action<T1, T2, T3, T4, T5, T6> action)
        {
            args.IfSomeDo(a => a.PassTo(action));
        }
    }

    public static class TupleExtensions
    {
        /// <summary>
        /// Swaps (1, "a") to ("a", 1)
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        [Pure]
        public static Tuple<T2, T1> Swap<T1, T2>(this Tuple<T1, T2> source)
        {
            return Tuple.Create(source.Item2, source.Item1);
        }

        [Pure]
        public static IEnumerable<T1> SelectItem1<T1, T2>(this IEnumerable<Tuple<T1, T2>> source)
        {
            return source.Select(x => x.Item1);
        }

        [Pure]
        public static IEnumerable<T2> SelectItem2<T1, T2>(this IEnumerable<Tuple<T1, T2>> source)
        {
            return source.Select(x => x.Item2);
        }

        /// <summary>
        /// [1,4].SelectToItem1(x => x+5) -> [(6,1), (9,4)]
        /// </summary>
        [Pure]
        public static IEnumerable<Tuple<TResult, T>> SelectToItem1<T, TResult>(this IEnumerable<T> source, Func<T, TResult> func)
        {
            return source.Select(x => Tuple.Create(func(x), x));
        }

        /// <summary>
        /// [1,4].SelectToItem2(x => x+5) -> [(1,6), (4,9)]
        /// </summary>
        [Pure]
        public static IEnumerable<Tuple<T, TResult>> SelectToItem2<T, TResult>(this IEnumerable<T> source, Func<T, TResult> func)
        {
            return source.Select(x => Tuple.Create(x, func(x)));
        }

        /// <summary>
        /// ["1", "a", "2"].ChooseToItem1(OptionParseInt) -> [(1,"1"), (2,"2")]
        /// </summary>
        [Pure]
        public static IEnumerable<Tuple<TResult, T>> ChooseToItem1<T, TResult>(this IEnumerable<T> source, Func<T, Option<TResult>> func)
        {
            return source.Choose(x => Tuple.Create(func(x), x).SequenceByItem1());
        }

        /// <summary>
        /// ["1", "a", "2"].ChooseToItem2(OptionParseInt) -> [("1",1), ("2",2)]
        /// </summary>
        [Pure]
        public static IEnumerable<Tuple<T, TResult>> ChooseToItem2<T, TResult>(this IEnumerable<T> source, Func<T, Option<TResult>> func)
        {
            return source.Choose(x => Tuple.Create(x, func(x)).SequenceByItem2());
        }

        #region Tuple Zip
        [Pure]
        public static Tuple<TResult, TResult> ZipWith<T1, T2, TResult>(this Tuple<T1, T1> source, Tuple<T2, T2> source2, Func<T1, T2, TResult> func)
        {
            return Tuple.Create(func(source.Item1, source2.Item1), func(source.Item2, source2.Item2));
        }
        #endregion

        #region Tuple AsEnumerable

        [Pure]
        public static T[] AsEnumerable<T>(this Tuple<T, T> source)
        {
            return new[] {source.Item1, source.Item2};
        }

        [Pure]
        public static T[] AsEnumerable<T>(this Tuple<T, T, T> source)
        {
            return new[] {source.Item1, source.Item2, source.Item3};
        }

        [Pure]
        public static T[] AsEnumerable<T>(this Tuple<T, T, T, T> source)
        {
            return new[] {source.Item1, source.Item2, source.Item3, source.Item4};
        }

        [Pure]
        public static T[] AsEnumerable<T>(this Tuple<T, T, T, T, T> source)
        {
            return new[] {source.Item1, source.Item2, source.Item3, source.Item4, source.Item5};
        }


        [Pure]
        public static Option<Tuple<T, T>> OptionTakeExactly2<T>(this IEnumerable<T> source)
        {
            return TakeExactly2_(source, Option.ReturnNone);
        }

        [Pure]
        public static Option<Tuple<T, T, T>> OptionTakeExactly3<T>(this IEnumerable<T> source)
        {
            return TakeExactly3_(source, Option.ReturnNone);
        }

        [Pure]
        public static Option<Tuple<T, T, T, T>> OptionTakeExactly4<T>(this IEnumerable<T> source)
        {
            return TakeExactly4_(source, Option.ReturnNone);
        }

        [Pure]
        public static Option<Tuple<T, T, T, T, T>> OptionTakeExactly5<T>(this IEnumerable<T> source)
        {
            return TakeExactly5_(source, Option.ReturnNone);
        }


        [Pure]
        public static Tuple<T, T> TakeExactly2<T>(this IEnumerable<T> source)
        {
            return TakeExactly2_(source, Option.ThrowException).Get();
        }

        [Pure]
        public static Tuple<T, T, T> TakeExactly3<T>(this IEnumerable<T> source)
        {
            return TakeExactly3_(source, Option.ThrowException).Get();
        }

        [Pure]
        public static Tuple<T, T, T, T> TakeExactly4<T>(this IEnumerable<T> source)
        {
            return TakeExactly4_(source, Option.ThrowException).Get();
        }

        [Pure]
        public static Tuple<T, T, T, T, T> TakeExactly5<T>(this IEnumerable<T> source)
        {
            return TakeExactly5_(source, Option.ThrowException).Get();
        }


        [Pure]
        private static Option<Tuple<T, T>> TakeExactly2_<T>(this IEnumerable<T> source, Option.ErrorHelper returnNoneOrThrow)
        {
            return TakeExactly(source, 2, returnNoneOrThrow).Transform(xs => Tuple.Create(xs[0], xs[1]));
        }

        [Pure]
        private static Option<Tuple<T, T, T>> TakeExactly3_<T>(this IEnumerable<T> source, Option.ErrorHelper returnNoneOrThrow)
        {
            return TakeExactly(source, 3, returnNoneOrThrow).Transform(xs => Tuple.Create(xs[0], xs[1], xs[2]));
        }

        [Pure]
        private static Option<Tuple<T, T, T, T>> TakeExactly4_<T>(this IEnumerable<T> source, Option.ErrorHelper returnNoneOrThrow)
        {
            return TakeExactly(source, 4, returnNoneOrThrow).Transform(xs => Tuple.Create(xs[0], xs[1], xs[2], xs[3]));
        }

        [Pure]
        private static Option<Tuple<T, T, T, T, T>> TakeExactly5_<T>(this IEnumerable<T> source, Option.ErrorHelper returnNoneOrThrow)
        {
            return TakeExactly(source, 5, returnNoneOrThrow).Transform(xs => Tuple.Create(xs[0], xs[1], xs[2], xs[3], xs[4]));
        }

        private static Option<T[]> TakeExactly<T>(IEnumerable<T> source, int count, Option.ErrorHelper returnNoneOrThrow)
        {
            var xs = source.Take(count + 1).ToArray();
            if (xs.Length != count)
                return returnNoneOrThrow("Expected " + count + " items, but got " + (xs.Length > count ? " more." : xs.Length + " items."));
            return Option.GetSome(xs);
        }

        #endregion

        #region Tuple Transform

        [Pure]
        public static Tuple<TResult, TResult> TransformItems<TSource, TResult>(this Tuple<TSource, TSource> source, Func<TSource, TResult> selector)
        {
            return Tuple.Create(selector(source.Item1), selector(source.Item2));
        }

        [Pure]
        public static Tuple<TResult, TResult, TResult> TransformItems<TSource, TResult>(this Tuple<TSource, TSource, TSource> source, Func<TSource, TResult> selector)
        {
            return Tuple.Create(selector(source.Item1), selector(source.Item2), selector(source.Item3));
        }

        [Pure]
        public static Tuple<TResult, TResult, TResult, TResult> TransformItems<TSource, TResult>(this Tuple<TSource, TSource, TSource, TSource> source, Func<TSource, TResult> selector)
        {
            return Tuple.Create(selector(source.Item1), selector(source.Item2), selector(source.Item3), selector(source.Item4));
        }

        [Pure]
        public static Tuple<TResult, TResult, TResult, TResult, TResult> TransformItems<TSource, TResult>(this Tuple<TSource, TSource, TSource, TSource, TSource> source, Func<TSource, TResult> selector)
        {
            return Tuple.Create(selector(source.Item1), selector(source.Item2), selector(source.Item3), selector(source.Item4), selector(source.Item5));
        }

        [Pure]
        public static Tuple<TResult, T2> TransformItem1<TSource, TResult, T2>(this Tuple<TSource, T2> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(func(source.Item1), source.Item2);
        }

        [Pure]
        public static Tuple<T1, TResult> TransformItem2<TSource, TResult, T1>(this Tuple<T1, TSource> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, func(source.Item2));
        }


        [Pure]
        public static Tuple<TResult, T2, T3> TransformItem1<TSource, TResult, T2, T3>(this Tuple<TSource, T2, T3> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(func(source.Item1), source.Item2, source.Item3);
        }

        [Pure]
        public static Tuple<T1, TResult, T3> TransformItem2<TSource, TResult, T1, T3>(this Tuple<T1, TSource, T3> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, func(source.Item2), source.Item3);
        }

        [Pure]
        public static Tuple<T1, T2, TResult> TransformItem3<TSource, TResult, T1, T2>(this Tuple<T1, T2, TSource> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, source.Item2, func(source.Item3));
        }



        [Pure]
        public static Tuple<TResult, T2, T3, T4> TransformItem1<TSource, TResult, T2, T3, T4>(this Tuple<TSource, T2, T3, T4> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(func(source.Item1), source.Item2, source.Item3, source.Item4);
        }

        [Pure]
        public static Tuple<T1, TResult, T3, T4> TransformItem2<TSource, TResult, T1, T3, T4>(this Tuple<T1, TSource, T3, T4> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, func(source.Item2), source.Item3, source.Item4);
        }

        [Pure]
        public static Tuple<T1, T2, TResult, T4> TransformItem3<TSource, TResult, T1, T2, T4>(this Tuple<T1, T2, TSource, T4> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, source.Item2, func(source.Item3), source.Item4);
        }

        [Pure]
        public static Tuple<T1, T2, T3, TResult> TransformItem4<TSource, TResult, T1, T2, T3>(this Tuple<T1, T2, T3, TSource> source, Func<TSource, TResult> func)
        {
            return Tuple.Create(source.Item1, source.Item2, source.Item3, func(source.Item4));
        }

        #endregion
    }

    public static class ChoiceExtensions
    {
        #region FSharpChoice<T1, T2>

        // int value = 10;
        // Choice.Choose1.If(() => height > 0).Then(() => value)
        //       .Choose2.Otherwise(() => "neg")

        [Pure]
        public static FSharpChoice<TR1, TR2> Transform<T1, T2, TR1, TR2>(this FSharpChoice<T1, T2> choice, Func<T1, TR1> func1, Func<T2, TR2> func2)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2>.Tags.Choice1Of2:
                    return FSharpChoice<TR1, TR2>.NewChoice1Of2(func1((choice.CastTo<FSharpChoice<T1, T2>.Choice1Of2>()).Item));
                case FSharpChoice<T1, T2>.Tags.Choice2Of2:
                    return FSharpChoice<TR1, TR2>.NewChoice2Of2(func2((choice.CastTo<FSharpChoice<T1, T2>.Choice2Of2>()).Item));
            }
            throw new InvalidOperationException();
        }

        [Pure]
        public static TResult Match<T1, T2, TResult>(this FSharpChoice<T1, T2> choice, Func<T1,TResult> func1, Func<T2,TResult> func2)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2>.Tags.Choice1Of2:
                    return func1((choice.CastTo<FSharpChoice<T1, T2>.Choice1Of2>()).Item);
                case FSharpChoice<T1, T2>.Tags.Choice2Of2:
                    return func2((choice.CastTo<FSharpChoice<T1, T2>.Choice2Of2>()).Item);
            }
            throw new InvalidOperationException();
        }

        public static void Match<T1, T2>(this FSharpChoice<T1, T2> choice, Action<T1> action1, Action<T2> action2)
        {
            choice.Match(x => { action1(x); return 0; }, x => { action2(x); return 0; });
        }

        [Pure]
        public static Option<T1> GetChoice1Of2<T1, T2>(this FSharpChoice<T1, T2> choice)
        {
            return choice.Match(Option.GetSome, x => Option.None);
        }

        [Pure]
        public static Option<T2> GetChoice2Of2<T1, T2>(this FSharpChoice<T1, T2> choice)
        {
            return choice.Match(x => Option.None, Option.GetSome);
        }

        [Pure]
        public static IEnumerable<T1> WhereIsChoice1Of2<T1, T2>(this IEnumerable<FSharpChoice<T1, T2>> source)
        {
            return source.Select(c => c.GetChoice1Of2()).Flatten();
        }

        [Pure]
        public static IEnumerable<T2> WhereIsChoice2Of2<T1, T2>(this IEnumerable<FSharpChoice<T1, T2>> source)
        {
            return source.Select(c => c.GetChoice2Of2()).Flatten();
        }
        #endregion

        #region FSharpChoice<T1, T2, T3>
        [Pure]
        public static FSharpChoice<TR1, TR2, TR3> Transform<T1, T2, T3, TR1, TR2, TR3>(this FSharpChoice<T1, T2, T3> choice, Func<T1, TR1> func1, Func<T2, TR2> func2, Func<T3, TR3> func3)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2, T3>.Tags.Choice1Of3:
                    return FSharpChoice<TR1, TR2, TR3>.NewChoice1Of3(func1((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice1Of3>()).Item));
                case FSharpChoice<T1, T2, T3>.Tags.Choice2Of3:
                    return FSharpChoice<TR1, TR2, TR3>.NewChoice2Of3(func2((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice2Of3>()).Item));
                case FSharpChoice<T1, T2, T3>.Tags.Choice3Of3:
                    return FSharpChoice<TR1, TR2, TR3>.NewChoice3Of3(func3((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice3Of3>()).Item));
            }
            throw new InvalidOperationException();
        }

        [Pure]
        public static TResult Match<T1, T2, T3, TResult>(this FSharpChoice<T1, T2, T3> choice, Func<T1, TResult> func1, Func<T2, TResult> func2, Func<T3, TResult> func3)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2, T3>.Tags.Choice1Of3:
                    return func1((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice1Of3>()).Item);
                case FSharpChoice<T1, T2, T3>.Tags.Choice2Of3:
                    return func2((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice2Of3>()).Item);
                case FSharpChoice<T1, T2, T3>.Tags.Choice3Of3:
                    return func3((choice.CastTo<FSharpChoice<T1, T2, T3>.Choice3Of3>()).Item);
            }
            throw new InvalidOperationException();
        }

        public static void Match<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice, Action<T1> action1, Action<T2> action2, Action<T3> action3)
        {
            choice.Match(x => { action1(x); return 0; }, x => { action2(x); return 0; }, x => { action3(x); return 0; });
        }

        [Pure]
        public static Option<T1> GetChoice1Of3<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice)
        {
            return choice.Match(Option.GetSome, x => Option.None, x => Option.None);
        }

        [Pure]
        public static Option<T2> GetChoice2Of3<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice)
        {
            return choice.Match(x => Option.None, Option.GetSome, x => Option.None);
        }

        [Pure]
        public static Option<T3> GetChoice3Of3<T1, T2, T3>(this FSharpChoice<T1, T2, T3> choice)
        {
            return choice.Match(x => Option.None, x => Option.None, Option.GetSome);
        }

        [Pure]
        public static IEnumerable<T1> WhereIsChoice1Of3<T1, T2, T3>(this IEnumerable<FSharpChoice<T1, T2, T3>> source)
        {
            return source.Select(c => c.GetChoice1Of3()).Flatten();
        }

        [Pure]
        public static IEnumerable<T2> WhereIsChoice2Of3<T1, T2, T3>(this IEnumerable<FSharpChoice<T1, T2, T3>> source)
        {
            return source.Select(c => c.GetChoice2Of3()).Flatten();
        }

        [Pure]
        public static IEnumerable<T3> WhereIsChoice3Of3<T1, T2, T3>(this IEnumerable<FSharpChoice<T1, T2, T3>> source)
        {
            return source.Select(c => c.GetChoice3Of3()).Flatten();
        }
        #endregion

        #region FSharpChoice<T1, T2, T3, T4>
        [Pure]
        public static FSharpChoice<TR1, TR2, TR3, TR4> Transform<T1, T2, T3, T4, TR1, TR2, TR3, TR4>(this FSharpChoice<T1, T2, T3, T4> choice, Func<T1, TR1> func1, Func<T2, TR2> func2, Func<T3, TR3> func3, Func<T4, TR4> func4)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice1Of4:
                    return FSharpChoice<TR1, TR2, TR3, TR4>.NewChoice1Of4(func1((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice1Of4>()).Item));
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice2Of4:
                    return FSharpChoice<TR1, TR2, TR3, TR4>.NewChoice2Of4(func2((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice2Of4>()).Item));
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice3Of4:
                    return FSharpChoice<TR1, TR2, TR3, TR4>.NewChoice3Of4(func3((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice3Of4>()).Item));
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice4Of4:
                    return FSharpChoice<TR1, TR2, TR3, TR4>.NewChoice4Of4(func4((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice4Of4>()).Item));
            }
            throw new InvalidOperationException();
        }

        [Pure]
        public static TResult Match<T1, T2, T3, T4, TResult>(this FSharpChoice<T1, T2, T3, T4> choice, Func<T1, TResult> func1, Func<T2, TResult> func2, Func<T3, TResult> func3, Func<T4, TResult> func4)
        {
            switch (choice.Tag)
            {
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice1Of4:
                    return func1((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice1Of4>()).Item);
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice2Of4:
                    return func2((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice2Of4>()).Item);
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice3Of4:
                    return func3((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice3Of4>()).Item);
                case FSharpChoice<T1, T2, T3, T4>.Tags.Choice4Of4:
                    return func4((choice.CastTo<FSharpChoice<T1, T2, T3, T4>.Choice4Of4>()).Item);
            }
            throw new InvalidOperationException();
        }

        public static void Match<T1, T2, T3, T4>(this FSharpChoice<T1, T2, T3, T4> choice, Action<T1> action1, Action<T2> action2, Action<T3> action3, Action<T4> action4)
        {
            choice.Match(x => { action1(x); return 0; }, x => { action2(x); return 0; }, x => { action3(x); return 0; }, x => { action4(x); return 0; });
        }
        #endregion

    }
}
