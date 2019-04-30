using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public class NameRule : Rule
    {
        public const string Type = "Name";
        public string TypeName { get { return Type; } }
        public override string ToString() { return Type; }

        public StringPattern Name { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static NameRule Read(Context context, XmlElement element)
        {
            return new NameRule
            {
                Name = StringPattern.Create(MCAD.XmlCommon.XmlTools.GetElement(element, "name"))
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool Matches(Context context, RuleTarget target)
        {
            return Name.IsMatch(target.Name);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Write(XmlDocument document, XmlElement element)
        {
            element.AppendChild(Name.Write(document, "name"));
        }
    }
}
