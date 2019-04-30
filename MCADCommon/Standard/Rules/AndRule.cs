using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public class AndRule : Rule
    {
        public const string Type = "And";
        public string TypeName { get { return Type; } }
        public override string ToString() { return Type; }

        public List<Rule> Rules { get; set; }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static AndRule Read(Context context, XmlElement element)
        {
            List<Rule> rules = new List<Rule>();
            Option<XmlElement> rulesElement = MCAD.XmlCommon.XmlTools.SafeGetElement(element, "rules");
            if (rulesElement.IsSome)
                rules = Factories<Rule>.Read(context.RuleFactories, context, MCAD.XmlCommon.XmlTools.GetElements(rulesElement.Get(), "rule"));

            return new AndRule
            {
                Rules = rules
            };
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool Matches(Context context, RuleTarget target)
        {
            foreach (Rule rule in Rules)
                if (!rule.Matches(context, target))
                    return false;

            return true;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Write(XmlDocument document, XmlElement element)
        {
            XmlElement rulesElement = document.CreateElement("rules");

            foreach (Rule rule in Rules)
            {
                XmlElement ruleElement = document.CreateElement("rule");
                ruleElement.Attributes.Append(MCAD.XmlCommon.XmlTools.WriteStringAttribute(document, "type", rule.TypeName));
                rule.Write(document, ruleElement);
                rulesElement.AppendChild(ruleElement);
            }

            element.AppendChild(rulesElement);
        }
    }
}
