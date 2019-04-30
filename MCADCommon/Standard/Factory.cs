using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard
{
    public interface Factory<T>
    {
        IEnumerable<string> Types { get; }

        bool CanCreate(string type);

        T Read(Context context, string type, XmlElement element);
    }
}
