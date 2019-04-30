using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Common.DotNet.Extensions
{
    public class TreeUtils
    {
        /// <summary>
        /// Depth-first traversal.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="item"></param>
        /// <param name="getChildren"></param>
        /// <param name="maxDepth"></param>
        /// <returns></returns>
        public static IEnumerable<T> Flatten<T>(T item, Func<T, IEnumerable<T>> getChildren, int? maxDepth = null)
        {
            if(maxDepth.HasValue && maxDepth < 0)
                yield break;
            yield return item;
            if(maxDepth.HasValue && maxDepth < 1)
                yield break;
            foreach (var child in getChildren(item))
                foreach (var flattenedChild in Flatten(child, getChildren, maxDepth-1))
                    yield return flattenedChild;
        }

        public static IEnumerable<T> Flatten<T>(IEnumerable<T> items, Func<T, IEnumerable<T>> getChildren, int? maxDepth = null)
        {
            return items.SelectMany(item => Flatten(item, getChildren, maxDepth));
        }
    }
}
