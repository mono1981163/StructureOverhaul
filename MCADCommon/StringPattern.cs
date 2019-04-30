using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MCADCommon
{
    public class StringPattern
    {
        public bool IsRegex { get; set; }
        public string Pattern { get; set; }

        /*****************************************************************************************/
        public override string ToString()
        {
            return Pattern;
        }

        /*****************************************************************************************/
        public static List<string> FindMatches(IEnumerable<StringPattern> patterns, IEnumerable<string> values)
        {
            List<string> matches = new List<string>();

            foreach (string value in values)
                if (IsMatch(patterns, value))
                    matches.Add(value);

            return matches;
        }

        /*****************************************************************************************/
        public static bool IsMatch(IEnumerable<StringPattern> patterns, string text)
        {
            foreach (StringPattern pattern in patterns)
                if (pattern.IsMatch(text))
                    return true;

            return false;
        }

        /*****************************************************************************************/
        public bool IsMatch(string text)
        {
            string pattern = IsRegex ? Pattern : ("^" + Pattern.Trim().Replace("*", ".*") + "$");

            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(text);
        }

        /*****************************************************************************************/
        public bool isSamePath(string text)
        {
            string pattern = IsRegex ? Pattern : ("^" + Pattern.Trim().Replace("*", ".*"));

            pattern = pattern.Replace(@"\", @"\\");

            if (!IsRegex)
            {
                if (!pattern.Contains(".*"))
                    pattern += ".*";
                pattern += "$";
            }
            return new Regex(pattern, RegexOptions.IgnoreCase).IsMatch(text);
        }

        /*****************************************************************************************/
        public static StringPattern Create(XmlElement element)
        {
            bool isRegex = bool.Parse(MCAD.XmlCommon.XmlTools.SafeReadStringAttribute(element, "is_regex").Else("false"));
            string pattern = isRegex ? MCAD.XmlCommon.XmlTools.UnescapeTextFromXml(element.InnerText) : element.InnerText;

            return new StringPattern { IsRegex = isRegex, Pattern = pattern };
        }

        /*****************************************************************************************/
        public XmlElement Write(XmlDocument document, string name)
        {
            XmlElement element = document.CreateElement(name);
            element.Attributes.Append(MCAD.XmlCommon.XmlTools.WriteStringAttribute(document, "is_regex", IsRegex.ToString()));
            element.InnerText = IsRegex ? MCAD.XmlCommon.XmlTools.EscapeTextForXml(Pattern) : Pattern;
            return element;
        }
    }
}
