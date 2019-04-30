using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public interface Rule
    {
        string TypeName { get; }

        bool Matches(Context context, RuleTarget target);

        void Write(XmlDocument document, XmlElement element);
    }
}
