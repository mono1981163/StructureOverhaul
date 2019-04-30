using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.GUI
{
    public interface Control
    {
        string Type { get; }

        string Name { get; }
        string InternalName { get; }

        void Initialize(Context context);

        void Write(XmlDocument document, XmlElement element);
    }
}
