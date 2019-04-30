using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Xml;

namespace MCAD.XmlCommon
{
    public static class XmlTools
    {
        /*****************************************************************************************/
        public static string EscapeTextForXml(string text)
        {
            return HttpUtility.HtmlEncode(text);
        }

        /*****************************************************************************************/
        public static string UnescapeTextFromXml(string text)
        {
            return HttpUtility.HtmlDecode(text);
        }

        /*****************************************************************************************/
        public static Option<XmlElement> SafeGetElement(XmlElement parent, string tagName)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement child = (XmlElement)node;

                    if (child.Name.Equals(tagName, StringComparison.CurrentCultureIgnoreCase))
                        return child.AsOption();
                }
            }

            return Option.None;
        }

        /*****************************************************************************************/
        public static XmlElement GetElement(XmlElement parent, string tagName, bool required = true)
        {
            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement child = (XmlElement)node;

                    if (child.Name.Equals(tagName, StringComparison.CurrentCultureIgnoreCase))
                        return child;
                }
            }

            if (required)
                throw new Exception("Failed to find child of '" + parent.Name + "' with tag: '" + tagName + "'.");
            else
                return null;
        }

        /*****************************************************************************************/
        public static List<XmlElement> GetElements(XmlElement parent, string childTag)
        {
            List<XmlElement> elements = new List<XmlElement>();

            foreach (XmlNode node in parent.ChildNodes)
            {
                if (node is XmlElement)
                {
                    XmlElement child = (XmlElement)node;
                    if (child.Name.Equals(childTag, StringComparison.CurrentCultureIgnoreCase))
                    {
                        elements.Add(child);
                    }
                }
            }

            return elements;
        }

        /*****************************************************************************************/
        public static Option<string> SafeReadString(XmlElement element, string tagName)
        {
            Option<XmlElement> valueElement = SafeGetElement(element, tagName);
            if (valueElement.IsNone)
                return Option.None;

            return valueElement.Get().InnerText.AsOption();
        }

        /*****************************************************************************************/
        public static string ReadString(XmlElement element, string tagName, string defaultValue)
        {
            XmlElement stringElement = GetElement(element, tagName, false);

            return stringElement == null ? defaultValue : stringElement.InnerText;
        }

        /*****************************************************************************************/
        public static string ReadString(XmlElement element, string tagName)
        {
            return GetElement(element, tagName).InnerText;
        }

        /*****************************************************************************************/
        public static List<string> ReadStringValues(XmlElement element, string groupTagName, string childTagName, bool required = true)
        {
            List<string> values = new List<string>();

            XmlElement groupElement = GetElement(element, groupTagName, required);
            if (groupElement != null)
                foreach (XmlElement child in GetElements(groupElement, childTagName))
                    values.Add(child.InnerText);

            return values;
        }

        /*****************************************************************************************/
        public static XmlElement WriteString(XmlDocument document, string tagName, string value)
        {
            XmlElement element = document.CreateElement(tagName);
            element.InnerText = value;
            return element;
        }

        /*****************************************************************************************/
        public static XmlElement WriteStringValues(XmlDocument document, string groupTagName, string childTagName, List<string> values)
        {
            XmlElement groupElement = document.CreateElement(groupTagName);

            foreach (string value in values)
                groupElement.AppendChild(WriteString(document, childTagName, value));

            return groupElement;
        }

        /*****************************************************************************************/
        public static Option<string> SafeReadStringAttribute(XmlElement element, string name)
        {
            foreach (XmlAttribute attribute in element.Attributes)
                if (attribute.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    return attribute.Value.AsOption();

            return Option.None;
        }

        /*****************************************************************************************/
        public static XmlAttribute WriteStringAttribute(XmlDocument document, string name, string value)
        {
            XmlAttribute attribute = document.CreateAttribute(name);
            attribute.Value = value;
            return attribute;
        }
    }
}
