using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.DotNet.Extensions;
using MCAD.XmlCommon;
using System.Xml;
namespace VaultEagleLib.Model.TacTon
{
    public class Component
    {
        public Component(string name/*, string description, string featureValues,*/, Dictionary<string, string> properties)
        {
            Name = name;
            //   Description = description;
            //   FeatureValues = featureValues;
            Properties = properties;
        }

        public string Name { get; private set; }
        // public string Description { get; private set; }
        // public string FeatureValues { get; private set; }
        public Dictionary<string, string> Properties { get; private set; }
        public static Component Read(XmlElement componentElement, string assemblyPath)
        {
            List<string> propertiesToCheck = new List<string>() { "da_drawing", "da_assembly", "da_document" };
            string name = XmlTools.ReadString(componentElement, "name");
            // string description = XmlTools.ReadString(componentElement, "description");
            // string featureValues = XmlTools.ReadString(componentElement, "feature-values");
            Option<XmlElement> propertiesElement = XmlTools.SafeGetElement(componentElement, "properties");
            Dictionary<string, string> properties = new Dictionary<string, string>();
            if (propertiesElement.IsSome)
            {
                foreach (XmlElement propertyElement in XmlTools.GetElements(propertiesElement.Get(), "property"))
                {
                    string path = XmlTools.ReadString(propertyElement, "value");
                    path = path.Replace("\\", "/");
                    if (!assemblyPath.EndsWith("/"))
                        assemblyPath += "/";
                    if (path.StartsWith("/"))
                        path = assemblyPath + path.Substring(1);
                    
                    else
                        path = assemblyPath + path;
                    if (propertiesToCheck.Contains(XmlTools.ReadString(propertyElement, "name")))
                        properties[XmlTools.ReadString(propertyElement, "name")] = path;
                }
            }

            return new Component(name,/* description, featureValues,*/ properties);

        }

        public override string ToString()
        {
            if (Properties.Count > 0)
                return $"Component: Name: {Name}, {Properties.First().Key}: {Properties.First().Value} ";
            else
                return $"Component: Name: {Name}";
        }

    }
}
