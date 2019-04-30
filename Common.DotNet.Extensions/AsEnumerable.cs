using System;
using System.Collections;
using System.Linq;
using System.Collections.Generic;

namespace Common.DotNet.Extensions
{
    public static class MiscAsEnumerableExtensionMethods
    {

        public static IEnumerable<System.Data.DataColumn> AsEnumerable(this System.Data.DataColumnCollection source)
        {
            TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<System.Data.DataColumn>();
        }

        public static IEnumerable<System.Data.DataRow> AsEnumerable(this System.Data.DataRowCollection source)
        {
            TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<System.Data.DataRow>();
        }

        public static IEnumerable<System.Text.RegularExpressions.Match> AsEnumerable(this System.Text.RegularExpressions.MatchCollection source)
        {
            TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<System.Text.RegularExpressions.Match>();
        }

        public static IEnumerable<System.Xml.XPath.XPathNavigator> AsEnumerable(this System.Xml.XPath.XPathNodeIterator source)
        {
            TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s.Current);
            return source.Cast<System.Xml.XPath.XPathNavigator>();
        }

        public static IEnumerable<string> AsEnumerable(this System.Collections.Specialized.NameValueCollection source)
        {
            TypeCheckEnumerable(source, s => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<string>();
        }

        public static IEnumerable<bool> AsEnumerable(this System.Collections.BitArray source)
        {
            TypeCheckEnumerable(source, s => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<bool>();
        }

        public static IEnumerable<object> AsEnumerable(this System.Data.DataRow source)
        {
            int columnCount = source.Table.Columns.Count;
            for (int i = 0; i < columnCount; i++)
                yield return source[i];
        }

        [System.Diagnostics.Conditional("ALWAYZ__EXKLUDE_ME_")]
        private static void TypeCheckEnumerable<TSource, TItem>(TSource t1, Func<TSource, IEnumerable<TItem>> getEnumerableFunc, Func<TSource, TItem> getSingleElementFunc)
        {
        }
    }

    // Workaround for C# compiler bug requiring that in this case would require Winforms to be linked in.
    // http://stackoverflow.com/a/5999420/579344
    namespace Winforms
    {
        public static class WinformsAsEnumerableExtensionMethods
        {
            public static IList<System.Windows.Forms.DataGridViewRow> AsIList(this System.Windows.Forms.DataGridViewRowCollection source)
            {
                return IListWrapper.Get(source, s => s[0]);
            }

            public static IEnumerable<System.Windows.Forms.DataGridViewRow> AsEnumerable(this System.Windows.Forms.DataGridViewRowCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.DataGridViewRow>();
            }

            public static IList<System.Windows.Forms.DataGridViewCell> AsIList(this System.Windows.Forms.DataGridViewCellCollection source)
            {
                return IListWrapper.Get(source, s => s[0]);
            }

            public static IEnumerable<System.Windows.Forms.DataGridViewCell> AsEnumerable(this System.Windows.Forms.DataGridViewCellCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.DataGridViewCell>();
            }

            public static IList<System.Windows.Forms.DataGridViewColumn> AsIList(this System.Windows.Forms.DataGridViewColumnCollection source)
            {
                return IListWrapper.Get(source, s => s[0]);
            }

            public static IEnumerable<object> AsEnumerable(this System.Windows.Forms.ComboBox.ObjectCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<object>();
            }

            public static IEnumerable<System.Windows.Forms.DataGridViewColumn> AsEnumerable(this System.Windows.Forms.DataGridViewColumnCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.DataGridViewColumn>();
            }

            public static IEnumerable<System.Windows.Forms.ColumnHeader> AsEnumerable(this System.Windows.Forms.ListView.ColumnHeaderCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.ColumnHeader>();
            }

            public static IEnumerable<System.Windows.Forms.ToolStripItem> AsEnumerable(this System.Windows.Forms.ToolStripItemCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.ToolStripItem>();
            }

            public static IEnumerable<System.Windows.Forms.TreeNode> GetAllNodes(this System.Windows.Forms.TreeView source)
            {
                return source.GetNodes().SelectMany(node => TreeUtils.Flatten(node, n => n.GetNodes()));
            }

            public static IEnumerable<System.Windows.Forms.TreeNode> GetNodes(this System.Windows.Forms.TreeNode source)
            {
                TypeCheckEnumerable(source, (s) => s.GetNodes(), (s) => s.Nodes[0]);
                return source.Nodes.Cast<System.Windows.Forms.TreeNode>();
            }

            public static IEnumerable<System.Windows.Forms.TreeNode> GetNodes(this System.Windows.Forms.TreeView source)
            {
                TypeCheckEnumerable(source, (s) => s.GetNodes(), (s) => s.Nodes[0]);
                return source.Nodes.Cast<System.Windows.Forms.TreeNode>();
            }

            public static IEnumerable<System.Windows.Forms.TreeNode> AsEnumerable(
                this System.Windows.Forms.TreeNodeCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.TreeNode>();
            }

            public static IEnumerable<System.Windows.Forms.ListViewItem> AsEnumerable(
                this System.Windows.Forms.ListView.ListViewItemCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.ListViewItem>();
            }

            public static IEnumerable<System.Windows.Forms.ListViewItem.ListViewSubItem> AsEnumerable(
                this System.Windows.Forms.ListViewItem.ListViewSubItemCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.ListViewItem.ListViewSubItem>();
            }

            public static IEnumerable<int> AsEnumerable(this System.Windows.Forms.ListView.SelectedIndexCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<int>();
            }

            public static IEnumerable<System.Windows.Forms.ListViewItem> AsEnumerable(this System.Windows.Forms.ListView.SelectedListViewItemCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.ListViewItem>();
            }

            public static IEnumerable<System.Windows.Forms.Control> AsEnumerable(this System.Windows.Forms.Control.ControlCollection source)
            {
                TypeCheckEnumerable(source, (s) => s.AsEnumerable(), (s) => s[0]);
                return source.Cast<System.Windows.Forms.Control>();
            }

            public static class IListWrapper
            {
                public static Wrapper<TItem> Get<TSource, TItem>(TSource source, Func<TSource, TItem> getSingleElementFunc) where TSource : IList
                {
                    return new Wrapper<TItem>(source);
                }

                public class Wrapper<T> : IList<T>
                {
                    private IList source;

                    /// <summary>
                    /// Initializes a new instance of the <see cref="T:System.Object"/> class.
                    /// </summary>
                    public Wrapper(IList source)
                    {
                        this.source = source;
                    }

                    /// <summary>
                    /// Returns an enumerator that iterates through the collection.
                    /// </summary>
                    /// <returns>
                    /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
                    /// </returns>
                    IEnumerator<T> IEnumerable<T>.GetEnumerator()
                    {
                        return source.Cast<T>().GetEnumerator();
                    }

                    /// <summary>
                    /// Returns an enumerator that iterates through a collection.
                    /// </summary>
                    /// <returns>
                    /// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
                    /// </returns>
                    public IEnumerator GetEnumerator()
                    {
                        return source.GetEnumerator();
                    }

                    /// <summary>
                    /// Copies the elements of the <see cref="T:System.Collections.ICollection"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
                    /// </summary>
                    /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection"/>. The <see cref="T:System.Array"/> must have zero-based indexing. </param><param name="index">The zero-based index in <paramref name="array"/> at which copying begins. </param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null. </exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is less than zero. </exception><exception cref="T:System.ArgumentException"><paramref name="array"/> is multidimensional.-or- The number of elements in the source <see cref="T:System.Collections.ICollection"/> is greater than the available space from <paramref name="index"/> to the end of the destination <paramref name="array"/>.-or-The type of the source <see cref="T:System.Collections.ICollection"/> cannot be cast automatically to the type of the destination <paramref name="array"/>.</exception>
                    public void CopyTo(Array array, int index)
                    {
                        source.CopyTo(array, index);
                    }

                    /// <summary>
                    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
                    /// </summary>
                    /// <returns>
                    /// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
                    /// </returns>
                    /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
                    public bool Remove(T item)
                    {
                        var found = IndexOf(item) != -1;
                        source.Remove(item);
                        return found;
                    }

                    /// <summary>
                    /// Gets the number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
                    /// </summary>
                    /// <returns>
                    /// The number of elements contained in the <see cref="T:System.Collections.ICollection"/>.
                    /// </returns>
                    public int Count
                    {
                        get { return source.Count; }
                    }

                    /// <summary>
                    /// Gets an object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
                    /// </summary>
                    /// <returns>
                    /// An object that can be used to synchronize access to the <see cref="T:System.Collections.ICollection"/>.
                    /// </returns>
                    public object SyncRoot
                    {
                        get { return source.SyncRoot; }
                    }

                    /// <summary>
                    /// Gets a value indicating whether access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe).
                    /// </summary>
                    /// <returns>
                    /// true if access to the <see cref="T:System.Collections.ICollection"/> is synchronized (thread safe); otherwise, false.
                    /// </returns>
                    public bool IsSynchronized
                    {
                        get { return source.IsSynchronized; }
                    }

                    /// <summary>
                    /// Adds an item to the <see cref="T:System.Collections.IList"/>.
                    /// </summary>
                    /// <returns>
                    /// The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection,
                    /// </returns>
                    /// <param name="value">The object to add to the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
                    public int Add(object value)
                    {
                        return source.Add(value);
                    }

                    /// <summary>
                    /// Determines whether the <see cref="T:System.Collections.IList"/> contains a specific value.
                    /// </summary>
                    /// <returns>
                    /// true if the <see cref="T:System.Object"/> is found in the <see cref="T:System.Collections.IList"/>; otherwise, false.
                    /// </returns>
                    /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>. </param>
                    public bool Contains(object value)
                    {
                        return source.Contains(value);
                    }

                    /// <summary>
                    /// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
                    /// </summary>
                    /// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.</exception>
                    public void Add(T item)
                    {
                        source.Add(item);
                    }

                    /// <summary>
                    /// Removes all items from the <see cref="T:System.Collections.IList"/>.
                    /// </summary>
                    /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only. </exception>
                    public void Clear()
                    {
                        source.Clear();
                    }

                    /// <summary>
                    /// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
                    /// </summary>
                    /// <returns>
                    /// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
                    /// </returns>
                    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.</param>
                    public bool Contains(T item)
                    {
                        return source.Contains(item);
                    }

                    /// <summary>
                    /// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
                    /// </summary>
                    /// <param name="array">The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.</param><param name="arrayIndex">The zero-based index in <paramref name="array"/> at which copying begins.</param><exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception><exception cref="T:System.ArgumentException">The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.</exception>
                    public void CopyTo(T[] array, int arrayIndex)
                    {
                        source.CopyTo(array, arrayIndex);
                    }

                    /// <summary>
                    /// Determines the index of a specific item in the <see cref="T:System.Collections.IList"/>.
                    /// </summary>
                    /// <returns>
                    /// The index of <paramref name="value"/> if found in the list; otherwise, -1.
                    /// </returns>
                    /// <param name="value">The object to locate in the <see cref="T:System.Collections.IList"/>. </param>
                    public int IndexOf(object value)
                    {
                        return source.IndexOf(value);
                    }

                    /// <summary>
                    /// Inserts an item to the <see cref="T:System.Collections.IList"/> at the specified index.
                    /// </summary>
                    /// <param name="index">The zero-based index at which <paramref name="value"/> should be inserted. </param><param name="value">The object to insert into the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception><exception cref="T:System.NullReferenceException"><paramref name="value"/> is null reference in the <see cref="T:System.Collections.IList"/>.</exception>
                    public void Insert(int index, object value)
                    {
                        source.Insert(index, value);
                    }

                    /// <summary>
                    /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList"/>.
                    /// </summary>
                    /// <param name="value">The object to remove from the <see cref="T:System.Collections.IList"/>. </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
                    public void Remove(object value)
                    {
                        source.Remove(value);
                    }

                    /// <summary>
                    /// Determines the index of a specific item in the <see cref="T:System.Collections.Generic.IList`1"/>.
                    /// </summary>
                    /// <returns>
                    /// The index of <paramref name="item"/> if found in the list; otherwise, -1.
                    /// </returns>
                    /// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.IList`1"/>.</param>
                    public int IndexOf(T item)
                    {
                        return source.IndexOf(item);
                    }

                    /// <summary>
                    /// Inserts an item to the <see cref="T:System.Collections.Generic.IList`1"/> at the specified index.
                    /// </summary>
                    /// <param name="index">The zero-based index at which <paramref name="item"/> should be inserted.</param><param name="item">The object to insert into the <see cref="T:System.Collections.Generic.IList`1"/>.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
                    public void Insert(int index, T item)
                    {
                        source.Insert(index, item);
                    }

                    /// <summary>
                    /// Removes the <see cref="T:System.Collections.IList"/> item at the specified index.
                    /// </summary>
                    /// <param name="index">The zero-based index of the item to remove. </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList"/> is read-only.-or- The <see cref="T:System.Collections.IList"/> has a fixed size. </exception>
                    public void RemoveAt(int index)
                    {
                        source.RemoveAt(index);
                    }

                    /// <summary>
                    /// Gets or sets the element at the specified index.
                    /// </summary>
                    /// <returns>
                    /// The element at the specified index.
                    /// </returns>
                    /// <param name="index">The zero-based index of the element to get or set.</param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.Generic.IList`1"/>.</exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.Generic.IList`1"/> is read-only.</exception>
                    T IList<T>.this[int index]
                    {
                        get { return (T) source[index]; }
                        set { source[index] = value; }
                    }

                    /// <summary>
                    /// Gets or sets the element at the specified index.
                    /// </summary>
                    /// <returns>
                    /// The element at the specified index.
                    /// </returns>
                    /// <param name="index">The zero-based index of the element to get or set. </param><exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index"/> is not a valid index in the <see cref="T:System.Collections.IList"/>. </exception><exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.IList"/> is read-only. </exception>
                    public object this[int index]
                    {
                        get { return source[index]; }
                        set { source[index] = value; }
                    }

                    /// <summary>
                    /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> is read-only.
                    /// </summary>
                    /// <returns>
                    /// true if the <see cref="T:System.Collections.IList"/> is read-only; otherwise, false.
                    /// </returns>
                    public bool IsReadOnly
                    {
                        get { return source.IsReadOnly; }
                    }

                    /// <summary>
                    /// Gets a value indicating whether the <see cref="T:System.Collections.IList"/> has a fixed size.
                    /// </summary>
                    /// <returns>
                    /// true if the <see cref="T:System.Collections.IList"/> has a fixed size; otherwise, false.
                    /// </returns>
                    public bool IsFixedSize
                    {
                        get { return source.IsFixedSize; }
                    }
                }
            }

            [System.Diagnostics.Conditional("ALWAYZ__EXKLUDE_ME_")]
            private static void TypeCheckEnumerable<TSource, TItem>(TSource t1, Func<TSource, IEnumerable<TItem>> getEnumerableFunc, Func<TSource, TItem> getSingleElementFunc)
            {
            }
        }
    }

    public static class XmlAsEnumerableExtensionMethods
    {
        public static IEnumerable<System.Xml.XmlAttribute> AsEnumerable(this System.Xml.XmlAttributeCollection source)
        {
            if (source == null)
                return new System.Xml.XmlAttribute[0];
            TypeCheckEnumerable(source, s => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<System.Xml.XmlAttribute>();
        }

        public static IEnumerable<System.Xml.XmlNode> AsEnumerable(this System.Xml.XmlNodeList source)
        {
            if (source == null)
                return new System.Xml.XmlNode[0];
            TypeCheckEnumerable(source, s => s.AsEnumerable(), (s) => s[0]);
            return source.Cast<System.Xml.XmlNode>();
        }

        [System.Diagnostics.Conditional("ALWAYZ__EXKLUDE_ME_")]
        private static void TypeCheckEnumerable<TSource, TItem>(TSource t1, Func<TSource, IEnumerable<TItem>> getEnumerableFunc, Func<TSource, TItem> getSingleElementFunc)
        {
        }
    }
}
