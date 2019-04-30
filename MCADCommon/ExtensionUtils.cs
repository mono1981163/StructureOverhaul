using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace MCADCommon
{
    public class ExtensionUtils
    {
        private const string ExtensionExtension = ".extension.dll";
        private const string ExtensionClassName = "Extension";

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static void LoadExtensions(Standard.Context context, List<string> extensionPaths)
        {
            foreach (string extensionPath in extensionPaths)
            {
                Option<ExtensionBase> extension = LoadExtension(Environment.ExpandEnvironmentVariables(extensionPath));
                if (extension.IsSome)
                    extension.Get().AddToContext(context);
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        private static Option<ExtensionBase> LoadExtension(string path)
        {
            if (!File.Exists(path))
                return Option.None;

            Assembly assembly = Assembly.LoadFrom(path);
            string typeName = Path.GetFileName(path);
            typeName = typeName.Substring(0, typeName.Length - ExtensionExtension.Length) + "." + ExtensionClassName;
            Type type = assembly.GetType(typeName);
            if (type == null)
                return Option.None;

            ExtensionBase extension = (ExtensionBase)Activator.CreateInstance(type);

            return ((ExtensionBase)Activator.CreateInstance(type)).AsOption();
        }
    }
}
