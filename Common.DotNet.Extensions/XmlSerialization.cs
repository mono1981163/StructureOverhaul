using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

namespace Common.DotNet.Extensions
{
    public class XmlSerialization
    {
        public static void Serialize<T>(T o, string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var serializer = new XmlSerializer(typeof(T));
            using (var textWriter = new StreamWriter(File.Open(path, FileMode.Create, FileAccess.Write)))
                serializer.Serialize(textWriter, o);
        }

        public static T DeserializeOrDefault<T>(string path) where T : class, new()
        {
            var t = Deserialize<T>(path);
            return t ?? new T();
        }

        public static T Deserialize<T>(string path) where T : class
        {
            if (!File.Exists(path))
                return null;

            var deserializer = new XmlSerializer(typeof(T));
            using (var textReader = new StreamReader(File.Open(path, FileMode.Open, FileAccess.Read)))
                return deserializer.Deserialize(textReader).CastTo<T>();
        }
    }
}
