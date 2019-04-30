using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.GUI
{
    public interface Location
    {
        string Type { get; }

        void Add(Context context, Control control);

        void Write(XmlDocument document, XmlElement element);
    }
}
