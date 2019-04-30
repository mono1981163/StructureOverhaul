using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public class AlwaysRule : Rule
    {
        public const string Type = "Always";
        public string TypeName { get { return Type; } }
        public override string ToString() { return Type; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool Matches(Context context, RuleTarget target)
        {
            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Write(XmlDocument document, XmlElement element)
        {
        }
    }
}
