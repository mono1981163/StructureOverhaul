using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard.Rules
{
    public class BuiltInRuleFactory : Factory<Rule>
    {
        ///////////////////////////////////////////////////////////////////////////////////////////
        public IEnumerable<string> Types
        {
            get 
            {
                List<string> types = new List<string>();
                types.Add(AlwaysRule.Type);
                types.Add(AndRule.Type);
                types.Add(NameRule.Type);
                types.Add(NotRule.Type);
                types.Add(OrRule.Type);
                return types;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool CanCreate(string type)
        {
            switch (type)
            {
                case AlwaysRule.Type:
                case AndRule.Type:
                case NameRule.Type:
                case NotRule.Type:
                case OrRule.Type:
                    return true;
                default:
                    return false;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public Rule Read(Context context, string type, XmlElement element)
        {
            switch (type)
            {
                case AlwaysRule.Type:
                    return new AlwaysRule();
                case AndRule.Type:
                    return AndRule.Read(context, element);
                case NameRule.Type:
                    return NameRule.Read(context, element);
                case NotRule.Type:
                    return NotRule.Read(context, element);
                case OrRule.Type:
                    return OrRule.Read(context, element);
                default:
                    throw new ErrorMessageException("MCADCommon rule factory can not create rule of type: '" + type + "'.");
            }
        }
    }
}
