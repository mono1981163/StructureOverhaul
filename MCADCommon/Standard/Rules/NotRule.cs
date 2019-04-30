using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public class NotRule : Rule
    {
        public const string Type = "Not";
        public string TypeName { get { return Type; } }
        public override string ToString() { return Type; }

        public Rule SubRule { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static NotRule Read(Context context, XmlElement element)
        {
            XmlElement subRuleElement = MCAD.XmlCommon.XmlTools.GetElement(element, "sub_rule");

            return new NotRule
            {
                SubRule = Factories<Rule>.Read(context.RuleFactories, context, subRuleElement.Attributes["type"].Value, subRuleElement)
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool Matches(Context context, RuleTarget target)
        {
            return !SubRule.Matches(context, target);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Write(XmlDocument document, XmlElement element)
        {
            XmlElement subRuleElement = document.CreateElement("sub_rule");
            subRuleElement.Attributes.Append(MCAD.XmlCommon.XmlTools.WriteStringAttribute(document, "type", SubRule.TypeName));
            SubRule.Write(document, subRuleElement);
            element.AppendChild(subRuleElement);
        }
    }
}
