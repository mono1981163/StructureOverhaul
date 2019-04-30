using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

using Common.DotNet.Extensions;

namespace VaultEagle
{
    public static class ExtensionMethods
    {
        public static string DeepToString(this IEnumerable e)
        {
            return DeepToString_((object)e);
        }

        public static string DeepToString_(object o)
        {
            if (o is string)
                return "'" + o + "'";
            if (o is char)
                return "'" + o + "'";
            if (o is System.Collections.IEnumerable)
                return DeepToString_(((System.Collections.IEnumerable)o).Cast<object>());
            return (o ?? "null").ToString();
        }

        public static string DeepToString_<T>(IEnumerable<T> e)
        {
            return "[" + string.Join(", ", from t in e select DeepToString_(t)) + "]";
        }

        public static string RemoveAndReturnLast(this List<string> returnValues)
        {
            var lastIndex = returnValues.Count - 1;
            var encodedReturnValue = returnValues[lastIndex];
            returnValues.RemoveAt(lastIndex);
            return encodedReturnValue;
        }

        public static T GetLast<T>(this IList<T> list)
        {
            return list[list.Count-1];
        }

        public static T[] Sorted<T>(this IEnumerable<T> source)
        {
            return source.OrderBy(x => x).ToArray();
        }

        public static List<T> Sorted<T>(this List<T> list)
        {
            var newList = new List<T>(list);
            newList.Sort();
            return newList;
        }

        public static IEnumerable<TSource> Distinct<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keyFunction
            )
        {
            var encountered = new HashSet<TKey>();
            foreach (var item in source)
            {
                var key = keyFunction(item);
                if (!encountered.Contains(key))
                {
                    yield return item;
                    encountered.Add(key);
                }
            }
        }

        public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> source)
        {
            foreach (var item in source)
            {
                collection.Add(item);
            }
        }

        public static HashSet<T> ToSet<T>(this IEnumerable<T> source)
        {
            HashSet<T> set = new HashSet<T>();
            foreach (var item in source)
            {
                set.Add(item);
            }
            return set;
        }

        public static SortedDictionary<TKey, TValue> ToSortedDictionary<TKey, TValue> (this IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            var dict = source as Dictionary<TKey,TValue>;
            dict = dict ?? source.ToDictionary(kv => kv.Key, kv => kv.Value);
            return new SortedDictionary<TKey, TValue>(dict);
        }

        //public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue> (this IEnumerable<KeyValuePair<TKey, TValue>> source)
        //{
        //    var dict = source as Dictionary<TKey,TValue>;
        //    return dict ?? source.ToDictionary(kv => kv.Key, kv => kv.Value);
        //}

        public static void DisposeUnlessNull<T>(this T disposable, ref T disposableRef) where T : class, IDisposable
        {
            if (disposable != null)
            {
                disposable.Dispose();
                disposableRef = null;
            }
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
        public static Dictionary<bool, List<T>> PartitionBy<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            var result = 
                source.GroupBy(predicate)
                      .ToDictionary(x => x.Key, x => x.ToList());
            foreach (var b in new bool[]{ true, false })
                if (!result.ContainsKey(b))
                    result[b] = new List<T>();
            return result;
        }

        [Pure]
        public static string[] Lines(this string s)
        {
            return s.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
        }

        public static T GetAssemblyAttribute<T>(this System.Reflection.Assembly assembly) where T : System.Attribute
        {
            return (T)Attribute.GetCustomAttribute(assembly, typeof(T));
        }

        public static IEnumerable<System.Xml.XmlNode> AsEnumerable(this System.Xml.XmlNodeList nodes)
        {
            return nodes.Cast<System.Xml.XmlNode>();
        }

#if DEBUG
        public static string Dump(this object value, bool fullDetail = false)
        {
            var sb = new StringBuilder();

            var settings = new Newtonsoft.Json.JsonSerializerSettings() { Converters = new List<Newtonsoft.Json.JsonConverter>() { new Newtonsoft.Json.Converters.StringEnumConverter() } };
            if (!fullDetail)
                settings.DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore;
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(value, Newtonsoft.Json.Formatting.Indented, settings);

            string type = value.GetType().ToString();
            sb.Append(type);
            sb.Append(": ");
            sb.Append(json);
            string s = value.ToString();
            if (s.Trim() != type.Trim())
                sb.Append(".ToString() == \"" + s + "\" ");
            System.Diagnostics.Debug.WriteLine(sb.ToString());
            return sb.ToString();
        }
#endif
    }
}