using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Common.DotNet.Extensions
{
    public class CircularBuffer<T> : IEnumerable<T>
    {
        private List<T> buffer;
        private int nextIndex = 0;
        private int size;

        public CircularBuffer(int size)
        {
            buffer = new List<T>(CheckSize(size));
            this.size = size;
        }

        private static int CheckSize(int size)
        {
            if(!(size > 0))
                throw new ArgumentException("size must be positive", "size");
            return size;
        }

        public void Add(T item)
        {
            if (nextIndex < buffer.Count)
                buffer[nextIndex] = item;
            else
                buffer.Add(item);

            nextIndex = (nextIndex + 1) % size;
        }

        private IEnumerable<T> _AsEnumerable()
        {
            if(buffer.Count < size)
                return buffer;
            return buffer.Skip(nextIndex).Concat(buffer.Take(nextIndex));
        }

        public int Count
        {
            get { return buffer.Count; }
        }

        public T[] ToArray()
        {
            return _AsEnumerable().ToArray();
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _AsEnumerable().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
