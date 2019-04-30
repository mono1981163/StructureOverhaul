using Common.DotNet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MCADCommon.Standard
{
    public class Factories<T>
    {
        private List<Factory<T>> Instances;

        IEnumerable<string> Types 
        { 
            get
            {
                List<string> types = new List<string>();

                foreach (Factory<T> factory in Instances)
                    types.AddRange(factory.Types.ToArray());

                return types;
            }
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static bool CanCreate(Factories<T> factories, string type)
        {
            foreach (Factory<T> factory in factories.Instances)
                if (factory.CanCreate(type))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static List<T> Read(Factories<T> factories, Context context, IEnumerable<XmlElement> elements)
        {
            List<T> items = new List<T>();

            foreach (XmlElement element in elements)
                items.Add(Read(factories, context, element.Attributes["type"].Value, element));

            return items;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public static T Read(Factories<T> factories, Context context, string type, XmlElement element)
        {
            foreach (Factory<T> factory in factories.Instances)
                if (factory.CanCreate(type))
                    return factory.Read(context, type, element);

            throw new ErrorMessageException("There is no factory to create: '" + typeof(T).FullName + "' for: '" + type + "'.");
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public Factories()
        {
            Instances = new List<Factory<T>>();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public void Add(Factory<T> factory)
        {
            Instances.Add(factory);
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public bool CanCreate(string type)
        {
            foreach (Factory<T> factory in Instances)
                if (factory.CanCreate(type))
                    return true;

            return false;
        }

        ///////////////////////////////////////////////////////////////////////////////////////////
        public T Read(Context context, string type, XmlElement element)
        {
            foreach (Factory<T> factory in Instances)
                if (factory.CanCreate(type))
                    return factory.Read(context, type, element);

            throw new ErrorMessageException("Factory can not create: '" + typeof(T).FullName + "' for: '" + type + "'.");
        }
    }
}
