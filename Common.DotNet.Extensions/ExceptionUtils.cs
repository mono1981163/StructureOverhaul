using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Common.DotNet.Extensions
{
    [Serializable]
    public class SimpleException<T> : Exception
    {
        //
        // For guidelines regarding the creation of new exception types, see
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
        // and
        //    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
        //

        public SimpleException()
        {
        }

        public SimpleException(string message)
            : base(message)
        {
        }

        public SimpleException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected SimpleException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
        }
    }

    public static class ExceptionUtils
    {
        private static Lazy<Func<object, string>> MicrosoftVisualBasicInformationTypeName = Lazy.Simple(GetMicrosoftVisualBasicInformationTypeName);
        private static Func<object, string> GetMicrosoftVisualBasicInformationTypeName()
        {
            try
            {
                var microsoftVisualBasic = System.Reflection.Assembly.Load("Microsoft.VisualBasic, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a");
                var information = microsoftVisualBasic.GetType("Microsoft.VisualBasic.Information");
                var typeName = information.GetMethod("TypeName");
                return Delegate.CreateDelegate(typeof(Func<object, string>), typeName).CastTo<Func<object, string>>();
            }
            catch (Exception)
            {
                return _ => null;
            }
        }

        /// <summary>
        /// Don't use this on value types!
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        [Obsolete]
        public static void CastTo<T>(this ValueType obj)
        {
            ((object) obj).CastTo<T>();
            throw new InvalidCastException();
        }

        /// <summary>
        /// Instead of (string)obj, a bit like static_cast&lt;T&gt; in C++.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        [Pure]
        public static T CastTo<T>(this object obj)
        {
            if(obj == null)
// ReSharper disable ExpressionIsAlwaysNull
                return (T)obj;
// ReSharper restore ExpressionIsAlwaysNull

            if (obj is T)
                return (T)obj;

            var typeFullName = obj.GetType().FullName;
            if (typeFullName == "System.__ComObject")
            {
                var vbType = MicrosoftVisualBasicInformationTypeName.Value(obj);
                if (vbType != null)
                    typeFullName = vbType;
            }
            ExceptionContext.AddInfo("castFromType", typeFullName);
// ReSharper disable PossibleInvalidCastException
            return (T)obj;
// ReSharper restore PossibleInvalidCastException
        }
    }

    public static class ExceptionContext
    {
        public class Context
        {
            private List<Tuple<string, Lazy<string>>> _currentContextStack;

            public Context(List<Tuple<string, Lazy<string>>> currentContextStack)
            {
                _currentContextStack = new List<Tuple<string, Lazy<string>>>(currentContextStack);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public void InExisting(Action action)
            {
                int stackHeightBefore = FullContext.Count;
                FullContext.Add(_currentContextStack);

                action(); // may throw exception, that's the point

                ResetStack(stackHeightBefore, FullContext);
            }

            [System.Diagnostics.DebuggerNonUserCode]
            public T InExisting<T>(Func<T> action)
            {
                int stackHeightBefore = FullContext.Count;
                FullContext.Add(_currentContextStack);

                var t = action(); // may throw exception, that's the point

                ResetStack(stackHeightBefore, FullContext);
                return t;
            }
        }

        [ThreadStatic]
        private static List<List<Tuple<string, Lazy<string>>>> _currentContextStack;

        [System.Diagnostics.DebuggerNonUserCode]
        public static void InNew(Action action, params Tuple<string,string>[] info)
        {
            int stackHeightBefore = FullContext.Count;
            int innerStackHeightBefore = FullContext.Last().Count;

            foreach (var tuple in info)
                AddInfo(tuple.Item1, tuple.Item2);
            action(); // may throw exception, that's the point

            ResetStack(stackHeightBefore, FullContext);
            ResetStack(innerStackHeightBefore, FullContext.Last());
        }

        [System.Diagnostics.DebuggerNonUserCode]
        public static T InNew<T>(Func<T> action, params Tuple<string,string>[] info)
        {
            int stackHeightBefore = FullContext.Count;
            int innerStackHeightBefore = FullContext.Last().Count;

            foreach (var tuple in info)
                AddInfo(tuple.Item1, tuple.Item2);
            var t =  action(); // may throw exception, that's the point

            ResetStack(stackHeightBefore, FullContext);
            ResetStack(innerStackHeightBefore, FullContext.Last());
            return t;
        }

        private static void ResetStack<T>(int stackHeightBefore, List<T> stack)
        {
            while (stack.Count > stackHeightBefore)
                stack.RemoveAndReturnLast();
        }

        public static void AddInfo(string key, string value)
        {
            FullContext.Last().Add(Tuple.Create(key, Lazy.Const(value)));
        }

        public static void AddInfo(string key, Func<string> valueFunc)
        {
            FullContext.Last().Add(Tuple.Create(key, Lazy.Simple(valueFunc)));
        }

        public static Context CurrentContext
        {
            get
            {
                return new Context(FullContext.Last());
            }
        }

        private static List<List<Tuple<string, Lazy<string>>>> FullContext
        {
            get
            {
                if (_currentContextStack == null)
                {
                    _currentContextStack = new List<List<Tuple<string, Lazy<string>>>>(); // must initialize in each thread
                    _currentContextStack.Add(new List<Tuple<string, Lazy<string>>>());
                }
                return _currentContextStack;
            }
        }

        public static string InfoProvider()
        {
            var context = FullContext
                .Last()
                .Select(x =>
                            {
                                string value;
                                try
                                {
                                    value = x.Item2.Value;
                                }
                                catch (Exception ex)
                                {
                                    value = string.Format("{{{0}: {1}}}", ex.GetType().FullName, ex.Message);
                                }
                                return Tuple.Create(x.Item1, value);
                            })
                .DistinctBy(x => Tuple.Create(x.Item1.ToLowerInvariant(), x.Item2.ToLowerInvariant()))
                .Select(x => "(context) " +x.Item1 + ": " + x.Item2)
                .ToArray();
            if(context.Any())
                return string.Join("\r\n", context);
            return null;
        }
    }
}
