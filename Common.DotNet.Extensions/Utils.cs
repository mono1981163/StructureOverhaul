using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Common.DotNet.Extensions
{
    public static class ProgressedTask
    {
        public static ProgressedTask<T> Create<T>(int totalTicks, Func<Action, T> task)
        {
            return new ProgressedTask<T>(totalTicks, task);
        }

        public static ProgressedTask<TArg, TResult> Create<TArg, TResult>(int totalTicks, Func<Action, TArg, TResult> task)
        {
            return new ProgressedTask<TArg, TResult>(totalTicks, task);
        }
    }

    public class ProgressedTask<T>
    {
        public static ProgressedTask<T, TResult> Create<TResult>(int totalTicks, Func<Action, T, TResult> task)
        {
            return new ProgressedTask<T, TResult>(totalTicks, task);
        }

        private Func<Action, T> _task;

        public ProgressedTask(int totalTicks, Func<Action, T> task)
        {
            TotalTicks = totalTicks;
            _task = task;
        }

        public int TotalTicks { get; private set; }

        public T Run(Action ticker)
        {
            return _task(ticker);
        }

        public static ProgressedTask<T> Empty { get { return new ProgressedTask<T>(0, tick => default(T)); } }
    }

    public class ProgressedTask<TArg, TResult>
    {
        private Func<Action, TArg, TResult> _task;

        public ProgressedTask(int totalTicks, Func<Action, TArg, TResult> task)
        {
            TotalTicks = totalTicks;
            _task = task;
        }

        public int TotalTicks { get; private set; }

        public TResult Run(Action ticker, TArg arg)
        {
            return _task(ticker, arg);
        }

        public static ProgressedTask<TArg, TResult> Empty { get { return new ProgressedTask<TArg, TResult>(0, (tick, arg) => default(TResult)); } }
    }

    public static class EnumerableUtils
    {
        [Pure]
        public static IEnumerable<T> Concat<T>(params IEnumerable<T>[] enumerables)
        {
            return enumerables.Aggregate(Enumerable.Empty<T>(), (x, y) => x.Concat(y));
        }
    }
    public class Utils
    {
        public static readonly Encoding Latin1Encoding = Encoding.GetEncoding("Windows-1252");
        //public static readonly Encoding Latin1Encoding = Encoding.GetEncoding("iso-8859-1");

        public static void DisposeUnlessNull<T>(ref T t) where T : class, IDisposable
        {
            DisposeUnlessNull(ref t, x => x.Dispose());
        }

        public static void DisposeUnlessNull<T>(ref T t, Action<T> disposeAction) where T : class
        {
            if (t != null)
            {
                disposeAction(t);
                t = null;
            }
        }

        public static void MoveFile(string source, string targetPath)
        {
            var creationDateUtc = System.IO.File.GetCreationTimeUtc(source);
            DeleteFile(targetPath);
            System.IO.File.Move(source, targetPath); // sequence atomic on NTFS, apparently
            var targetInfo = new System.IO.FileInfo(targetPath);

            var attributes = targetInfo.Attributes;
            targetInfo.Attributes = System.IO.FileAttributes.Normal; // disable read only temporarily
            targetInfo.CreationTimeUtc = creationDateUtc; // preserve creation date. (note file system "tunneling")
            targetInfo.Attributes = attributes;
        }

        public static void DeleteFile(string targetPath)
        {
            var fileInfo = new System.IO.FileInfo(targetPath);
            if (!fileInfo.Exists)
                return;

            if (fileInfo.Attributes != System.IO.FileAttributes.Normal)
                fileInfo.Attributes = System.IO.FileAttributes.Normal;

            fileInfo.Delete();
        }

        public static Dictionary<TKey, TValue> GetIdentityDictionary<TKey, TValue>() where TKey : class
        {
            return new Dictionary<TKey, TValue>(new IdentityEqualityComparer<TKey>());
        }

        public static StaticRandomImp StaticRandom = new StaticRandomImp();

        public static IEnumerable<T> NullToEnumerable<T>(T item) where T : class
        {
            if (item != null)
                return new T[] { item };
            else
                return new T[0];
        }

        public static T ParseEnum<T>(string s) where T : struct, IConvertible
        {
            Type type = typeof(T);
            if (!type.IsEnum)
                throw new ArgumentException(type + " is not an enum type");
            var e = (T)Enum.Parse(type, s);
            if (!Enum.IsDefined(type, e))
                throw new ArgumentException(s + " is not a defined enum value");

            return e;
        }

        public static Option<T> ParseEnumOption<T>(string s) where T : struct, IConvertible
        {
            Type type = typeof(T);
            if (!type.IsEnum)
                return Option.None;
            T e;
            if (!Enum.TryParse(s, out e))
                return Option.None;
            if (!Enum.IsDefined(type, e))
                return Option.None;
            return Option.GetSome(e);
        }
		
        /// <summary>
        /// Gets the relative path by removing a prefix, i.e. this function won't add dots like in "../../some/path".
        /// E.g. if "/tmp" is the prefix then "/tmp/some/path" becomes "some/path", but "/usr/bin" is unchanged, and "/tmp" becomes just ".".
        /// </summary>
        /// <param name="path"></param>
        /// <param name="referencePrefixPath"></param>
        /// <returns></returns>
        public static string GetPrefixRelativePath(string path, string referencePrefixPath)
        {
            return GetPrefixRelativePathOption(path, referencePrefixPath).Else(path);
        }

        /// <summary>
        /// Gets the relative path by removing a prefix, i.e. this function won't add dots like in "../../some/path".
        /// E.g. if "/tmp" is the prefix then "/tmp/some/path" becomes "some/path", and "/tmp" becomes just ".".
        /// </summary>
        /// <param name="path"></param>
        /// <param name="referencePrefixPath"></param>
        /// <returns></returns>
        public static Option<string> GetPrefixRelativePathOption(string path, string referencePrefixPath)
        {
            if (string.IsNullOrEmpty(referencePrefixPath))
                return Option.None;
            char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            string normReferencePath = referencePrefixPath.TrimEnd(seps).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            string normPath = path.TrimEnd(seps).Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            if (normPath == normReferencePath)
                return Option.GetSome(".");
            if (normPath.StartsWith(normReferencePath))
                return Option.GetSome(path.Substring(normReferencePath.Length + 1));
            return Option.None;
        }

        /// <summary>
        /// E.g. common/path and common/other/path -> common, C:\Temp and C:\Users -> C:\, some/path and another/path -> None.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static Option<string> FindCommonPrefixFolder(string path1, string path2)
        {
            if (string.IsNullOrEmpty(path1))
                return Option.None;
            if (string.IsNullOrEmpty(path2))
                return Option.None;
            char[] seps = { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar };
            string normPath1 = path1.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();
            string normPath2 = path2.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).ToLowerInvariant();

            var prefixCount = normPath1.Zip(normPath2, (x, y) => x == y).TakeWhile(x => x).Count();
            if (prefixCount == 0)
                return Option.None;
            var commonPath = path1.Substring(0, prefixCount); // e.g. some/path and some/part -> some/pa
            if (normPath1[prefixCount - 1] == Path.AltDirectorySeparatorChar) // e.g. some/path/
                commonPath += "x"; // E.g. some/path/x or C:/x
            return Path.GetDirectoryName(commonPath).AsOption(); // e.g. some/pa -> returning "some"
        }

        public static T[] AsArray<T>(params T[] ts)
        {
            return ts;
        }

        public static T[] AsNonNullArray<T>(params T[] ts) where T : class
        {
            if (ts == null)
                return new T[0];
            return ts.Where(x => x != null).ToArray();
        }

        public static void Swap<T>(ref T t1, ref T t2)
        {
            T temp = t1;
            t1 = t2;
            t2 = temp;
        }

        /// <summary>
        /// E.g. 2012-12-22 13:50:50 +02:00
        /// </summary>
        public const string ReadableIso8601TimeFormatString = "yyyy'-'MM'-'dd' 'HH':'mm':'ss' 'zzz";

        public const string ReadableIso8601TimeFormatStringWithoutTimeZone = "yyyy'-'MM'-'dd' 'HH':'mm':'ss";

        /// <summary>
        /// Current time in readable iso-8601 format. E.g. "2012-12-22 13:50:50 +02:00" for Dec 22, 1:50:50 PM.
        /// </summary>
        public static string CurrentTimeString
        {
            get
            {
                return DateTimeOffset.Now.ToString(ReadableIso8601TimeFormatString);
            }
        }

        public static System.IO.FileInfo GetExecutingAssemblyLocation()
        {
            return new System.IO.FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public static System.IO.DirectoryInfo GetDirectoryOfExecutingAssembly()
        {
            return GetExecutingAssemblyLocation().Directory;
        }

        public static bool IsCurrentTimeSameAsSwedishTime()
        {
            var utcNow = DateTimeOffset.UtcNow;
            var swedishTime = ConvertToSwedishTime(utcNow);
            var localTime = TimeZoneInfo.ConvertTime(utcNow, TimeZoneInfo.Local);
            return swedishTime == localTime;
        }

        public static DateTimeOffset ConvertToSwedishTime(DateTimeOffset utcTime)
        {
            //TimeZoneInfo swedishTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
            //var swedishTime = TimeZoneInfo.ConvertTime(utcTime, swedishTimeZoneInfo);
            var swedishTime = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(utcTime, "W. Europe Standard Time");
            return swedishTime;
        }

        public static string GetTempPath(string extension = "txt", string prefix = "", string folderPath = null)
        {
            folderPath = folderPath ?? Path.GetTempPath();
            int counter = 1;
            var currentTime = DateTimeOffset.Now.ToString(ReadableIso8601TimeFormatStringWithoutTimeZone.Replace(":", "."));

            var fileName = string.Format("{0}{1}.{2}", prefix, currentTime, extension); // first try wihout counter
            var filePath = Path.Combine(folderPath, fileName);

            while (File.Exists(filePath))
            {
                fileName = string.Format("{0}{1} - {2}.{3}", prefix, currentTime, counter, extension);
                filePath = Path.Combine(folderPath, fileName);
                counter++;
            }

            return filePath;
        }

        public static string[][] ParseTsv(string text, string comment = null)
        {
            return text.Lines().Where(x => !(x.Trim().Length == 0 || (comment != null && x.Trim().StartsWith(comment)))).Select(line => line.Split("\t").Select(x => x.Trim()).ToArray()).Where(x => x.Any()).ToArray();
        }

        /// <summary>
        /// Like System.Io.File.ReadAllText, but can also read files locked by e.g. Excel.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string ReadAllText(string path, Encoding encoding = null)
        {
            //return System.IO.File.ReadAllText(path, encoding: Encoding.UTF8);

            encoding = encoding ?? Encoding.UTF8;

            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var textReader = new StreamReader(fileStream, encoding))
            {
                return textReader.ReadToEnd();
            }
        }

        /// <summary>
        /// Example: using (Utils.SetUnsetMagic(() => obj.SomeProperty, "temp disable")) { DoSomething(); }
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="variable"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Undoer.UndoOnDispose SetUnsetMagic<T>(System.Linq.Expressions.Expression<Func<T>> variable, T value)
        {
            var oldValue = variable.Compile()();

            var parameterExpression = System.Linq.Expressions.Expression.Parameter(typeof(T));
            var assignExpr = System.Linq.Expressions.Expression.Assign(variable.Body, parameterExpression);
            var setter = System.Linq.Expressions.Expression.Lambda<Action<T>>(assignExpr, parameterExpression).Compile();

            return new Undoer(() => setter(value), () => setter(oldValue)).Do();
        }

        public static Func<T1, TResult> GetFunc<T1, TResult>(Func<T1, TResult> func) { return func; }
        public static Func<T1, T2, TResult> GetFunc<T1, T2, TResult>(Func<T1, T2, TResult> func) { return func; }
        public static Func<T1, T2, T3, TResult> GetFunc<T1, T2, T3, TResult>(Func<T1, T2, T3, TResult> func) { return func; }
        public static Func<T1, T2, T3, T4, TResult> GetFunc<T1, T2, T3, T4, TResult>(Func<T1, T2, T3, T4, TResult> func) { return func; }
        public static Func<T1, T2, T3, T4, T5, TResult> GetFunc<T1, T2, T3, T4, T5, TResult>(Func<T1, T2, T3, T4, T5, TResult> func) { return func; }
        public static Func<T1, T2, T3, T4, T5, T6, TResult> GetFunc<T1, T2, T3, T4, T5, T6, TResult>(Func<T1, T2, T3, T4, T5, T6, TResult> func) { return func; }
        public static Func<T1, T2, T3, T4, T5, T6, T7, TResult> GetFunc<T1, T2, T3, T4, T5, T6, T7, TResult>(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func) { return func; }

        [ThreadStatic]
        private static WeakReference sha1Reference;
        public static System.Security.Cryptography.SHA1Managed SHA1ManagedInstance
        {
            get
            {
                if(sha1Reference == null)
                    sha1Reference = new WeakReference(null);
                var cachedWrapped = sha1Reference.Target as DisposeOnGCWrapper<System.Security.Cryptography.SHA1Managed>;
                if (cachedWrapped != null)
                    return cachedWrapped.Value;

                var wrapped = new System.Security.Cryptography.SHA1Managed().WrapToDisposeOnGC();
                sha1Reference.Target = wrapped;
                return wrapped.Value;
            }
        }

        /// <summary>
        /// Writes a standard TSV (tab-separated values) file, using UTF-8 and converting existing tabs to (single) spaces, including the expected end-of-file newline.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public static void WriteTsv(string fileName, IEnumerable<string[]> data, Encoding encoding = null)
        {
            encoding = encoding ?? Encoding.UTF8;
            File.WriteAllLines(fileName, GetTsvLines(data), encoding);
        }

        /// <summary>
        /// Gets a standard TSV (tab-separated values) string of the given data, converting existing tabs to (single) spaces.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetTsvString(IEnumerable<string[]> data)
        {
            return string.Join(Environment.NewLine, GetTsvLines(data));
        }

        private static IEnumerable<string> GetTsvLines(IEnumerable<string[]> data)
        {
            return data.Select(row => string.Join("\t", row.Select(entry => entry.Replace('\t', ' '))));
        }

        /// <summary>
        /// Writes an Excel-specific TSV file that can be double-clicked if saved as e.g. data.csv.
        /// It is encoded using UTF-16LE and with existing tabs and new-lines converted to (single) spaces.
        /// This is required since Excel is unable to easily open standard tsv-files.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="data"></param>
        public static void WriteTsvForExcel(string fileName, IEnumerable<string[]> data)
        {
            var dataWithoutNewLines = data.Select(row => row.Select(x => string.Join(" ", x.Lines())).ToArray());
            File.WriteAllLines(fileName, GetTsvLines(dataWithoutNewLines), Encoding.Unicode);
        }
    }

    public class StaticRandomImp : Random
    {
        internal readonly object Lock = new object();

        /// <summary>
        /// Fills the elements of a specified array of bytes with random numbers.
        /// </summary>
        /// <param name="buffer">An array of bytes to contain random numbers. 
        ///                 </param><exception cref="T:System.ArgumentNullException"><paramref name="buffer"/> is null. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override void NextBytes(byte[] buffer)
        {
            lock (Lock)
                base.NextBytes(buffer);
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override double NextDouble()
        {
            lock (Lock)
                return base.NextDouble();
        }

        /// <summary>
        /// Returns a nonnegative random number less than the specified maximum.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero, and less than <paramref name="maxValue"/>; that is, the range of return values ordinarily includes zero but not <paramref name="maxValue"/>. However, if <paramref name="maxValue"/> equals zero, <paramref name="maxValue"/> is returned.
        /// </returns>
        /// <param name="maxValue">The exclusive upper bound of the random number to be generated. <paramref name="maxValue"/> must be greater than or equal to zero. 
        ///                 </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="maxValue"/> is less than zero. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override int Next(int maxValue)
        {
            lock (Lock)
                return base.Next(maxValue);
        }

        /// <summary>
        /// Returns a random number within a specified range.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to <paramref name="minValue"/> and less than <paramref name="maxValue"/>; that is, the range of return values includes <paramref name="minValue"/> but not <paramref name="maxValue"/>. If <paramref name="minValue"/> equals <paramref name="maxValue"/>, <paramref name="minValue"/> is returned.
        /// </returns>
        /// <param name="minValue">The inclusive lower bound of the random number returned. 
        ///                 </param><param name="maxValue">The exclusive upper bound of the random number returned. <paramref name="maxValue"/> must be greater than or equal to <paramref name="minValue"/>. 
        ///                 </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="minValue"/> is greater than <paramref name="maxValue"/>. 
        ///                 </exception><filterpriority>1</filterpriority>
        public override int Next(int minValue, int maxValue)
        {
            lock (Lock)
                return base.Next(minValue, maxValue);
        }

        /// <summary>
        /// Returns a nonnegative random number.
        /// </summary>
        /// <returns>
        /// A 32-bit signed integer greater than or equal to zero and less than <see cref="F:System.Int32.MaxValue"/>.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public override int Next()
        {
            lock (Lock)
                return base.Next();
        }

        /// <summary>
        /// Returns a random number between 0.0 and 1.0.
        /// </summary>
        /// <returns>
        /// A double-precision floating point number greater than or equal to 0.0, and less than 1.0.
        /// </returns>
        protected override double Sample()
        {
            lock (Lock)
                return base.Sample();
        }
    }


    public class IdentityEqualityComparer<T> : IEqualityComparer<T> where T : class
    {
        public int GetHashCode(T value)
        {
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
        }

        public bool Equals(T left, T right)
        {
            return ReferenceEquals(left, right);
        }
    }

    public class NaturalStringComparer : StringComparer
    {
        private StringComparer stringComparer;
        private EnumerableComparer<object> enumerableComparer;
        private bool useFloatingPoint = false;

        public NaturalStringComparer() : this(OrdinalIgnoreCase)
        {
        }

        public NaturalStringComparer(StringComparer stringComparer, bool useFloatingPoint = false)
        {
            this.stringComparer = stringComparer;
            this.useFloatingPoint = useFloatingPoint;
            enumerableComparer = new EnumerableComparer<object>(ChainingDefaultComparer.Create(stringComparer));
        }

        public override int Compare(string x, string y)
        {
            int result = enumerableComparer.Compare(SplitByNumberOrString(x, useFloatingPoint), SplitByNumberOrString(y, useFloatingPoint));
            return result != 0 ? result : stringComparer.Compare(x, y);
        }

        public override bool Equals(string x, string y)
        {
            return stringComparer.Equals(x, y);
        }

        public override int GetHashCode(string obj)
        {
            return stringComparer.GetHashCode(obj);
        }

        public static IEnumerable<object> SplitByNumberOrString(string input, bool useFloatingPoint = false)
        {
            if (useFloatingPoint)
                return Regex.Split(Regex.Replace(input, "\\s+", " "), "(-?[0-9]*[.,]?[0-9]+)")
                    .Select(s => s.OptionParseDecimal().Transform(x => (object)x).Else(s));
            else
                return Regex.Split(Regex.Replace(input, "\\s+", " "), "([0-9]+)")
                    .Select(s => s.OptionParseInt().Transform(x => (object)x).Else(s));
        }
    }

    public class ChainingDefaultComparer
    {
        public static ChainingDefaultComparer<T> Create<T>(IComparer<T> comparer)
        {
            return new ChainingDefaultComparer<T>(comparer);
        }
    }

    public class ChainingDefaultComparer<T> : IComparer<object>
    {
        private IComparer<T> comp;
        private Comparer<object> objComp = Comparer<object>.Default;

        public ChainingDefaultComparer(IComparer<T> comparer)
        {
            comp = comparer;
        }

        public int Compare(object x, object y)
        {
            int result = 0;
            if(x is T && y is T)
                result = comp.Compare((T) x, (T) y);
            return result != 0 ? result : objComp.Compare(x,y);
        }
    }

    // http://www.interact-sw.co.uk/iangblog/2007/12/13/natural-sorting
    /// <summary>
    /// Compares two sequences.
    /// </summary>
    /// <typeparam name="T">Type of item in the sequences.</typeparam>
    /// <remarks>
    /// Compares elements from the two input sequences in turn. If we
    /// run out of list before finding unequal elements, then the shorter
    /// list is deemed to be the lesser list.
    /// </remarks>
    public class EnumerableComparer<T> : IComparer<IEnumerable<T>>
    {
        public EnumerableComparer()
        {
            comp = Comparer<T>.Default;
        }

        public EnumerableComparer(IComparer<T> comparer)
        {
            comp = comparer;
        }

        private IComparer<T> comp;

        public int Compare(IEnumerable<T> x, IEnumerable<T> y)
        {
            using (IEnumerator<T> leftIt = x.GetEnumerator())
            using (IEnumerator<T> rightIt = y.GetEnumerator())
            {
                while (true)
                {
                    bool left = leftIt.MoveNext();
                    bool right = rightIt.MoveNext();

                    if (!(left || right)) return 0;

                    if (!left) return -1;
                    if (!right) return 1;

                    int itemResult = comp.Compare(leftIt.Current, rightIt.Current);
                    if (itemResult != 0) return itemResult;
                }
            }
        }
    }

    //bool deferUpdatesWasEnabled = sketch.DeferUpdates;
    //var disableDeferUpdatesUndoer = new Undoer(@do: () => sketch.DeferUpdates = false, undo: () => sketch.DeferUpdates = deferUpdatesWasEnabled);
    //using (disableDeferUpdatesUndoer.Do())
    //{
    //}

    public class Undoer
    {
        private readonly Action _do;
        private readonly Action _undo;

        public class UndoOnDispose : IDisposable
        {
            private Undoer _parent;
            public UndoOnDispose(Undoer parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                _parent._undo();
            }
        }

        public Undoer(Action @do, Action undo)
        {
            _do = @do;
            _undo = undo;
        }

        public UndoOnDispose Do()
        {
            _do();
            return new UndoOnDispose(this);
        }
    }

    public class DisposeOnGCWrapper<T> : IDisposable where T : IDisposable
    {
        private Option<T> _value;

        public T Value
        {
            get { return _value.ElseException("Already disposed"); }
        }

        public DisposeOnGCWrapper(T value)
        {
            _value = Option.GetSome(value);
        }

        ~DisposeOnGCWrapper()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (_value.IsNone)
                return;
            var v = _value.Get();
            _value = Option.None;
            v.Dispose();
        }
    }

    #region ListViewExtendedStyles

    public enum ListViewExtendedStyles
    {
        /// <summary>
        /// LVS_EX_GRIDLINES
        /// </summary>
        GridLines = 0x00000001,

        /// <summary>
        /// LVS_EX_SUBITEMIMAGES
        /// </summary>
        SubItemImages = 0x00000002,

        /// <summary>
        /// LVS_EX_CHECKBOXES
        /// </summary>
        CheckBoxes = 0x00000004,

        /// <summary>
        /// LVS_EX_TRACKSELECT
        /// </summary>
        TrackSelect = 0x00000008,

        /// <summary>
        /// LVS_EX_HEADERDRAGDROP
        /// </summary>
        HeaderDragDrop = 0x00000010,

        /// <summary>
        /// LVS_EX_FULLROWSELECT
        /// </summary>
        FullRowSelect = 0x00000020,

        /// <summary>
        /// LVS_EX_ONECLICKACTIVATE
        /// </summary>
        OneClickActivate = 0x00000040,

        /// <summary>
        /// LVS_EX_TWOCLICKACTIVATE
        /// </summary>
        TwoClickActivate = 0x00000080,

        /// <summary>
        /// LVS_EX_FLATSB
        /// </summary>
        FlatsB = 0x00000100,

        /// <summary>
        /// LVS_EX_REGIONAL
        /// </summary>
        Regional = 0x00000200,

        /// <summary>
        /// LVS_EX_INFOTIP
        /// </summary>
        InfoTip = 0x00000400,

        /// <summary>
        /// LVS_EX_UNDERLINEHOT
        /// </summary>
        UnderlineHot = 0x00000800,

        /// <summary>
        /// LVS_EX_UNDERLINECOLD
        /// </summary>
        UnderlineCold = 0x00001000,

        /// <summary>
        /// LVS_EX_MULTIWORKAREAS
        /// </summary>
        MultilWorkAreas = 0x00002000,

        /// <summary>
        /// LVS_EX_LABELTIP
        /// </summary>
        LabelTip = 0x00004000,

        /// <summary>
        /// LVS_EX_BORDERSELECT
        /// </summary>
        BorderSelect = 0x00008000,

        /// <summary>
        /// LVS_EX_DOUBLEBUFFER
        /// </summary>
        DoubleBuffer = 0x00010000,

        /// <summary>
        /// LVS_EX_HIDELABELS
        /// </summary>
        HideLabels = 0x00020000,

        /// <summary>
        /// LVS_EX_SINGLEROW
        /// </summary>
        SingleRow = 0x00040000,

        /// <summary>
        /// LVS_EX_SNAPTOGRID
        /// </summary>
        SnapToGrid = 0x00080000,

        /// <summary>
        /// LVS_EX_SIMPLESELECT
        /// </summary>
        SimpleSelect = 0x00100000
    }

    public enum ListViewMessages
    {
        First = 0x1000,
        SetExtendedStyle = (First + 54),
        GetExtendedStyle = (First + 55),
    }

    /// <summary>
    /// http://stackoverflow.com/questions/87795/how-to-prevent-flickering-in-listview-when-updating-a-single-listviewitems-text
    /// Contains helper methods to change extended styles on ListView, including enabling double buffering.
    /// Based on Giovanni Montrone's article on <see cref="http://www.codeproject.com/KB/list/listviewxp.aspx"/>
    /// </summary>
    public class ListViewHelper
    {
        private ListViewHelper()
        {
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern int SendMessage(IntPtr handle, int messg, int wparam, int lparam);

        public static void SetExtendedStyle(System.Windows.Forms.Control control, ListViewExtendedStyles exStyle)
        {
            ListViewExtendedStyles styles;
            styles = (ListViewExtendedStyles)SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            styles |= exStyle;
            SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }

        public static void EnableDoubleBuffer(System.Windows.Forms.Control control)
        {
            ListViewExtendedStyles styles;
            // read current style
            styles = (ListViewExtendedStyles)SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            // enable double buffer
            styles |= ListViewExtendedStyles.DoubleBuffer;
            // write new style
            SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }

        public static void DisableDoubleBuffer(System.Windows.Forms.Control control)
        {
            ListViewExtendedStyles styles;
            // read current style
            styles = (ListViewExtendedStyles)SendMessage(control.Handle, (int)ListViewMessages.GetExtendedStyle, 0, 0);
            // disable double buffer
            styles -= styles & ListViewExtendedStyles.DoubleBuffer;
            // write new style
            SendMessage(control.Handle, (int)ListViewMessages.SetExtendedStyle, 0, (int)styles);
        }
    }

    #endregion

    public static class Dbg
    {
        // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        [System.Diagnostics.DebuggerNonUserCode]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(bool condition, string message = null)
        {
            System.Diagnostics.Debug.Assert(condition, message);
            if(!condition)
                if(message != null)
                    throw new Exception(message);
                else
                    throw new Exception();
        }

        [System.Diagnostics.DebuggerNonUserCode]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void Assert(Func<bool> condition, string message = null)
        {
            Assert(condition());
        }

        // http://blog.functionalfun.net/2008/05/debuggernonusercode-suppressing.html
        [System.Diagnostics.DebuggerNonUserCode]
        [System.Diagnostics.Conditional("DEBUG")]
        public static void AssertEquals<T>(T value, T expected, string message = null)
        {
            Assert(EqualityComparer<T>.Default.Equals(value, expected), string.Format("Assertion failed! Was \"{0}\" but expected \"{1}\"! {2}", (ReferenceEquals(null, value)) ? "null" : "" + value, (ReferenceEquals(null, expected)) ? "null" : "" + expected, message));
        }

        private static Dictionary<object, System.Diagnostics.Stopwatch> watches = new Dictionary<object, Stopwatch>();

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Trace(object obj)
        {
            Trace((obj ?? "null").ToString(), 2);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        public static void Trace(string msg = null)
        {
            Trace(msg, 2);
        }

        //[System.Diagnostics.Conditional("DEBUG")]
        public static void TraceDiff(string msg = null, object id = null)
        {
            id = id ?? "";
            try
            {
                var watch = watches.OptionGetValue(id);
                if (watch.IsSome)
                    watch.Get().Stop();
            }
            catch
            {
                watches.Remove(id);
            }
            Trace(msg, 2, traceDiffId: Option.GetSome(id));
        }

        private static void Trace(string msg, int levelOfIndirection, Option<object> traceDiffId = default(Option<object>))
        {
            var timeStamp = GetTimeStamp(traceDiffId);
            string callingMethodNameString = "";
            try
            {
                var method = new System.Diagnostics.StackFrame(levelOfIndirection).GetMethod();
                var typeName = method.DeclaringType.GetFriendlyTypeName();
                callingMethodNameString = typeName + "." + method.Name + ": ";
            }
            catch (Exception)
            {
            }
            System.Diagnostics.Trace.WriteLine(timeStamp + " " + callingMethodNameString + (msg ?? "Called!"));
        }

        private static string GetTimeStamp(Option<object> traceDiffId = default(Option<object>))
        {
            string msString = "";
            if(traceDiffId.IsSome)
            {
                var watchOption = watches.OptionGetValue(traceDiffId.Get());
                if (watchOption.IsSome)
                {
                    var watch = watchOption.Get();
                    long ms = watch.ElapsedMilliseconds;
                    msString = " [" + ms + "ms]";
                    watch.Reset();
                    watch.Start();
                }
                else
                    watches[traceDiffId.Get()] = Stopwatch.StartNew();
            }
            var timeStamp = "[" + Utils.CurrentTimeString + "]" + msString;
            return timeStamp;
        }
    }

    public interface ICloneable<T> : ICloneable
    {
        new T Clone();
    }

    public static class Lazy
    {
        /// <summary>
        /// Lazy initalization with <see cref="T:System.Lazy`1"/>. Note: Not thread-safe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctor"></param>
        /// <returns></returns>
        [Pure]
        public static Lazy<T> Simple<T>(Func<T> ctor)
        {
            return new Lazy<T>(ctor, LazyThreadSafetyMode.None);
        }

        /// <summary>
        /// Constant Lazy initalization with <see cref="T:System.Lazy`1"/>. Note: Not thread-safe.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ctor"></param>
        /// <returns></returns>
        [Pure]
        public static Lazy<T> Const<T>(T ctor)
        {
            var lazy = new Lazy<T>(() => ctor, LazyThreadSafetyMode.None);
            var dummy = lazy.Value;
            return lazy;
        }


        [Pure]
        public static Lazy<T> Create<T>() where T : new()
        {
            return Create(() => new T());
        }

        [Pure]
        public static Lazy<T> Create<T>(Func<T> ctor)
        {
            return new Lazy<T>(ctor);
        }

        [Pure]
        public static Lazy<TResult> SelectMany<TSource, TCollection, TResult>(
            this Lazy<TSource> source,
            Func<TSource, Lazy<TCollection>> collectionSelector,
            Func<TSource, TCollection, TResult> resultSelector)
        {
            if (collectionSelector == null) throw new ArgumentNullException("collectionSelector");
            if (resultSelector == null) throw new ArgumentNullException("resultSelector");
            if(source.IsValueCreated)
            {
                var col = collectionSelector(source.Value);
                return col.Select(c => resultSelector(source.Value, c));
            }
            return Lazy.Simple(() => resultSelector(source.Value, collectionSelector(source.Value).Value));
        }

        /// <summary>
        /// "Transforms" a lazy value with a Func. If the value is already created this is strict, otherwise it is lazy.
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="source"></param>
        /// <param name="selector"></param>
        /// <returns></returns>
        [Pure]
        public static Lazy<TResult> Select<TSource, TResult>(this Lazy<TSource> source, Func<TSource, TResult> selector)
        {
            if (selector == null) throw new ArgumentNullException("selector");
            if (source.IsValueCreated)
                return Lazy.Const(selector(source.Value));
            return Lazy.Simple(() => selector(source.Value));
        }

        [Pure]
        public static Option<T> GetCachedValueOption<T>(this Lazy<T> source)
        {
            if (source.IsValueCreated)
                return Option.GetSome(source.Value);
            else
                return Option.None;
        }
    }

    [DataContract]
    public class CommonDotNetExtensionsConfig
    {
        [DataMember]
        public string ExceptionLogPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "exceptions");


        private static string GetConfigFilePath()
        {
            FileInfo dllPath = Utils.GetExecutingAssemblyLocation();
            string confFileName = Path.GetFileNameWithoutExtension(dllPath.FullName) + ".conf"; // Common.DotNet.Extensions.conf
            return Path.Combine(Utils.GetDirectoryOfExecutingAssembly().FullName, confFileName);
        }

        public void SaveToFile()
        {
            string path = GetConfigFilePath();
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            XmlSerialization.Serialize(this, path);
        }

        public static CommonDotNetExtensionsConfig LoadFromFile()
        {
            try
            {
                string path = GetConfigFilePath();
                return XmlSerialization.DeserializeOrDefault<CommonDotNetExtensionsConfig>(path);
            }
            catch (Exception)
            {
                return new CommonDotNetExtensionsConfig();
            }
        }
    }
}
