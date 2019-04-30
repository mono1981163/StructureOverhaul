using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MCAD.XmlCommon;
using System.Xml;
using System.IO;
using Common.DotNet.Extensions;
namespace VaultEagleLib.Model.TacTon
{
    public class TactonConfiguration
    {
        public static List<Component> ReadVaultFilesFromTactonFile(string path, string vaultPath)
        {
            List<Component> components = new List<Component>();
            List<string> tactonFiles = new List<string>();
            string moduleName = Path.GetFileNameWithoutExtension(path);
            List<string> missingFiles = new List<string>();
            List<string> notExistingFiles = new List<string>();
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
           // string[] children = { "model", "component-classes", "component-class", "components" };
            XmlElement currentElement = doc.DocumentElement;
            XmlElement modelElement = XmlTools.GetElement(currentElement, "model");
            XmlElement componentClassesElement = XmlTools.GetElement(modelElement, "component-classes");
            foreach(XmlElement componentClassElement in XmlTools.GetElements(componentClassesElement, "component-class"))
            {
                Option<XmlElement> componentsElement = XmlTools.SafeGetElement(componentClassElement, "components");
                if (componentsElement.IsSome)
                {
                    foreach (XmlElement componentElement in XmlTools.GetElements(componentsElement.Get(), "component"))
                        components.Add(Component.Read(componentElement, vaultPath));
                }
            }

            XmlElement rootPartsElement = XmlTools.GetElement(modelElement, "root-parts");
            foreach (XmlElement rootPartElement in XmlTools.GetElements(rootPartsElement, "part"))
                components.AddRange(GetComponentsFromPart(vaultPath, rootPartElement));
            

            List<Component> componentsWithProperties = components.Where(c => c.Properties.Count > 0).ToList();
            List<Component> removeDuplicates = componentsWithProperties.DistinctBy(c => c.Properties.First()).ToList();

            return removeDuplicates;


        }

        private static List<Component> GetComponentsFromPart(string vaultPath, XmlElement rootPartElement)
        {
            List<Component> components = new List<Component>();
            components.Add(Component.Read(rootPartElement, vaultPath));
            Option<XmlElement> subPartsElement = XmlTools.SafeGetElement(rootPartElement, "subparts");
            if (subPartsElement.IsSome)
                components.AddRange(GetComponentsFromSubParts(subPartsElement.Get(), vaultPath));

            return components;
        }

        private static List<Component> GetComponentsFromSubParts(XmlElement subParts, string vaultPath)
        {
            List<Component> components = new List<Component>();
            foreach(XmlElement subPart in XmlTools.GetElements(subParts, "subpart"))
            {
                Option<XmlElement> partElement = XmlTools.SafeGetElement(subPart, "part");
                if (partElement.IsSome)
                    components.AddRange(GetComponentsFromPart(vaultPath, partElement.Get()));
                
            }
            return components;
        }

    }
}
