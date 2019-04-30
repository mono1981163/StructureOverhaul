using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Common.DotNet.Extensions
{
    public class CursorWrapper : IDisposable
    {
        private Cursor PreviousCursor;

        public CursorWrapper(Cursor newCursor)
        {
            PreviousCursor = Cursor.Current;
            Cursor.Current = newCursor;
        }

        public void Dispose()
        {
            Cursor.Current = PreviousCursor;
        }
    }
}
